@echo off
set NuGetExe="%~dp0NuGet.exe"

echo Restoring packages: Toolsets
call %NugetExe% restore -verbosity quiet "%~dp0build\ToolsetPackages\project.json" -configfile "%~dp0nuget.config"

echo Restoring packages: Samples
call %NugetExe% restore -verbosity quiet "%~dp0src\Samples\Samples.sln" -configfile "%~dp0nuget.config"

echo Restoring packages: Roslyn (this may take some time)
call %NugetExe% restore -verbosity quiet "%~dp0Roslyn.sln" -configfile "%~dp0nuget.config"