@setlocal enabledelayedexpansion

REM Parse Arguments.

set NugetZipUrlRoot=https://dotnetci.blob.core.windows.net/roslyn
set NugetZipUrl=%NuGetZipUrlRoot%/nuget.stabilization.26.zip
set RoslynRoot=%~dp0
set BuildConfiguration=Debug
set BuildRestore=false
:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/test32" set Test64=false&&shift&& goto :ParseArguments
if /I "%1" == "/test64" set Test64=true&&shift&& goto :ParseArguments
if /I "%1" == "/restore" set BuildRestore=true&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat"

REM Restore the NuGet packages 
if "%BuildRestore%" == "true" (
    nuget.exe restore -nocache -verbosity quiet %RoslynRoot%build/ToolsetPackages/project.json
    nuget.exe restore -nocache -verbosity quiet %RoslynRoot%build/Toolset.sln
    nuget.exe restore -nocache %RoslynRoot%build\ToolsetPackages\project.json
    nuget.exe restore -nocache %RoslynRoot%Roslyn.sln
    nuget.exe restore -nocache %RoslynRoot%src\Samples\Samples.sln
) else (
    powershell -noprofile -executionPolicy RemoteSigned -command "%RoslynRoot%\build\scripts\restore.ps1 %NugetZipUrl%"
)

REM Set the build version only so the assembly version is set to the semantic version,
REM which allows analyzers to laod because the compiler has binding redirects to the
REM semantic version
msbuild /nologo /v:m /m /p:BuildVersion=0.0.0.0 %RoslynRoot%build/Toolset.sln /p:NuGetRestorePackages=false /p:Configuration=%BuildConfiguration%

mkdir %RoslynRoot%Binaries\Bootstrap
move Binaries\%BuildConfiguration%\* %RoslynRoot%Binaries\Bootstrap
copy build\scripts\* %RoslynRoot%Binaries\Bootstrap

REM Clean the previous build
msbuild /v:m /t:Clean build/Toolset.sln /p:Configuration=%BuildConfiguration%
taskkill /F /IM vbcscompiler.exe

msbuild /v:m /m /p:BootstrapBuildPath=%RoslynRoot%Binaries\Bootstrap BuildAndTest.proj /p:Configuration=%BuildConfiguration% /p:Test64=%Test64%
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
@echo   /test32 Run unit tests in the 32-bit runner.  This is the default.
@echo   /test64 Run units tests in the 64-bit runner.
@goto :eof
