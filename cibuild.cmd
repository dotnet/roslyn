@setlocal enabledelayedexpansion

REM Parse Arguments.

set RoslynRoot=%~dp0
set BuildConfiguration=Debug

REM Because override the C#/VB toolset to build against our LKG package, it is important
REM that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise, 
REM we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
set MSBuildAdditionalCommandLineArgs=/nologo /m /nodeReuse:false /consoleloggerparameters:Verbosity=minimal /filelogger /fileloggerparameters:Verbosity=normal

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/test32" set Test64=false&&shift&& goto :ParseArguments
if /I "%1" == "/test64" set Test64=true&&shift&& goto :ParseArguments
if /I "%1" == "/testDeterminism" set TestDeterminism=true&&shift&& goto :ParseArguments
if /I "%1" == "/testPerfCorrectness" set TestPerfCorrectness=true&&shift&& goto :ParseArguments

REM /buildTimeLimit is the time limit, measured in minutes, for the Jenkins job that runs
REM the build. The Jenkins script netci.groovy passes the time limit to this script.
if /I "%1" == "/buildTimeLimit" set BuildTimeLimit=%2&&shift&&shift&& goto :ParseArguments

call :Usage && exit /b 1
:DoneParsing

REM This script takes the presence of the /buildTimeLimit option as an indication that it
REM should run the tests under the control of the ProcessWatchdog, which, if the tests
REM exceed the time limit, will take a screenshot, obtain memory dumps from the test
REM process and all its descendants, and shut those processes down.
REM
REM Developers building from the command line will presumably not pass /buildTimeLimit,
REM and so the tests will not run under the ProcessWatchdog.
if not "%BuildTimeLimit%" == "" (
    set CurrentDate=%date%
    set CurrentTime=%time: =0%
    set BuildStartTime=!CurrentDate:~-4!-!CurrentDate:~-10,2!-!CurrentDate:~-7,2!T!CurrentTime!
    set RunProcessWatchdog=true
) else (
    set RunProcessWatchdog=false
)
    
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat" || goto :BuildFailed

powershell -noprofile -executionPolicy RemoteSigned -file "%RoslynRoot%\build\scripts\check-branch.ps1" || goto :BuildFailed

REM Output the commit that we're building, for reference in Jenkins logs
echo Building this commit:
git show --no-patch --pretty=raw HEAD

REM Restore the NuGet packages 
call "%RoslynRoot%\Restore.cmd" || goto :BuildFailed

REM Ensure the binaries directory exists because msbuild can fail when part of the path to LogFile isn't present.
set bindir=%RoslynRoot%Binaries
if not exist "%bindir%" mkdir "%bindir%" || goto :BuildFailed

REM Set the build version only so the assembly version is set to the semantic version,
REM which allows analyzers to load because the compiler has binding redirects to the
REM semantic version
msbuild %MSBuildAdditionalCommandLineArgs% /p:BuildVersion=0.0.0.0 "%RoslynRoot%build\Toolset.sln" /p:NuGetRestorePackages=false /p:Configuration=%BuildConfiguration% /fileloggerparameters:LogFile="%bindir%\Bootstrap.log" || goto :BuildFailed
powershell -noprofile -executionPolicy RemoteSigned -file "%RoslynRoot%\build\scripts\check-msbuild.ps1" "%bindir%\Bootstrap.log" || goto :BuildFailed

if not exist "%bindir%\Bootstrap" mkdir "%bindir%\Bootstrap" || goto :BuildFailed
move "Binaries\%BuildConfiguration%\*" "%bindir%\Bootstrap" || goto :BuildFailed
copy "build\bootstrap\*" "%bindir%\Bootstrap" || goto :BuildFailed

REM Clean the previous build
msbuild %MSBuildAdditionalCommandLineArgs% /t:Clean build/Toolset.sln /p:Configuration=%BuildConfiguration%  /fileloggerparameters:LogFile="%bindir%\BootstrapClean.log" || goto :BuildFailed

call :TerminateBuildProcesses

if defined TestDeterminism (
    powershell -noprofile -executionPolicy RemoteSigned -file "%RoslynRoot%\build\scripts\test-determinism.ps1" "%bindir%\Bootstrap" || goto :BuildFailed
    call :TerminateBuildProcesses
    exit /b 0
)

if defined TestPerfCorrectness (
    msbuild %MSBuildAdditionalCommandLineArgs% Roslyn.sln /p:Configuration=%BuildConfiguration% || goto :BuildFailed
    .\Binaries\%BuildConfiguration%\Roslyn.Test.Performance.Runner.exe --ci-test || goto :BuildFailed
    exit /b 0
)

msbuild %MSBuildAdditionalCommandLineArgs% /p:BootstrapBuildPath="%bindir%\Bootstrap" BuildAndTest.proj /p:Configuration=%BuildConfiguration% /p:Test64=%Test64% /p:RunProcessWatchdog=%RunProcessWatchdog% /p:BuildStartTime=%BuildStartTime% /p:"ProcDumpExe=%ProcDumpExe%" /p:BuildTimeLimit=%BuildTimeLimit% /p:PathMap="%RoslynRoot%=q:\roslyn" /p:Feature=pdb-path-determinism /fileloggerparameters:LogFile="%bindir%\Build.log";verbosity=diagnostic || goto :BuildFailed
powershell -noprofile -executionPolicy RemoteSigned -file "%RoslynRoot%\build\scripts\check-msbuild.ps1" "%bindir%\Build.log" || goto :BuildFailed

call :TerminateBuildProcesses

REM Verify that our project.lock.json files didn't change as a result of 
REM restore.  If they do then the commit changed the dependencies without 
REM updating the lock files.
REM git diff --exit-code --quiet
REM if ERRORLEVEL 1 (
REM    echo Commit changed dependencies without updating project.lock.json
REM    git diff --exit-code
REM    exit /b 1
REM )


REM Ensure caller sees successful exit.
exit /b 0

:Usage
@echo Usage: cibuild.cmd [/debug^|/release] [/test32^|/test64] [/restore]
@echo   /debug   Perform debug build.  This is the default.
@echo   /release Perform release build.
@echo   /test32  Run unit tests in the 32-bit runner.  This is the default.
@echo   /test64  Run units tests in the 64-bit runner.
@echo.
@goto :eof

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
call :TerminateBuildProcesses
exit /b 1

:TerminateBuildProcesses
@REM Kill any instances VBCSCompiler.exe to release locked files, ignoring stderr if process is not open
@REM This prevents future CI runs from failing while trying to delete those files.
@REM Kill any instances of msbuild.exe to ensure that we never reuse nodes (e.g. if a non-roslyn CI run
@REM left some floating around).

taskkill /F /IM vbcscompiler.exe 2> nul
taskkill /F /IM msbuild.exe 2> nul
