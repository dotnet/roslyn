@echo off
set PSMODULEPATH=
powershell -noprofile -executionPolicy Unrestricted -file "%~dp0\generate-compiler-code.ps1" %* 

