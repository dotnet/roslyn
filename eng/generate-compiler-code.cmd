@echo off
dotnet pwsh -noprofile -executionPolicy RemoteSigned -file "%~dp0\generate-compiler-code.ps1" %* 

