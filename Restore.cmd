@echo off
@setlocal

set NuGetExe="%~dp0NuGet.exe"
set NuGetAdditionalCommandLineArgs=-verbosity quiet -configfile "%~dp0nuget.config" -Project2ProjectTimeOut 1200

REM If someone passed in a different Roslyn solution, use that.
REM We make use of this when Roslyn is an sub-module for some 
REM internal repositories.
set RoslynSolution=%1
if "%RoslynSolution%" == "" set RoslynSolution=%~dp0Roslyn.sln

echo Restoring packages: Toolsets
call %NugetExe% restore "%~dp0build\ToolsetPackages\project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Samples
call %NugetExe% restore "%~dp0src\Samples\Samples.sln" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Roslyn (this may take some time)
call %NugetExe% restore "%RoslynSolution%" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

exit /b 0

:RestoreFailed
echo Restore failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1