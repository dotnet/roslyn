@echo off

call "%~dp0..\..\..\Restore.cmd"

set MSBuild=%ProgramFiles%\MSBuild\14.0\bin\msbuild.exe
if not exist "%MSBuild%" set MSBuild=%ProgramFiles(x86)%\MSBuild\14.0\bin\msbuild.exe
"%MSBuild%" "%~dp0..\..\Interactive\csi\csi.csproj" /p:Configuration=Release /p:OutDir="%~dp0infra\bin\\"

if "%USERDNSDOMAIN%" == "REDMOND.CORP.MICROSOFT.COM" (
    if exist "%SYSTEMDRIVE%/CPC" ( rd /s /q "%SYSTEMDRIVE%/CPC" )
    robocopy \\mlangfs1\public\basoundr\CpcBinaries %SYSTEMDRIVE%\CPC /mir 
    robocopy \\mlangfs1\public\basoundr\vibenchcsv2json %SYSTEMDRIVE%\CPC /s 
) else (
    echo "Machine not in Microsoft Corp Net Domain. Hence not downloading internal tools"
)