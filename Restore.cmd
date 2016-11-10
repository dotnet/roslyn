@echo off
@setlocal enabledelayedexpansion

set RoslynRoot=%~dp0
set DevDivPackages=%RoslynRoot%src\Setup\DevDivPackages

:ParseArguments
if /I "%1" == "/?" goto :Usage
if /I "%1" == "/clean" set RestoreClean=true&&shift&& goto :ParseArguments
if /I "%1" == "/fast" set RestoreFast=true&&shift&& goto :ParseArguments
goto :DoneParsing

:DoneParsing

REM Allow for alternate solutions to be passed as restore targets.
set RoslynSolution=%1
if "%RoslynSolution%" == "" set RoslynSolution=%RoslynRoot%\Roslyn.sln

REM Load in the inforation for NuGet
call "%RoslynRoot%build\scripts\LoadNuGetInfo.cmd" || goto :LoadNuGetInfoFailed

if "%RestoreClean%" == "true" (
    echo Clearing the NuGet caches
    call "%NugetExe%" locals all -clear || goto :CleanFailed
)

if "%RestoreFast%" == "" (
    echo Deleting project.lock.json files
    pushd "%RoslynRoot%src"
    echo "Dummy lock file to avoid error when there is no project.lock.json file" > project.lock.json
    del /s /q project.lock.json > nul
    popd
)

echo Restoring packages: Toolsets
call "%NugetExe%" restore "%RoslynRoot%build\ToolsetPackages\project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Toolsets (Dev14 VS SDK build tools)
call "%NugetExe%" restore "%RoslynRoot%build\ToolsetPackages\dev14.project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Toolsets (Dev15 VS SDK build tools)
call "%NugetExe%" restore "%RoslynRoot%build\ToolsetPackages\dev15.project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Toolsets (Dev15 VS SDK 'Willow' build tools)
call "%NugetExe%" restore "%RoslynRoot%build\ToolsetPackages\dev15Willow.project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Toolsets (Dev15 VS SDK RC build tools)
call "%NugetExe%" restore "%RoslynRoot%build\ToolsetPackages\dev15rc.project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Roslyn SDK
call "%NugetExe%" restore "%RoslynRoot%build\ToolsetPackages\roslynsdk.project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Locating MSBuild for Solution restore
call "%RoslynRoot%SetDevCommandPrompt.cmd" || goto :RestoreFailed

REM If we have an applocal copy of MSBuild, pass it to NuGet.  Otherwise, assume NuGet knows how to find it.
if exist "%DevenvDir%\..\..\MSBuild\15.0\Bin\MSBuild.exe" (
    set NuGetAdditionalCommandLineArgs=%NuGetAdditionalCommandLineArgs% -msbuildpath "%DevenvDir%\..\..\MSBuild\15.0\Bin"
)

echo Restoring packages: Samples
call "%NugetExe%" restore "%RoslynRoot%src\Samples\Samples.sln" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Templates
call "%NugetExe%" restore "%RoslynRoot%src\Setup\Templates\Templates.sln" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Toolset
call "%NugetExe%" restore "%RoslynRoot%build\Toolset\Toolset.csproj" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: Roslyn (this may take some time)
call "%NugetExe%" restore "%RoslynSolution%" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

echo Restoring packages: DevDiv tools
call "%NugetExe%" restore "%RoslynRoot%src\Setup\DevDivInsertionFiles\DevDivInsertionFiles.sln" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed
call "%NugetExe%" restore "%DevDivPackages%\Roslyn\project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed
call "%NugetExe%" restore "%DevDivPackages%\Debugger\project.json" %NuGetAdditionalCommandLineArgs% || goto :RestoreFailed

exit /b 0

:CleanFailed
echo Clean failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1

:RestoreFailed
echo Restore failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1

:LoadNuGetInfoFailed
echo Error loading NuGet.exe information %ERRORLEVEL%
exit /b 1

:Usage
@echo Usage: Restore.cmd /clean [Solution File]
exit /b 1
