@echo off
@setlocal

set RoslynRoot=%~dp0\..\..\
call "%RoslynRoot%build\scripts\LoadNuGetInfo.cmd" || goto :CleanFailed

:ParseArguments
if /I "%1" == "-?" goto :Usage
if /I "%1" == "-b" set ClearBinaries=true&&shift&& goto :ParseArguments
if /I "%1" == "-p" set ClearLocalPackages=true&&shift&& goto :ParseArguments
if /I "%1" == "-c" set ClearUserPackages=true&&shift&& goto :ParseArguments
if /I "%1" == "-all" (
    set ClearLocalPackages=true
    set ClearBinaries=true
    set ClearUserPackages=true
    set ClearingAll=true
    shift
    goto :ParseArguments
)

goto :DoneParsing

:DoneParsing

if "%ClearBinaries%" == "true" (
    echo Clearing the binaries directory
    rd /s /q "%RoslynRoot%/Binaries/" 
)

if "%ClearLocalPackages%" == "true" (
    echo Clearing the local packages directory
    rd /s /q "%RoslynRoot%/packages/"
)

if "%ClearUserPackages%" == "true" (
    echo Clearing the user package cache
    call %NugetExe% locals all -clear || goto :CleanFailed
)

goto :Success

:Usage
echo clean
echo Options:
echo     -b  Deletes the binary output directory
echo     -p  Deletes the local package directory
echo     -c  Deletes the user package cache
echo     -all  Performs all of the above

:Success
exit /b 0

:CleanFailed
exit /b 1
