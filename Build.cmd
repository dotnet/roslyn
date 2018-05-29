@echo off
if "%~1" == "" (
    powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\build.ps1" -build -skipBuildExtras %*
) else (
    powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\build.ps1" %*
)
