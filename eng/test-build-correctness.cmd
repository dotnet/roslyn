@echo off
dotnet pwsh -noprofile -file "%~dp0\test-build-correctness.ps1" %*
