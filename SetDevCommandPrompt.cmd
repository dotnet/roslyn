@echo off

:: prefer building with Dev15

set CommonToolsDir=%VS150COMNTOOLS%

:: VS150COMNTOOLS is not set globally any more, so fall back to default preview location before trying Dev14
if not exist "%CommonToolsDir%" set CommonToolsDir=%ProgramFiles(x86)%\\Microsoft Visual Studio\\VS15Preview\\Common7\\Tools\\
if not exist "%CommonToolsDir%" set CommonToolsDir=%VS140COMNTOOLS%
if not exist "%CommonToolsDir%" exit /b 1

call "%CommonToolsDir%\VsDevCmd.bat"
