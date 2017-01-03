@echo off

REM This is a script that will set environment information about where to find
REM NuGet.exe, it's version and ensure that it's present on the enlistment.
set NuGetExeVersion=3.6.0-beta1
set NuGetExeFolder=%~dp0..\..
set NuGetExe=%NuGetExeFolder%\NuGet.exe
set NuGetAdditionalCommandLineArgs=-verbosity quiet -configfile "%NuGetExeFolder%\nuget.config" -Project2ProjectTimeOut 1200

REM Download NuGet.exe if we haven't already
powershell -noprofile -executionPolicy Bypass -file "%~dp0download-nuget.ps1" "%NuGetExeVersion%" "%NuGetExeFolder%" "%NuGetExeFolder%\Binaries"

