@echo off
set PSMODULEPATH=
powershell -noprofile -file "%~dp0\test-rebuild.ps1" %*
