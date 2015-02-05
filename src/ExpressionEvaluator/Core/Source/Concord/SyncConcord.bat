@echo on
setlocal

if "%1" == "" goto USAGE1
if "%2" == "" goto USAGE2

set QUEUE_NAME=%1
set CONCORDDIR=%~dp0
set DROPDIR=\\cpvsbuild\drops\dev14\%2

REM Search for the latest build number.
for /f %%F in ('dir "%DROPDIR%\raw\?????.00" /b /o:-n') do (
    set LATEST_BUILD_NUMBER=%%F
    goto ENDLOOP1
)

REM Unable to find latest build number.
exit /b 1

:ENDLOOP1

REM Read the existing build number.
for /f %%F in ('type "%CONCORDDIR%\debuggerdrop.txt"') do (
    set EXISTING_BUILD=%%F
    goto ENDLOOP2
)

REM Unable to read the existing build number.
exit /b 2

:ENDLOOP2

REM Don't do anything if we're already on the latest build.
set LATEST_BUILD=%DROPDIR%\raw\%LATEST_BUILD_NUMBER%
if %EXISTING_BUILD% EQU %LATEST_BUILD% exit /b 3

set BINDIR=%LATEST_BUILD%\binaries.x86ret

REM Check for the existence of the files we want to sync (i.e. make sure the build is sufficiently complete).
if not exist "%BINDIR%\InterAPIsCandidates\Debugger\ref\v2.0\ret\Microsoft.VisualStudio.Debugger.Metadata.dll" exit /b 4
if not exist "%BINDIR%\InterAPIsCandidates\Debugger\ref\v2.0\ret\Microsoft.VisualStudio.Debugger.Engine.dll" exit /b 5
if not exist "%BINDIR%\Debugger\IDE\Microsoft.VisualStudio.Debugger.Engine.xml" exit /b 6

REM Also check for files that we are not going to sync, but which we will need for patching VS (e.g. in the lab).
if not exist "%BINDIR%\Debugger\RemoteDebugger\Microsoft.VisualStudio.Debugger.Engine.dll" exit /b 7
if not exist "%BINDIR%\Debugger\RemoteDebugger\Microsoft.VisualStudio.Debugger.Engine.pdb" exit /b 8
if not exist "%BINDIR%\bin\i386\Microsoft.VisualStudio.Debugger.Metadata.dll" exit /b 9
if not exist "%BINDIR%\bin\i386\Microsoft.VisualStudio.Debugger.Metadata.pdb" exit /b 10
if not exist "%DROPDIR%\layouts\x86ret\%LATEST_BUILD_NUMBER%\enu\vs\intellitracefull\dvd\vs_IntelliTraceFull.exe" exit /b 11

call "%VS140COMNTOOLS%\vsvars32.bat"

pushd "%CONCORDDIR%"

REM Get our enlistment to a clean state.
tf scorch /noprompt
tf get

REM Copy the files we care about (if they've changed).
robocopy "%BINDIR%\InterAPIsCandidates\Debugger\ref\v2.0\ret" "%TEMP%\Concord" Microsoft.VisualStudio.Debugger.Metadata.dll /njh /njs /ndl /ns /nc /np
robocopy "%BINDIR%\InterAPIsCandidates\Debugger\ref\v2.0\ret" "%TEMP%\Concord" Microsoft.VisualStudio.Debugger.Engine.dll /njh /njs /ndl /ns /nc /np
robocopy "%BINDIR%\Debugger\IDE" "%CONCORDDIR%\." Microsoft.VisualStudio.Debugger.Engine.xml /njh /njs /ndl /ns /nc /np

ildasm /out=Microsoft.VisualStudio.Debugger.Metadata.il /nobar /visibility:pub+fam /noca "%TEMP%\Concord\Microsoft.VisualStudio.Debugger.Metadata.dll"
if %ERRORLEVEL% NEQ 0 exit /b 12
ildasm /out=Microsoft.VisualStudio.Debugger.Engine.il /nobar /visibility:pub+fam "%TEMP%\Concord\Microsoft.VisualStudio.Debugger.Engine.dll"
if %ERRORLEVEL% NEQ 0 exit /b 13

REM Update debuggerdrop.txt with the latest build number.
echo %LATEST_BUILD% > "%CONCORDDIR%\debuggerdrop.txt"

REM Shelve the changed files.
set SHELVESET_NAME=Sync Concord %LATEST_BUILD_NUMBER% via %QUEUE_NAME%
set SHELVESET_MESSAGE=Automated checkin: update Concord binaries from %EXISTING_BUILD% to version %LATEST_BUILD_NUMBER%.
tf shelve /replace /move "%SHELVESET_NAME%" /comment:"%SHELVESET_MESSAGE%" /noprompt

REM Don't queue a build if shelving failed.
if %ERRORLEVEL% NEQ 0 exit /b 14

REM Queue a gated checkin with our shelveset.
tfsbuild start http://vstfdevdiv:8080/DevDiv2 Roslyn %QUEUE_NAME% "/shelveset:%SHELVESET_NAME%" /queue /checkin

exit /b 0

:USAGE1

echo Missing required TF queue name.
exit /b 15

:USAGE2

echo Missing required branch name (under \\cpvsbuild\drops\dev14).
exit /b 16