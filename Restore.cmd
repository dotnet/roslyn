@echo off
@setlocal

set NuGetExe="%~dp0NuGet.exe"

REM If someone passed in a different Roslyn solution, use that.
REM We make use of this when Roslyn is an sub-module for some 
REM internal repositories.
set RoslynSolution=%1
if "%RoslynSolution%" == "" set RoslynSolution=%~dp0Roslyn.sln

echo Restoring packages: Toolsets
call %NugetExe% restore -verbosity quiet "%~dp0build\ToolsetPackages\project.json" -configfile "%~dp0nuget.config" || goto :RestoreFailed

echo Restoring packages: Samples
call %NugetExe% restore -verbosity quiet "%~dp0src\Samples\Samples.sln" -configfile "%~dp0nuget.config" || goto :RestoreFailed

echo Restoring packages: Syntax Vizualizer Components
call %NugetExe% restore -verbosity quiet "%~dp0src\Tools\Source\SyntaxVisualizer\SyntaxVisualizerControl\project.json" -configfile "%~dp0nuget.config" || goto :RestoreFailed
call %NugetExe% restore -verbosity quiet "%~dp0src\Tools\Source\SyntaxVisualizer\SyntaxVisualizerDgmlHelper\project.json" -configfile "%~dp0nuget.config" || goto :RestoreFailed

echo Restoring packages: Roslyn (this may take some time)
call %NugetExe% restore -verbosity quiet "%RoslynSolution%" -configfile "%~dp0nuget.config" || goto :RestoreFailed

exit /b 0

:RestoreFailed
echo Restore failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1