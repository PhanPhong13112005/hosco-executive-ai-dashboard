Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-TesterRepoRoot {
    $root = Split-Path -Parent $PSScriptRoot
    if (-not (Test-Path -LiteralPath (Join-Path $root 'Hosco.slnx'))) {
        throw 'Cannot locate Hosco.slnx. Keep these scripts inside the repository scripts directory.'
    }
    return $root
}

function Invoke-TesterCommand {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed (exit $LASTEXITCODE). See the command output; do not bypass Windows security policy."
    }
}

function Assert-TesterTools {
    foreach ($tool in @('dotnet', 'node', 'npm.cmd')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "Missing $tool. Install the prerequisites listed in docs/testing/TESTER_SETUP.md and reopen PowerShell."
        }
    }
    $sdk = & dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'The SDK selected by global.json is unavailable. Install .NET SDK 10.0.400 (or a compatible 10.0.4xx patch).' }
    $nodeVersion = & node --version
    if ($LASTEXITCODE -ne 0) { throw 'Cannot run Node.js.' }
    $nodeSemver = [version]($nodeVersion.Trim().TrimStart('v').Split('-')[0])
    if (-not (($nodeSemver.Major -eq 20 -and $nodeSemver -ge [version]'20.19.0') -or $nodeSemver -ge [version]'22.12.0')) {
        throw 'Vite requires Node.js 20.19+ in the 20.x line, or 22.12+.'
    }
    $npmVersion = & npm.cmd --version
    if ($LASTEXITCODE -ne 0) { throw 'Cannot run npm.cmd.' }
    Write-Host "SDK $sdk | Node $nodeVersion | npm $npmVersion"
}

function Assert-TesterAi {
    param([string]$Provider)
    if ($Provider -eq 'Mock') {
        Write-Host 'AI_PROVIDER=Mock: deterministic resolver; no paid LLM requests.'
        return
    }
    if ([string]::IsNullOrWhiteSpace($env:AI_MODEL)) {
        throw 'LIVE_LLM_PROVIDER=NOT_CONFIGURED: set AI_MODEL to a supported model identifier in this backend terminal.'
    }
    if ($env:AI_MODEL.Length -gt 100 -or $env:AI_MODEL -notmatch '^[a-zA-Z0-9._-]+$') {
        throw 'AI_MODEL must be a model identifier, not a URL.'
    }
    $keyName = if ($Provider -eq 'OpenAI') { 'OPENAI_API_KEY' } else { 'GEMINI_API_KEY' }
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($keyName, 'Process'))) {
        throw "LIVE_LLM_PROVIDER=NOT_CONFIGURED: provision $keyName in this backend terminal or choose -AiProvider Mock."
    }
    Write-Host "AI_PROVIDER=${Provider}: live requests may incur charges. Credential present; value not displayed."
}
