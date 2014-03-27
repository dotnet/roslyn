@echo off

setlocal
rem -----------------------------------------------
rem is batch executing with adiMistrator privileges
rem
rem -----------------------------------------------
set filename=%windir%\$%random%$
(@echo Hello, this is the administrator >%filename% ) 2> nul && (del %filename% & set elevated=1) || (set elevated=0)
if %elevated% == 0 (
    echo This script requires an elevated command shell to run.
    echo finished
    goto :eof
    )

if "%targetVS%"=="" set targetVS=12.0
if "%targetHive%"=="" set targetHive=Roslyn

set PreviewVersion=0.6.40308.1
set unconfigure=false
set verification=-Vr 
set label=Disable

echo Prepare for Roslyn Open Source development
echo.

echo Create or clean the RoslynDev addin hive
echo.

if "%VSSDK120Install%" == "" ( 
    echo ERROR!   To build and debug the .Net compilers Open source projects requires the latest Visual Studio SDK.
    echo.
    set errorlevel=1
    goto :eof
    )

if /I "%1"=="u" (
    set unconfigure=true
    set verification=-Vu
    set label=Enable
    )

if not "%unconfigure%" == "true" (
    set unconfigure=false
    set verification=-Vr 
    set label=Disable
    )

if not "%unconfigure%" == "true" (
    "%VSSDK120Install%\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe" /Reset /VSInstance=%targetVS% /RootSuffix=%TargetHive%  2> nul


	msbuild %~dp0\prepare.msbuild
    )

call :DisableVerificationFor Microsoft.CodeAnalysis.dll
call :DisableVerificationFor Microsoft.CodeAnalysis.CSharp.dll
call :DisableVerificationFor Microsoft.CodeAnalysis.VisualBasic.dll
call :DisableVerificationFor Microsoft.CodeAnalysis.Workspaces.dll
call :DisableVerificationFor Microsoft.CodeAnalysis.CSharp.Workspaces.dll
call :DisableVerificationFor Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll
call :DisableVerificationFor CompilerPackage.dll
call :DisableVerificationFor Roslyn.Compilers.BuildTasks.dll
call :DisableVerificationFor rcsc.exe
call :DisableVerificationFor rvbc.exe
call :DisableVerificationFor VBCSCompiler.exe

rem List skip verification entries
echo.
sn -q -Vl

if "%unconfigure%" == "true" (
    rem Reset the RoslynDev hive from the main hive, 
    "%VSSDK120Install%\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe" /Clean /VSInstance=%targetVS% /RootSuffix=%TargetHive%  2> nul
    reg delete HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\%targetVS%%TargetHive% /F > nul 2>&1
    reg delete HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\%targetVS%%TargetHive%_Config /F > nul 2>&1
    )

goto :eof

:DisableVerificationFor %1
@echo %label% verification for %1
for /F %%f in ('dir /s /b "%LOCALAPPDATA%\Microsoft\VisualStudio\%targetVS%%TargetHive%\%1"') do sn -q %verification% "%%f" && goto :eof
goto :eof
