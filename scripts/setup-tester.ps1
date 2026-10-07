[CmdletBinding()]
param([switch]$CheckOnly)

. (Join-Path $PSScriptRoot 'tester-common.ps1')
Push-Location (Get-TesterRepoRoot)
try {
    Assert-TesterTools
    if (-not $CheckOnly) {
        Invoke-TesterCommand dotnet @('restore', 'Hosco.slnx')
        Invoke-TesterCommand dotnet @('tool', 'restore', '--tool-manifest', 'dotnet-tools.json')
        Invoke-TesterCommand dotnet @('build', 'Hosco.slnx', '--no-restore')
        Push-Location 'src/Hosco.Web'
        try {
            # Lockfile-based install. No dependency upgrade or npm audit fix.
            Invoke-TesterCommand npm.cmd @('ci', '--no-audit', '--no-fund')
            Invoke-TesterCommand npm.cmd @('run', 'build')
        }
        finally { Pop-Location }
    }
    Write-Host 'Setup checks complete. No database was changed and no API key is required.'
    Write-Host 'Terminal 1: .\scripts\run-tester.ps1 -Service Backend -AiProvider Mock'
    Write-Host 'Terminal 2: .\scripts\run-tester.ps1 -Service Frontend'
    Write-Host 'Default SQLite is in-memory: schema/seed initialize at startup; all changes disappear on stop.'
    Write-Host 'For persistent SQL Server migration/seed, follow docs/testing/TESTER_SETUP.md (dedicated test database only).'
}
finally { Pop-Location }
