@echo off
set PSMODULEPATH=
powershell -noprofile -executionPolicy Unrestricted -file "%~dp0\make-bootstrap.ps1" %* 