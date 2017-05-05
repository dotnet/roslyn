@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\cibuild.ps1" %*
