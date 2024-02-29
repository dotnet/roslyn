@echo off
dotnet pwsh -noprofile -file "%~dp0\test-determinism.ps1" %*
