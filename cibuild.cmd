@setlocal enabledelayedexpansion

REM Parse Arguments.

set RoslynRoot=%~dp0
set BuildConfiguration=Debug
:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

REM Build the compiler so we can self host it for the full build
src\.nuget\NuGet.exe restore %RoslynRoot%/src/Toolset.sln -packagesdirectory packages
msbuild /nologo /v:m /m %RoslynRoot%/src/Toolset.sln /p:Configuration=%BuildConfiguration%

mkdir %RoslynRoot%\Binaries\Bootstrap
move Binaries\%BuildConfiguration%\* %RoslynRoot%\Binaries\Bootstrap
msbuild /v:m /t:Clean src/Toolset.sln /p:Configuration=%BuildConfiguration%
taskkill /F /IM vbcscompiler.exe

msbuild /v:m /m /p:BootstrapBuildPath=%RoslynRoot%\Binaries\Bootstrap BuildAndTest.proj /p:CIBuild=true /p:Configuration=%BuildConfiguration%
if ERRORLEVEL 1 (
    taskkill /F /IM vbcscompiler.exe
    echo Build failed
    exit /b 1
)

msbuild /v:m /m /p:BootstrapBuildPath=%RoslynRoot%\Binaries\Bootstrap src/Samples/Samples.sln /p:CIBuild=true /p:Configuration=%BuildConfiguration%
if ERRORLEVEL 1 (
    taskkill /F /IM vbcscompiler.exe
    echo Build failed
    exit /b 1
)

REM Kill any instances of VBCSCompiler.exe to release locked files;
REM otherwise future CI runs may fail while trying to delete those files.
taskkill /F /IM vbcscompiler.exe

REM It is okay and expected for taskkill to fail (it's a cleanup routine).  Ensure
REM caller sees successful exit.
exit /b 0

:Usage
@echo Usage: cibuild.cmd [/debug^|/release]
@echo   /debug 	Perform debug build.  This is the default.
@echo   /release Perform release build
@goto :eof