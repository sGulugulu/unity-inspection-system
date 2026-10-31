@echo off
setlocal
set "INSPECTION_LAUNCH_ROOT=%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "try { $root=$env:INSPECTION_LAUNCH_ROOT; $code=[IO.File]::ReadAllText((Join-Path $root 'start.ps1'),[Text.Encoding]::UTF8); & ([scriptblock]::Create($code)) -Root $root } catch { Write-Host $_.Exception.Message -ForegroundColor Red; exit 1 }"
if errorlevel 1 pause
endlocal
