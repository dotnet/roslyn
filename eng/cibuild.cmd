@echo off

REM !!!!TEMPORARY DEBUGGING!!!
setlocal
set COREHOST_TRACE=1
REM !!!!!!!!!!!!!!!!!!!!!!!!!!

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0build.ps1""" -ci -restore -build -bootstrap -pack -sign -publish -binaryLog %*"
