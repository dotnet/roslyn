@echo off
if defined DOTNET_HOST_PATH (
    set HOST_PATH=%DOTNET_HOST_PATH%\
) else (
    set HOST_PATH=
)
%HOST_PATH%dotnet %~dp0\vbc.dll %*
