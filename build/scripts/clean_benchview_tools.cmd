@echo off
setlocal

set "OUTPUT_DIR=%~1"
if "%OUTPUT_DIR%" == "" (echo Usage: clean_benchview_tools.cmd ^<output directory^> & exit /b 1)

:: Cleaning BenchView Tools
set "TOOLS_DIR=%OUTPUT_DIR%\Microsoft.BenchView.JSONFormat"
if exist "%TOOLS_DIR%" rmdir /S /Q "%TOOLS_DIR%"
if exist "%TOOLS_DIR%" (echo ERROR: Failed to remove "%TOOLS_DIR%". & exit /b 1)

exit /b 0
