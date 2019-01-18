@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0sdk-task.ps1""" -msbuildEngine dotnet -restore -projects PublishBuildAssets.proj -ci %*"
exit /b %ErrorLevel%
