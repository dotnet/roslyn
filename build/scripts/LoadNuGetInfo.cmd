@echo off

REM This is a script that will set environment information about where to find
REM NuGet.exe, it's version and ensure that it's present on the enlistment.
set NuGetExeVersion=3.5.0-beta2
set NuGetExeFolder=%~dp0..\..
set NuGetExe=%NuGetExeFolder%\NuGet.exe

REM Download NuGet.exe if we haven't already
if not exist "%NuGetExe%" (
    echo Downloading NuGet %NuGetExeVersion%
    powershell -noprofile -executionPolicy Bypass -file "%~dp0download-nuget.ps1" "%NuGetExeVersion%" "%NuGetExeFolder%" || goto :DownloadNuGetFailed
)

