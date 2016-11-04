@echo off

:: Prefer building with Dev15 and try the simple route first (we may be running from a DevCmdPrompt already)
set CommonToolsDir=%VS150COMNTOOLS%

:: VS150COMNTOOLS is not set globally any more, so fall back to using the managed query api before trying Dev14
if not exist "%CommonToolsDir%" for /f "usebackq delims=" %%v in (`powershell -noprofile -executionPolicy Bypass -file "%~dp0build\scripts\locate-vs.ps1"`) do set "CommonToolsDir=%%v"
if not exist "%CommonToolsDir%" set CommonToolsDir=%VS140COMNTOOLS%
if not exist "%CommonToolsDir%" exit /b 1

:: VsDevCmd.bat has new behavior where it will change your working directory to a special folder if you have ever cloned from the VsDevCmd window, push and pop the current directory to workaround this
pushd %~dp0
call "%CommonToolsDir%\VsDevCmd.bat"
popd

exit /b 0
