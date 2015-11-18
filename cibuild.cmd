@setlocal enabledelayedexpansion

REM Parse Arguments.

set NugetZipUrlRoot=https://dotnetci.blob.core.windows.net/roslyn
set NugetZipUrl=%NuGetZipUrlRoot%/nuget.35.zip
set RoslynRoot=%~dp0
set BuildConfiguration=Debug
set BuildRestore=false

REM Because override the C#/VB toolset to build against our LKG package, it is important
REM that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise, 
REM we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
set MSBuildAdditionalCommandLineArgs=/nologo /v:m /m /nodeReuse:false

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/test32" set Test64=false&&shift&& goto :ParseArguments
if /I "%1" == "/test64" set Test64=true&&shift&& goto :ParseArguments
if /I "%1" == "/perf" set Perf=true&&shift&& goto :ParseArguments
if /I "%1" == "/restore" set BuildRestore=true&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if defined Perf (
  if defined Test64 (
    echo ERROR: Cannot combine /perf with either /test32 or /test64
    call :Usage && exit /b 1
  )

  if "%BuildConfiguration%" == "Debug" (
    echo Warning: Running perf tests on a Debug build is not recommended. Use /release for a Release build.
  )
)

call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat" || goto :BuildFailed

REM Restore the NuGet packages 
if "%BuildRestore%" == "true" (
    call "%RoslynRoot%\Restore.cmd" || goto :BuildFailed
) else (
    powershell -noprofile -executionPolicy RemoteSigned -command "%RoslynRoot%\build\scripts\restore.ps1 %NugetZipUrl%" || goto :BuildFailed
)

REM Set the build version only so the assembly version is set to the semantic version,
REM which allows analyzers to laod because the compiler has binding redirects to the
REM semantic version
msbuild %MSBuildAdditionalCommandLineArgs% /p:BuildVersion=0.0.0.0 %RoslynRoot%build/Toolset.sln /p:NuGetRestorePackages=false /p:Configuration=%BuildConfiguration% || goto :BuildFailed

if not exist "%RoslynRoot%Binaries\Bootstrap" mkdir "%RoslynRoot%Binaries\Bootstrap" || goto :BuildFailed
move "Binaries\%BuildConfiguration%\*" "%RoslynRoot%Binaries\Bootstrap" || goto :BuildFailed
copy "build\scripts\*" "%RoslynRoot%Binaries\Bootstrap" || goto :BuildFailed

REM Clean the previous build
msbuild %MSBuildAdditionalCommandLineArgs% /t:Clean build/Toolset.sln /p:Configuration=%BuildConfiguration%  || goto :BuildFailed

call :TerminateCompilerServer

if defined Perf (
  set Target=Build
) else (
  set Target=BuildAndTest
)

msbuild %MSBuildAdditionalCommandLineArgs% /p:BootstrapBuildPath=%RoslynRoot%Binaries\Bootstrap BuildAndTest.proj /t:%Target% /p:Configuration=%BuildConfiguration% /p:Test64=%Test64% || goto :BuildFailed

call :TerminateCompilerServer

REM Verify that our project.lock.json files didn't change as a result of 
REM restore.  If they do then the commit changed the dependencies without 
REM updating the lock files.
REM git diff --exit-code --quiet
REM if ERRORLEVEL 1 (
REM    echo Commit changed dependencies without updating project.lock.json
REM    git diff --exit-code
REM    exit /b 1
REM )

if defined Perf (
  if DEFINED JenkinsCIPerfCredentials (
    powershell .\ciperf.ps1 -BinariesDirectory %RoslynRoot%Binaries\%BuildConfiguration% %JenkinsCIPerfCredentials% || goto :BuildFailed
  ) else (
    powershell .\ciperf.ps1 -BinariesDirectory %RoslynRoot%Binaries\%BuildConfiguration% -StorageAccountName roslynscratch -StorageContainer drops -SCRAMScope 'Roslyn\Azure' || goto :BuildFailed
  )
)

REM Ensure caller sees successful exit.
exit /b 0

:Usage
@echo Usage: cibuild.cmd [/debug^|/release] [/test32^|/test64^|/perf]
@echo   /debug   Perform debug build.  This is the default.
@echo   /release Perform release build.
@echo   /test32  Run unit tests in the 32-bit runner.  This is the default.
@echo   /test64  Run units tests in the 64-bit runner.
@echo   /perf    Submit a job to the performance test system. Usually combined
@echo            with /release. May not be combined with /test32 or /test64.
@echo.
@goto :eof

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
call :TerminateCompilerServer
exit /b 1

:TerminateCompilerServer
@REM Kill any instances VBCSCompiler.exe to release locked files, ignoring stderr if process is not open
@REM This prevents future CI runs from failing hile trying to delete those files.

taskkill /F /IM vbcscompiler.exe 2> nul
