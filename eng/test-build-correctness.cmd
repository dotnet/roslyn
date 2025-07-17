@echo off
set PSMODULEPATH=
powershell -noprofile -executionPolicy Unrestricted -file "%~dp0\test-build-correctness.ps1" %*
