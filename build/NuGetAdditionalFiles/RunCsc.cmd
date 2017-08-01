echo off
if defined DOTNET_HOST_PATH set HOST_PATH=%DOTNET_HOST_PATH%\
if not defined HOST_PATH set HOST_PATH=
%HOST_PATH%dotnet %~dp0\csc.dll %*
