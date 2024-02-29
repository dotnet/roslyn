@echo off
dotnet pwsh -noprofile -file "%~dp0\test-rebuild.ps1" %*
