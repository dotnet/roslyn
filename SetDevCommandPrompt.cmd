@echo off

:: prefer building with Dev15

SET

set CommonToolsDir=%VS150COMNTOOLS%
if not exist "%CommonToolsDir%" set CommonToolsDir=%VS140COMNTOOLS%
if not exist "%CommonToolsDir%" exit /b 1

call "%CommonToolsDir%\VsDevCmd.bat"
