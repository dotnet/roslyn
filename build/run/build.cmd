@echo off
@setlocal

set RoslynRoot=%~dp0\..\..\

msbuild "%RoslynRoot%\Roslyn.sln"
