[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$NoBrowser,
    [switch]$SmokeTest,
    [ValidateRange(10, 600)][int]$StartupTimeoutSeconds = 120
)

# Compatible with Windows PowerShell 5.1 and PowerShell 7. No policy bypass.
. (Join-Path $PSScriptRoot 'tester-common.ps1')

# Windows Job Object: child processes are automatically killed when this window is closed (X button).
# Uses KILL_ON_JOB_CLOSE so closing CMD/PowerShell window cleans up backend and frontend.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class HoscoJobObject {
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, ref JELO lpJobObjectInfo, uint cbJobObjectInfoLength);
    [DllImport("kernel32.dll", SetLastError=true)]
    static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);
    [StructLayout(LayoutKind.Sequential)]
    struct JELO { public uint BasicLimitFlags; public UIntPtr MinimumWorkingSetSize; public UIntPtr MaximumWorkingSetSize; public uint ActiveProcessLimit; public UIntPtr Affinity; public uint PriorityClass; public uint SchedulingClass; }
    const uint KILL_ON_JOB_CLOSE = 0x2000;
    static IntPtr _job = IntPtr.Zero;
    public static bool Init() {
        _job = CreateJobObject(IntPtr.Zero, null);
        if (_job == IntPtr.Zero) return false;
        var info = new JELO { BasicLimitFlags = KILL_ON_JOB_CLOSE };
        return SetInformationJobObject(_job, 9, ref info, (uint)System.Runtime.InteropServices.Marshal.SizeOf(info));
    }
    public static bool Add(IntPtr processHandle) {
        if (_job == IntPtr.Zero) return false;
        return AssignProcessToJobObject(_job, processHandle);
    }
}
'@ -ErrorAction SilentlyContinue
function Initialize-HoscoJobObject {
    try { $null = [HoscoJobObject]::Init() } catch { }
}
function Add-HoscoJobObject {
    param([Diagnostics.Process]$Process)
    try { $null = [HoscoJobObject]::Add($Process.Handle) } catch { }
}

function Assert-HoscoLocalPorts {
    $listeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
    foreach ($port in @(5000, 5173)) {
        if (@($listeners | Where-Object { $_.Port -eq $port }).Count -gt 0) {
            throw "Port $port is already in use. Stop the existing server yourself, then launch again. No existing process has been stopped."
        }
    }
}

function Start-HoscoLocalService {
    param([string]$Service, [string]$RepoRoot, [string]$LogDirectory)
    $shell = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    $runner = Join-Path $RepoRoot 'scripts/run-tester.ps1'
    $arguments = @('-NoLogo', '-NoProfile', '-File', ('"' + $runner + '"'), '-Service', $Service)
    if ($Service -eq 'Backend') { $arguments += @('-AiProvider', 'Mock', '-Database', 'Sqlite', '-EnableScheduler') }
    # A one-click demo never reuses a real signing key, DB connection or provider credential.
    $saved = @{}
    try {
        foreach ($name in @('Jwt__SigningKey', 'ConnectionStrings__HoscoDb', 'OPENAI_API_KEY', 'GEMINI_API_KEY', 'Notifications__Telegram__BotToken')) {
            $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            [Environment]::SetEnvironmentVariable($name, $null, 'Process')
        }
        $saved['AlertScheduler__RunOnStartup'] = [Environment]::GetEnvironmentVariable('AlertScheduler__RunOnStartup', 'Process')
        [Environment]::SetEnvironmentVariable('AlertScheduler__RunOnStartup', 'true', 'Process')
        return Start-Process -FilePath $shell -ArgumentList $arguments -WorkingDirectory $RepoRoot `
            -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $LogDirectory "$Service.stdout.log") `
            -RedirectStandardError (Join-Path $LogDirectory "$Service.stderr.log")
    }
    finally {
        foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process') }
    }
}

function Stop-HoscoLocalService {
    param([Diagnostics.Process]$Process)
    if ($null -eq $Process -or $Process.HasExited) { return }
    # Only kill the tree rooted at the exact child started by this invocation.
    # Never find/kill processes by port, image name or an arbitrary saved PID file.
    $kill = New-Object Diagnostics.ProcessStartInfo
    $kill.FileName = Join-Path $env:SystemRoot 'System32/taskkill.exe'
    $kill.Arguments = "/PID $($Process.Id) /T /F"
    $kill.UseShellExecute = $false
    $kill.CreateNoWindow = $true
    $kill.RedirectStandardOutput = $true
    $kill.RedirectStandardError = $true
    $stopper = [Diagnostics.Process]::Start($kill)
    try {
        $null = $stopper.StandardOutput.ReadToEnd()
        $null = $stopper.StandardError.ReadToEnd()
        $stopper.WaitForExit()
        if (-not $Process.WaitForExit(5000)) { Write-Warning "Child $($Process.Id) did not stop. See the local logs." }
    }
    finally { $stopper.Dispose() }
}

function Test-HoscoLocalUrl {
    param([string]$Uri)
    try {
        $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch { return $false }
}

function Open-HoscoLocalBrowser {
    try { Start-Process 'http://127.0.0.1:5173/' | Out-Null }
    catch { Write-Warning 'Could not open the default browser. Open http://127.0.0.1:5173/ manually; the servers are still running.' }
}

function Wait-HoscoLocalReady {
    param([object[]]$Processes, [int]$TimeoutSeconds)
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        foreach ($child in $Processes) {
            if ($child.HasExited) { throw "A local server exited before it was ready (exit $($child.ExitCode)). Read its log. If WDAC blocked a DLL, ask IT; do not bypass policy." }
        }
        if ((Test-HoscoLocalUrl 'http://localhost:5000/health/ready') -and
            (Test-HoscoLocalUrl 'http://127.0.0.1:5173/') -and
            (Test-HoscoLocalUrl 'http://127.0.0.1:5173/health/ready')) { return }
        Start-Sleep -Milliseconds 500
    }
    throw "Local servers were not ready within $TimeoutSeconds seconds. Read the startup logs; no browser was opened."
}

function Start-HoscoLocal {
    [CmdletBinding()]
    param(
        [switch]$CheckOnly,
        [switch]$NoBrowser,
        [switch]$SmokeTest,
        [ValidateRange(10, 600)][int]$StartupTimeoutSeconds = 120
    )
    $repoRoot = Get-TesterRepoRoot
    $mutex = $null
    $ownsMutex = $false
    $children = @()
    $logDirectory = $null
    Push-Location $repoRoot
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $rootHash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($repoRoot.ToUpperInvariant()))).Replace('-', '').Substring(0, 24) }
        finally { $sha.Dispose() }
        $mutex = New-Object Threading.Mutex($false, "Local\HoscoLocal_$rootHash")
        try { $ownsMutex = $mutex.WaitOne(0) }
        catch [Threading.AbandonedMutexException] { $ownsMutex = $true }
        if (-not $ownsMutex) { throw 'This repository already has a local launcher running. Use that window; no second setup has been started.' }
        Assert-TesterTools
        Assert-HoscoLocalPorts
        if ($CheckOnly) { Write-Host 'Prerequisites and ports OK. No dependencies installed, no servers started.'; return }

        Write-Host '[1/3] Restore locked dependencies and build backend/frontend. Internet is required on first setup.'
        # Reuse the tested setup; never upgrade packages or install system runtimes silently.
        $shell = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
        Invoke-TesterCommand $shell @('-NoLogo', '-NoProfile', '-File', (Join-Path $repoRoot 'scripts/setup-tester.ps1'))
        # Recheck after setup in case another application took a port while installing.
        Assert-HoscoLocalPorts
        $runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
        $logDirectory = Join-Path $repoRoot "work/local-run/$runId"
        $null = New-Item -ItemType Directory -Path $logDirectory -ErrorAction Stop
        Write-Host '[2/3] Start isolated SQLite demo, Mock AI, scheduler and Web. Telegram is disabled.'
        Write-Host "Local startup logs (ignored by Git): $logDirectory"
        Initialize-HoscoJobObject
        $children += Start-HoscoLocalService -Service Backend -RepoRoot $repoRoot -LogDirectory $logDirectory
        $children += Start-HoscoLocalService -Service Frontend -RepoRoot $repoRoot -LogDirectory $logDirectory
        foreach ($child in $children) { Add-HoscoJobObject $child }
        Wait-HoscoLocalReady -Processes $children -TimeoutSeconds $StartupTimeoutSeconds
        Write-Host '[3/3] READY: Web and API (including the Vite proxy) responded HTTP 200.'
        Write-Host 'Web: http://127.0.0.1:5173/ | Swagger: http://localhost:5000/swagger'
        Write-Host 'Use January 2026 filters for demo sales. SQLite RAM resets when stopped.'
        if ($SmokeTest) { Write-Host "Smoke test complete; stopping only this launcher's servers."; return }
        if (-not $NoBrowser) { Open-HoscoLocalBrowser }
        Write-Host 'Keep this window open. Press Enter or Ctrl+C to stop these servers.'
        while ($true) {
            foreach ($child in $children) {
                if ($child.HasExited) { throw "A local server stopped unexpectedly (exit $($child.ExitCode)). See $logDirectory" }
            }
            if (-not [Console]::IsInputRedirected -and [Console]::KeyAvailable) {
                if ([Console]::ReadKey($true).Key -eq [ConsoleKey]::Enter) { break }
            }
            Start-Sleep -Milliseconds 250
        }
    }
    finally {
        foreach ($child in $children) {
            try { Stop-HoscoLocalService $child }
            catch { Write-Warning "Could not stop our child process: $($_.Exception.Message)" }
            finally { if ($child -is [IDisposable]) { $child.Dispose() } }
        }
        if ($ownsMutex) { $mutex.ReleaseMutex() }
        if ($null -ne $mutex) { $mutex.Dispose() }
        Pop-Location
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    try { Start-HoscoLocal @PSBoundParameters }
    catch { Write-Error $_ -ErrorAction Continue; exit 1 }
}
