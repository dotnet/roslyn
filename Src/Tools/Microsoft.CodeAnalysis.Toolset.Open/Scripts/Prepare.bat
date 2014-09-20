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

if "%targetVS%"=="" set targetVS=14.0
if "%targetHive%"=="" set targetHive=Roslyn

set PreviewVersion=0.6.40308.1
set unconfigure=false
set verification=-Vr
set label=Disable

echo Prepare for Roslyn Open Source development
echo.

echo Create or clean the RoslynDev addin hive
echo.

if "%VSSDK140Install%" == "" (
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
    "%VSSDK140Install%\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe" /Reset /VSInstance=%targetVS% /RootSuffix=%TargetHive%  2> nul

	msbuild %~dp0\prepare.msbuild
    )

call :DisableVerificationFor "Microsoft.CodeAnalysis.CSharp.Desktop,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.Desktop,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.VisualBasic.Desktop,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.CSharp,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.VisualBasic,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.Workspaces,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.CSharp.Workspaces,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.VisualBasic.Workspaces,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.FxCopAnalyzers,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers,31BF3856AD364E35"
call :DisableVerificationFor "Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers,31BF3856AD364E35"
call :DisableVerificationFor "CompilerPackage,31BF3856AD364E35"
call :DisableVerificationFor "FxCopRulesSetup,31BF3856AD364E35"
call :DisableVerificationFor "Roslyn.Compilers.BuildTasks,31BF3856AD364E35"
call :DisableVerificationFor "csc,31BF3856AD364E35"
call :DisableVerificationFor "vbc,31BF3856AD364E35"
call :DisableVerificationFor "VBCSCompiler,31BF3856AD364E35"

rem List skip verification entries
echo.
if exist "%WindowsSDK_ExecutablePath_x86%"sn.exe echo. && echo x86 verification Entries && "%WindowsSDK_ExecutablePath_x86%sn.exe" -q  -Vl
if exist "%WindowsSDK_ExecutablePath_x64%"sn.exe echo. && echo x64 verification Entries && "%WindowsSDK_ExecutablePath_x64%sn.exe" -q  -Vl
echo.

if "%unconfigure%" == "true" (
    rem Reset the RoslynDev hive from the main hive, 
    "%VSSDK140Install%\VisualStudioIntegration\Tools\Bin\CreateExpInstance.exe" /Clean /VSInstance=%targetVS% /RootSuffix=%TargetHive%  2> nul
    reg delete HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\%targetVS%%TargetHive% /F > nul 2>&1
    reg delete HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\%targetVS%%TargetHive%_Config /F > nul 2>&1
    )

goto :eof

:DisableVerificationFor %1
@echo %label% verification for %1
if exist "%WindowsSDK_ExecutablePath_x86%"sn.exe "%WindowsSDK_ExecutablePath_x86%sn.exe" -q  %verification% %1
if exist "%WindowsSDK_ExecutablePath_x64%"sn.exe "%WindowsSDK_ExecutablePath_x64%sn.exe" -q  %verification% %1