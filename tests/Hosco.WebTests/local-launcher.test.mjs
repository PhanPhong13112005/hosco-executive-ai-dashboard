import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdtempSync, readFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import test from 'node:test'

const root = path.resolve(fileURLToPath(new URL('../..', import.meta.url)))
const script = path.join(root, 'scripts/start-local.ps1')
const ps = value => "'" + value.replaceAll("'", "''") + "'"
const shell = process.platform === 'win32' ? 'pwsh.exe' : 'pwsh'
const available = process.platform === 'win32' && !spawnSync(shell, ['-NoProfile', '-Command', 'exit 0'], { encoding: 'utf8' }).error

function runPowerShell(body) {
  const fixture = mkdtempSync(path.join(tmpdir(), 'hosco-launcher-'))
  const result = spawnSync(shell, ['-NoLogo', '-NoProfile', '-NonInteractive', '-Command', `
    $ErrorActionPreference = 'Stop'
    . ${ps(script)}
    function Get-TesterRepoRoot { return ${ps(fixture)} }
    function Assert-TesterTools { }
    function Assert-HoscoLocalPorts { }
    $script:setupCalls = 0
    $script:started = @()
    $script:stopped = @()
    function Invoke-TesterCommand { param($Command, $Arguments) $script:setupCalls++ }
    function Start-HoscoLocalService {
      param($Service, $RepoRoot, $LogDirectory)
      $script:started += $Service
      return [pscustomobject]@{ Name=$Service; HasExited=$false; ExitCode=0 }
    }
    function Stop-HoscoLocalService { param($Process) $script:stopped += $Process.Name }
    function Wait-HoscoLocalReady { param($Processes, $TimeoutSeconds) }
    ${body}
    Write-Output 'ASSERTIONS_OK'
  `], { encoding: 'utf8', timeout: 20000 })
  assert.equal(result.status, 0, result.stdout + result.stderr)
  assert.match(result.stdout, /ASSERTIONS_OK/)
}

test('double-click entry is relative, preserves errors and never bypasses Windows policy', () => {
  const cmd = readFileSync(path.join(root, 'CHAY_HOSCO_LOCAL.cmd'), 'utf8')
  const source = readFileSync(script, 'utf8')
  assert.match(cmd, /%~dp0scripts\\start-local\.ps1/)
  assert.match(cmd, /HOSCO_LOCAL_EXIT=%ERRORLEVEL%/)
  assert.match(cmd, /pause/)
  for (const text of [cmd, source]) {
    assert.doesNotMatch(text, /-ExecutionPolicy|Set-ExecutionPolicy|Unblock-File|npm\s+audit\s+fix|git\s+(?:reset|push|checkout)/i)
  }
  assert.match(source, /'-AiProvider', 'Mock', '-Database', 'Sqlite', '-EnableScheduler'/)
  assert.match(source, /-WindowStyle Hidden/)
  assert.match(source, /\/PID \$\(\$Process\.Id\) \/T \/F/)
  assert.doesNotMatch(source, /\/IM\b|Stop-Process\s+-Name/i)
})

test('launcher parses without errors in PowerShell', { skip: !available }, () => {
  const result = spawnSync(shell, ['-NoProfile', '-NonInteractive', '-Command', `
    $t=$null; $e=$null
    [System.Management.Automation.Language.Parser]::ParseFile(${ps(script)},[ref]$t,[ref]$e) | Out-Null
    if ($e.Count) { $e | Out-String | Write-Output; exit 1 }
  `], { encoding: 'utf8' })
  assert.equal(result.status, 0, result.stdout + result.stderr)
})

test('CheckOnly does not install or start services', { skip: !available }, () => {
  runPowerShell(`
    Start-HoscoLocal -CheckOnly
    if ($script:setupCalls -ne 0 -or $script:started.Count -ne 0) { throw 'CheckOnly mutated runtime' }
  `)
})

test('busy ports fail before setup and never stop another process', { skip: !available }, () => {
  runPowerShell(`
    function Assert-HoscoLocalPorts { throw 'PORT_IN_USE' }
    $failure=$null
    try { Start-HoscoLocal -SmokeTest -NoBrowser } catch { $failure=$_.Exception.Message }
    if ($failure -ne 'PORT_IN_USE') { throw 'Expected port error' }
    if ($script:setupCalls -ne 0 -or $script:started.Count -ne 0 -or $script:stopped.Count -ne 0) { throw 'Port failure mutated runtime' }
  `)
})

test('setup failure prevents service startup and releases the launch lock', { skip: !available }, () => {
  runPowerShell(`
    function Invoke-TesterCommand { param($Command,$Arguments) throw 'SETUP_FAILED' }
    $failure=$null
    try { Start-HoscoLocal -SmokeTest -NoBrowser } catch { $failure=$_.Exception.Message }
    if ($failure -ne 'SETUP_FAILED' -or $script:started.Count -ne 0) { throw 'Expected setup-only failure' }
    Start-HoscoLocal -CheckOnly
  `)
})

test('SmokeTest starts both services after setup then cleans up only those services', { skip: !available }, () => {
  runPowerShell(`
    Start-HoscoLocal -SmokeTest -NoBrowser
    if ($script:setupCalls -ne 1) { throw 'Setup was not called once' }
    if (($script:started -join ',') -ne 'Backend,Frontend') { throw 'Wrong services' }
    if (($script:stopped -join ',') -ne 'Backend,Frontend') { throw 'Missing cleanup' }
  `)
})

test('partial startup failure still stops the backend created by this invocation', { skip: !available }, () => {
  runPowerShell(`
    function Start-HoscoLocalService {
      param($Service,$RepoRoot,$LogDirectory)
      if ($Service -eq 'Frontend') { throw 'FRONTEND_FAILED' }
      return [pscustomobject]@{Name=$Service;HasExited=$false;ExitCode=0}
    }
    $failure=$null
    try { Start-HoscoLocal -SmokeTest -NoBrowser } catch { $failure=$_.Exception.Message }
    if ($failure -ne 'FRONTEND_FAILED' -or ($script:stopped -join ',') -ne 'Backend') { throw 'Backend leaked after partial failure' }
  `)
})

test('readiness failure cleans up both services and does not open browser', { skip: !available }, () => {
  runPowerShell(`
    function Wait-HoscoLocalReady { param($Processes,$TimeoutSeconds) throw 'NOT_READY' }
    function Start-Process { throw 'BROWSER_MUST_NOT_OPEN' }
    $failure=$null
    try { Start-HoscoLocal } catch { $failure=$_.Exception.Message }
    if ($failure -ne 'NOT_READY' -or $script:stopped.Count -ne 2) { throw 'Wrong readiness cleanup' }
  `)
})

test('child launch quotes paths and restores parent credentials without exposing them', { skip: !available }, () => {
  const result = spawnSync(shell, ['-NoProfile', '-NonInteractive', '-Command', String.raw`
    $ErrorActionPreference='Stop'
    . ${ps(script)}
    $env:Jwt__SigningKey='unit-fixture-not-a-real-secret'
    $env:AlertScheduler__RunOnStartup='false'
    # Override only process creation; exercise the actual isolation/restoration function.
    function Start-Process {
      param($FilePath,$ArgumentList,$WorkingDirectory,$WindowStyle,[switch]$PassThru,$RedirectStandardOutput,$RedirectStandardError)
      if ($env:Jwt__SigningKey) { throw 'Inherited signing credential' }
      if ($env:AlertScheduler__RunOnStartup -ne 'true') { throw 'Missing demo scheduler setting' }
      if ($ArgumentList[3] -ne '"C:\HOSCO with spaces\scripts\run-tester.ps1"') { throw 'Unquoted path' }
      return [pscustomobject]@{Id=123}
    }
    $null=Start-HoscoLocalService -Service Backend -RepoRoot 'C:\HOSCO with spaces' -LogDirectory 'C:\logs'
    if ($env:Jwt__SigningKey -ne 'unit-fixture-not-a-real-secret') { throw 'Parent credential not restored' }
    if ($env:AlertScheduler__RunOnStartup -ne 'false') { throw 'Parent scheduler setting not restored' }
  `], { encoding: 'utf8', timeout: 20000 })
  assert.equal(result.status, 0, result.stdout + result.stderr)
})

test('default browser failure is reported without tearing down working servers', { skip: !available }, () => {
  runPowerShell(`
    function Start-Process { throw 'NO_DEFAULT_BROWSER' }
    Open-HoscoLocalBrowser
    if ($script:stopped.Count -ne 0) { throw 'Browser error stopped servers' }
  `)
})
