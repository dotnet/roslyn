@echo off
pwsh -noprofile -executionPolicy RemoteSigned -file "%~dp0\generate-compiler-code.ps1" %* 

