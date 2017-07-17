@echo off
if "%*" == "" (
    powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\build.ps1" -build
) else (
    powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\build.ps1" %*
)
