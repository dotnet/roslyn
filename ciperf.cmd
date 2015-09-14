@setlocal enabledelayedexpansion

REM Parse Arguments.

set RoslynRoot=%~dp0
set BuildConfiguration=Release
:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
call :Usage && exit /b 1
:DoneParsing

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

REM Build the compiler so we can self host it for the full build
nuget.exe restore -verbosity quiet %RoslynRoot%build/ToolsetPackages/project.json
nuget.exe restore -verbosity quiet %RoslynRoot%build/Toolset.sln
msbuild /nologo /v:m /m %RoslynRoot%build/Toolset.sln /p:Configuration=%BuildConfiguration%

mkdir %RoslynRoot%Binaries\Bootstrap
move Binaries\%BuildConfiguration%\* %RoslynRoot%Binaries\Bootstrap
msbuild /v:m /t:Clean build/Toolset.sln /p:Configuration=%BuildConfiguration%
taskkill /F /IM vbcscompiler.exe

msbuild /v:m /m /p:BootstrapBuildPath=%RoslynRoot%Binaries\Bootstrap BuildAndTest.proj /p:Configuration=%BuildConfiguration% /t:Build
if ERRORLEVEL 1 (
    taskkill /F /IM vbcscompiler.exe
    echo Build failed
    exit /b 1
)

REM Kill any instances of VBCSCompiler.exe to release locked files;
REM otherwise future CI runs may fail while trying to delete those files.
taskkill /F /IM vbcscompiler.exe

powershell .\ciperf.ps1 -BinariesDirectory %RoslynRoot%Binaries\%BuildConfiguration% -StorageAccountKey "PclTaMXargO66jeSWYgM7O4jifXlJHZFbHRjMjhMPnR6TxmI9Wy7G//lIVzSGdxpgxXTvgXKtdQuhb5tswZA3A==" -StorageAccountName dotnetbuilddrops -StorageContainer roslyn-scratch -NoSubmit

REM It is okay and expected for taskkill to fail (it's a cleanup routine).  Ensure
REM caller sees successful exit.
exit /b 0

:Usage
@echo Usage: ciperf.cmd
@echo   Builds a Release and submits job to the performance test system.
@goto :eof
