@echo off

:: Prefer building with Dev15 and try the simple route first (we may be running from a DevCmdPrompt already)
set CommonToolsDir=%VS150COMNTOOLS%

:: VS150COMNTOOLS is not set globally any more, so fall back to using the managed query api before trying Dev14
if not exist "%CommonToolsDir%" for /f "usebackq delims=" %%v in (`powershell -noprofile -executionPolicy Bypass -file "%RoslynRoot%build\scripts\locate-vs.ps1"`) do set "CommonToolsDir=%%v"
if not exist "%CommonToolsDir%" set CommonToolsDir=%VS140COMNTOOLS%
if not exist "%CommonToolsDir%" exit /b 1

call "%CommonToolsDir%\VsDevCmd.bat"
exit /b 0
