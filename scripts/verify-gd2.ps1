[CmdletBinding()]
param(
    [switch]$RunApiChecks,
    [string]$ApiBaseUrl = 'http://127.0.0.1:51989'
)

$ErrorActionPreference = 'Stop'
$expectedBranch = 'chore/gd2-evidence'
$expectedCheckpoint = 'f75ac38'
$repoRoot = Split-Path -Parent $PSScriptRoot

function Invoke-NativeStep {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n== $Name ==" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE." }
}

function Invoke-ApiVerification {
    $email = $env:HOSCO_GD2_DEMO_EMAIL
    $password = $env:HOSCO_GD2_DEMO_PASSWORD
    $branchManagerEmail = $env:HOSCO_GD2_BRANCH_MANAGER_EMAIL
    $forbiddenBranchId = $env:HOSCO_GD2_FORBIDDEN_BRANCH_ID
    if ([string]::IsNullOrWhiteSpace($email) -or [string]::IsNullOrWhiteSpace($password) -or
        [string]::IsNullOrWhiteSpace($branchManagerEmail) -or [string]::IsNullOrWhiteSpace($forbiddenBranchId)) {
        throw 'API checks require HOSCO_GD2_DEMO_EMAIL, HOSCO_GD2_DEMO_PASSWORD, HOSCO_GD2_BRANCH_MANAGER_EMAIL, and HOSCO_GD2_FORBIDDEN_BRANCH_ID.'
    }

    $apiDll = Join-Path $repoRoot 'src/Hosco.Api/bin/Debug/net10.0/Hosco.Api.dll'
    if (-not (Test-Path -LiteralPath $apiDll)) { throw "API build output was not found at $apiDll." }
    $stdout = Join-Path ([System.IO.Path]::GetTempPath()) "hosco-gd2-api-$PID.out.log"
    $stderr = Join-Path ([System.IO.Path]::GetTempPath()) "hosco-gd2-api-$PID.err.log"
    $apiProcess = $null
    try {
        $arguments = @(
            $apiDll,
            '--Database:Provider=Sqlite',
            '--Seed:Enabled=true',
            '--Swagger:Enabled=true',
            '--Logging:LogLevel:Default=Warning',
            "--urls=$ApiBaseUrl"
        )
        $apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory (Split-Path -Parent $apiDll) -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $ready = $null
        for ($attempt = 0; $attempt -lt 60; $attempt++) {
            if ($apiProcess.HasExited) { throw "API exited during startup with code $($apiProcess.ExitCode). Review $stdout and $stderr." }
            try {
                $ready = Invoke-WebRequest -Uri "$ApiBaseUrl/health/ready" -SkipHttpErrorCheck
                if ($ready.StatusCode -eq 200) { break }
            } catch {}
            Start-Sleep -Milliseconds 500
        }
        if ($null -eq $ready -or $ready.StatusCode -ne 200) { throw 'API did not become ready within 30 seconds.' }

        $live = Invoke-WebRequest -Uri "$ApiBaseUrl/health/live" -SkipHttpErrorCheck
        if ($live.StatusCode -ne 200) { throw "Live check returned $($live.StatusCode)." }
        $unauthorized = Invoke-WebRequest -Uri "$ApiBaseUrl/api/v1/reporting/orders?pageSize=1" -SkipHttpErrorCheck
        if ($unauthorized.StatusCode -ne 401) { throw "Unauthenticated reporting check returned $($unauthorized.StatusCode), expected 401." }

        $login = Invoke-WebRequest -Uri "$ApiBaseUrl/api/v1/auth/login" -Method Post -ContentType 'application/json' -Body (@{ email = $email; password = $password } | ConvertTo-Json -Compress) -SkipHttpErrorCheck
        if ($login.StatusCode -ne 200) { throw "Owner login returned $($login.StatusCode)." }
        $token = ($login.Content | ConvertFrom-Json).accessToken
        $authorized = Invoke-WebRequest -Uri "$ApiBaseUrl/api/v1/reporting/orders?pageSize=1" -Headers @{ Authorization = "Bearer $token" } -SkipHttpErrorCheck
        if ($authorized.StatusCode -ne 200) { throw "Authenticated reporting check returned $($authorized.StatusCode)." }

        $branchLogin = Invoke-WebRequest -Uri "$ApiBaseUrl/api/v1/auth/login" -Method Post -ContentType 'application/json' -Body (@{ email = $branchManagerEmail; password = $password } | ConvertTo-Json -Compress) -SkipHttpErrorCheck
        if ($branchLogin.StatusCode -ne 200) { throw "Branch-manager login returned $($branchLogin.StatusCode)." }
        $branchToken = ($branchLogin.Content | ConvertFrom-Json).accessToken
        $forbidden = Invoke-WebRequest -Uri "$ApiBaseUrl/api/v1/reporting/orders?branchId=$forbiddenBranchId&pageSize=1" -Headers @{ Authorization = "Bearer $branchToken" } -SkipHttpErrorCheck
        if ($forbidden.StatusCode -ne 403) { throw "Out-of-scope branch check returned $($forbidden.StatusCode), expected 403." }

        $swagger = Invoke-WebRequest -Uri "$ApiBaseUrl/swagger/v1/swagger.json" -SkipHttpErrorCheck
        if ($swagger.StatusCode -ne 200) { throw "Swagger check returned $($swagger.StatusCode)." }
        Write-Host 'API checks passed: live=200, ready=200, login=200, authenticated=200, unauthenticated=401, out-of-scope=403, swagger=200.' -ForegroundColor Green
    }
    finally {
        if ($apiProcess -and -not $apiProcess.HasExited) {
            Stop-Process -Id $apiProcess.Id
            Wait-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
        }
    }
}

try {
    Push-Location $repoRoot
    $actualRoot = (git rev-parse --show-toplevel).Trim()
    if ($LASTEXITCODE -ne 0 -or [System.IO.Path]::GetFullPath($actualRoot) -ne [System.IO.Path]::GetFullPath($repoRoot)) { throw "Repository root mismatch: $actualRoot" }
    $branch = (git branch --show-current).Trim()
    if ($branch -ne $expectedBranch) { throw "Expected branch '$expectedBranch', found '$branch'." }
    git cat-file -e "$expectedCheckpoint^{commit}"
    if ($LASTEXITCODE -ne 0) { throw "Checkpoint $expectedCheckpoint is missing." }
    Write-Host "Repository and branch verified: $actualRoot [$branch]" -ForegroundColor Green

    Invoke-NativeStep 'Restore' { dotnet restore Hosco.slnx }
    Invoke-NativeStep 'Build' { dotnet build Hosco.slnx --no-restore }
    Invoke-NativeStep 'Tests' { dotnet test Hosco.slnx --no-build }
    Invoke-NativeStep 'EF migration list' { dotnet ef migrations list --no-build --project src/Hosco.Infrastructure --startup-project src/Hosco.Api }
    $migrations = Get-ChildItem -LiteralPath 'src/Hosco.Infrastructure/Persistence/Migrations' -Filter '*_*.cs' | Where-Object Name -NotLike '*.Designer.cs'
    if (-not $migrations) { throw 'No EF migration source file was found.' }
    Write-Host "Migration source files found: $($migrations.Count)" -ForegroundColor Green

    Write-Host "`n== Basic secret-location scan ==" -ForegroundColor Cyan
    $pattern = '(?i)["'']?([a-z0-9_]*(password|passwd|pwd|secret|api[_-]?key|signingkey))["'']?\s*[:=]\s*["'']?[^"''\s;,}{]{8,}|-----BEGIN [A-Z ]*PRIVATE KEY-----|bearer\s+eyJ'
    $findings = foreach ($file in (git ls-files)) {
        if ($file -eq 'scripts/verify-gd2.ps1' -or $file -like 'docs/gd2/evidence/*') { continue }
        if (Test-Path -LiteralPath $file -PathType Leaf) {
            Select-String -LiteralPath $file -Pattern $pattern | ForEach-Object { "$file`:$($_.LineNumber)" }
        }
    }
    if ($findings) {
        Write-Warning 'Potential secret-related material requires review (locations only; values suppressed):'
        $findings | Sort-Object -Unique | ForEach-Object { Write-Warning $_ }
    } else {
        Write-Host 'No basic pattern matches found.' -ForegroundColor Green
    }

    if ($RunApiChecks) { Invoke-ApiVerification }
    Write-Host "`nGD2 verification completed successfully." -ForegroundColor Green
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
}
