@echo off
set NuGetExe=%~dp0NuGet.exe

echo Restoring packages: Toolsets (this may take some time)
call %NugetExe% restore -verbosity quiet "%~dp0build\ToolsetPackages\project.json" -configfile "%~dp0nuget.config"

echo Restoring packages: Roslyn.sln (this may take some time)
call %NugetExe% restore -verbosity quiet "%~dp0Roslyn.sln" -configfile "%~dp0nuget.config"