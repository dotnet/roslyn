@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\validate-rules-missing-documentation.ps1" %*
