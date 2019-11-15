@echo off
SETLOCAL
set PS1UnderCmd=1
powershell.exe -NoProfile -NoLogo -ExecutionPolicy bypass -Command "try { & '%~dpn0.ps1' %*; exit $LASTEXITCODE } catch { write-host $_; exit 1 }"
