[CmdletBinding()]
param(
    [ValidateSet('Backend', 'Frontend')][string]$Service = 'Backend',
    [ValidateSet('Mock', 'Gemini', 'OpenAI')][string]$AiProvider = 'Mock',
    [ValidateSet('Sqlite', 'SqlServer')][string]$Database = 'Sqlite',
    [switch]$EnableScheduler,
    [switch]$CheckOnly
)

. (Join-Path $PSScriptRoot 'tester-common.ps1')
Push-Location (Get-TesterRepoRoot)
$savedEnvironment = @{}
function Set-TesterProcessVariable([string]$Name, [string]$Value) {
    if (-not $savedEnvironment.ContainsKey($Name)) {
        $savedEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process')
    }
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
}
try {
    Assert-TesterTools
    if ($Service -eq 'Frontend') {
        if (-not (Test-Path -LiteralPath 'src/Hosco.Web/node_modules/vite')) { throw 'Run scripts/setup-tester.ps1 first (frontend dependencies missing).' }
        if ($CheckOnly) { return }
        # Same-origin requests use the existing Vite proxy to localhost:5000.
        Set-TesterProcessVariable 'VITE_API_BASE_URL' ''
        # Frontend subprocess must never inherit backend credentials.
        foreach ($keyName in @('OPENAI_API_KEY', 'GEMINI_API_KEY', 'Jwt__SigningKey', 'ConnectionStrings__HoscoDb', 'Notifications__Telegram__BotToken')) {
            Set-TesterProcessVariable $keyName $null
        }
        Push-Location 'src/Hosco.Web'
        try {
            Write-Host 'Frontend http://localhost:5173. Stop with Ctrl+C. Port conflicts fail instead of selecting another port.'
            Invoke-TesterCommand npm.cmd @('run', 'dev', '--', '--host', '127.0.0.1', '--port', '5173', '--strictPort')
        }
        finally { Pop-Location }
        return
    }

    Assert-TesterAi $AiProvider
    if (-not (Test-Path -LiteralPath 'src/Hosco.Api/bin/Debug/net10.0/Hosco.Api.dll')) { throw 'Run scripts/setup-tester.ps1 first (backend build missing).' }
    if ($Database -eq 'SqlServer' -and [string]::IsNullOrWhiteSpace($env:ConnectionStrings__HoscoDb)) {
        throw 'Set ConnectionStrings__HoscoDb to a dedicated test database. Startup applies migrations and development seed.'
    }
    if (-not [string]::IsNullOrWhiteSpace($env:Jwt__SigningKey) -and $env:Jwt__SigningKey.Length -lt 32) {
        throw 'Jwt__SigningKey must contain at least 32 characters.'
    }
    if ($CheckOnly) { Write-Host 'Backend configuration checks passed (no server started).'; return }

    Set-TesterProcessVariable 'ASPNETCORE_ENVIRONMENT' 'Development'
    Set-TesterProcessVariable 'AI_PROVIDER' $AiProvider
    if ($AiProvider -eq 'Mock') {
        Set-TesterProcessVariable 'AI_MODEL' ''
        Set-TesterProcessVariable 'OPENAI_API_KEY' $null
        Set-TesterProcessVariable 'GEMINI_API_KEY' $null
    }
    if ([string]::IsNullOrWhiteSpace($env:Jwt__SigningKey)) {
        # Per-process development key only; never write or display the generated value.
        $randomBytes = New-Object byte[] 48
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($randomBytes) } finally { $rng.Dispose() }
        Set-TesterProcessVariable 'Jwt__SigningKey' ([Convert]::ToBase64String($randomBytes))
        Write-Host 'Generated an ephemeral JWT signing key. Log in again after every backend restart.'
    }
    # Pin non-secret configuration on the command line, above appsettings/environment.
    # Existing Telegram delivery remains opt-in; this handoff cannot send real notifications.
    Write-Host "Backend http://localhost:5000 | database=$Database | scheduler=$($EnableScheduler.IsPresent) | Telegram disabled. Stop with Ctrl+C."
    if ($Database -eq 'Sqlite') { Write-Host 'SQLite is in-memory, NOT a persistent file database. Restart resets all test data/configuration.' }
    Invoke-TesterCommand dotnet @('run', '--project', 'src/Hosco.Api', '--no-build', '--no-launch-profile', '--',
        '--urls=http://localhost:5000', "--Database:Provider=$Database", '--Seed:Enabled=true', '--Swagger:Enabled=true',
        "--AlertScheduler:Enabled=$($EnableScheduler.IsPresent)", '--Notifications:Telegram:Enabled=false',
        '--AI_TIMEOUT_SECONDS=5', '--AI_MAX_RETRIES=1')
}
finally {
    foreach ($name in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
    }
    Pop-Location
}
