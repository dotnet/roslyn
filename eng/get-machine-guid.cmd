@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\get-machine-guid.ps1" %* 