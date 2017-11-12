@echo off
if defined DOTNET_HOST_PATH (
    set HOST_PATH=%DOTNET_HOST_PATH%
) else (
    set HOST_PATH=dotnet
)
"%HOST_PATH%" "%~dp0\csc.dll" %*
