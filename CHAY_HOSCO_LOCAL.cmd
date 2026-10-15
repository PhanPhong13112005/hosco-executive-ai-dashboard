@echo off
setlocal
cd /d "%~dp0"
where pwsh.exe >nul 2>nul
if errorlevel 1 (
    if exist "%ProgramFiles%\PowerShell\7\pwsh.exe" (
        set "HOSCO_LOCAL_SHELL=%ProgramFiles%\PowerShell\7\pwsh.exe"
    ) else if exist "%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe" (
        rem Reuse the existing DEV runtime, if present; do not install or alter policy.
        set "HOSCO_LOCAL_SHELL=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe"
    ) else (
        set "HOSCO_LOCAL_SHELL=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
    )
) else (
    set "HOSCO_LOCAL_SHELL=pwsh.exe"
)
echo HOSCO - Tu dong cai thu vien va chay local
echo Giu cua so nay mo trong khi test. Nhan Enter de dung sau khi khoi dong.
echo.
"%HOSCO_LOCAL_SHELL%" -NoLogo -NoProfile -File "%~dp0scripts\start-local.ps1" %*
set "HOSCO_LOCAL_EXIT=%ERRORLEVEL%"
if not "%HOSCO_LOCAL_EXIT%"=="0" (
    echo.
    echo Khoi dong that bai. Doc loi o tren.
    echo Khong tat WDAC, khong thay ExecutionPolicy de ne chinh sach cong ty.
    pause
)
exit /b %HOSCO_LOCAL_EXIT%
