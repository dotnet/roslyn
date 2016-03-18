@echo off

call ..\..\..\Restore.cmd

set MSBuild=%ProgramFiles%\MSBuild\14.0\bin\msbuild.exe
if not exist "%MSBuild%" set MSBuild=%ProgramFiles(x86)%\MSBuild\14.0\bin\msbuild.exe
"%MSBuild%" ..\..\Interactive\csi\csi.csproj /p:Configuration=Release /p:OutDir="%~dp0infra\bin\\"


