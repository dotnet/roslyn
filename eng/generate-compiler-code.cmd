@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\generate-compiler-code.ps1" %* 

