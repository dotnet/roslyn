@echo off

call %~dp0..\..\..\Restore.cmd

set MSBuild=%ProgramFiles%\MSBuild\14.0\bin\msbuild.exe
if not exist "%MSBuild%" set MSBuild=%ProgramFiles(x86)%\MSBuild\14.0\bin\msbuild.exe
"%MSBuild%" %~dp0..\..\Interactive\csi\csi.csproj /p:Configuration=Release /p:OutDir="%~dp0infra\bin\\"


