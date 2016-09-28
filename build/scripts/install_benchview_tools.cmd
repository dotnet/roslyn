@echo off
setlocal

set "OUTPUT_DIR=%~1"
if "%OUTPUT_DIR%" == "" (echo Usage: build_benchview_tools.cmd ^<output directory^> & exit /b 1)

call "%~dp0clean_benchview_tools.cmd" "%OUTPUT_DIR%" || exit /b 1

:: Installing BenchView Tools
call "%~dp0LoadNuGetInfo.cmd" || (echo Failed to load nuget. & exit /b 1)

call "%NugetExe%" install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory "%OUTPUT_DIR%." -Prerelease -ExcludeVersion 1>NUL 2>&1 || echo Failed to install nuget package

exit /b 0
