@echo off
pwsh -noprofile -executionPolicy RemoteSigned -file "%~dp0\validate-rules-missing-documentation.ps1" %*
