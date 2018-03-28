@echo off

REM This is a script that will set environment information about where to find
REM NuGet.exe, it's version and ensure that it's present on the enlistment.
set NuGetExeFolder=%~dp0..\..\Binaries\Tools
set NuGetExe=%NuGetExeFolder%\NuGet.exe
set NuGetAdditionalCommandLineArgs=-verbosity quiet -configfile "%NuGetExeFolder%\nuget.config" -Project2ProjectTimeOut 1200

