@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\cibuild-legacy.ps1" %*
