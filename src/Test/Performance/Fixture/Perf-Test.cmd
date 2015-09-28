@echo off
setlocal enabledelayedexpansion

echo Hello from the Roslyn XUnit Performance test.
echo Command line: %0 %*
echo Running from %CD%
ver

rem =========================================================
rem Create a new local Results folder (delete any existing)
rem Note: The for statement is used to resolve the relative
rem path to an absolute path.
rem Note that the results are relative to the current working
rem directory. In Helix, the CWD is the "Exec" subfolder
rem beneath the work item folder.
rem =========================================================
for /f %%i in ("..\Results") do set RESULTS=%%~fi
if exist %RESULTS% rd /s /q %RESULTS%
md %RESULTS%

set > %RESULTS%\Environment.txt

rem =========================================================
rem If no extra arguments are on the command line, then look
rem for all performance test assemblies.
rem =========================================================
set TEST_ASSEMBLIES=%*
if "%TEST_ASSEMBLIES%" == "" (
    set TEST_ASSEMBLIES=*.PerformanceTests.dll
)

if not exist %~dp0ZipResult.py (
    echo ERROR: ZipResult.py missing
    goto :eof
)

if not exist %~dp0UploadResult.py (
    echo ERROR: UploadResult.py missing
    goto :eof
)

rem ========================================================
rem Workaround for PYTHONPATH
rem Helix sets PYTHONPATH to the path to python.exe
rem However, Python uses PYTHONPATH as a search path to
rem locate additional libraries (imports)
rem ========================================================
if DEFINED PYTHONPATH (
    if "%PYTHONPATH:~-4%" == ".exe" (
        set PYTHON=%PYTHONPATH%
        set PYTHONPATH=%HELIX_SCRIPT_ROOT%
    )
) else (
    set PYTHONPATH=%HELIX_SCRIPT_ROOT%
)

rem ========================================================
rem Find the drop folder location.
rem We could use HELIX_CORRELATION_PAYLOAD, but that usually
rem has the extended path prefix \\?\ on it which breaks all
rem sorts of scenarios. So, instead just move to the parent
rem of the folder where this rem script is running.
rem The 'for' loop here is used just to resolve the relative
rem path.
rem ========================================================
for /f %%i in ("%~dp0..") do set DROP=%%~fi

rem ========================================================
rem Run the tests from the root of the drop directory.
rem This allows us to use relative path names in the test
rem runner command line making it considerably shorter.
rem ========================================================
pushd %DROP%

rem ========================================================
rem Prepare to run XUNIT tests
rem ========================================================
set XUNIT=xunit
if not exist %XUNIT% (

    rem ===================================================
    rem Try to create the xunit fixture ourselves. This is
    rem useful for local runs.
    rem ===================================================
    if exist ..\..\nuget.exe (
        echo Note: xunit fixture not found. Trying to create it.
        set XUNIT_PACKAGES=%TEMP%\xunit%RANDOM%
        ..\..\nuget.exe install -OutputDirectory !XUNIT_PACKAGES! -NonInteractive -ExcludeVersion Microsoft.DotNet.xunit.performance.runner.Windows -Version 1.0.0-alpha-build0013 -Source https://www.myget.org/F/dotnet-buildtools/

        xcopy /s /i /y !XUNIT_PACKAGES!\xunit.runner.console\tools\* %XUNIT%
        xcopy /s /i /y !XUNIT_PACKAGES!\Microsoft.DotNet.xunit.performance.runner.Windows\tools\* %XUNIT%
    ) else (
        echo ERROR: xunit fixture not found
        popd
        goto :eof
    )
)

set XUNIT_CONSOLE=%XUNIT%\xunit.console.exe
set XUNIT_RUNNER=%XUNIT%\xunit.performance.run.exe

if not exist %XUNIT_CONSOLE% (
    echo ERROR: %XUNIT_CONSOLE% not found
    popd
    goto :eof
)

if not exist %XUNIT_RUNNER% (
    echo ERROR: %XUNIT_RUNNER% not found
    popd
    goto :eof
)

echo Running tests from %CD%

if defined HELIX_CORRELATION_ID (
    set RUNID=%HELIX_CORRELATION_ID%
) else (
    set RUNID=%COMPUTERNAME%_%USERNAME%_%DATE:~10%%DATE:~4,2%%DATE:~7,2%T%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
)

for %%f in (%TEST_ASSEMBLIES%) do (

    if not exist %%f (
        echo ERROR: Cannot find %%f in %CD%
    ) else (

    %XUNIT_RUNNER% %%f -verbose -runner %XUNIT_CONSOLE% -runnerargs "-verbose" -outdir %RESULTS% -runid %RUNID%

    rem ====== Workaround for https://github.com/Microsoft/xunit-performance/issues/73
    rem The output filename is tied to the runid. However, we want distinct output files
    rem for each test assembly.
    for %%g in (%RESULTS%\%RUNID%.*) do (ren %%g %%~nf%%~xg)

    if exist %RESULTS%\%%~nf.etl (
        echo Zipping %%~nf.etl
        pushd %RESULTS%
        %PYTHON% %~dp0ZipResult.py %%~nf.etl %%~nf.etl.zip
        del %%~nf.etl
        popd
    )
  )
)

popd

echo All tests done.
echo Results in %RESULTS%

rem ========================================================
rem Upload Results if running under HELIX
rem ========================================================
if DEFINED HELIX_RESULTS_CONTAINER_URI (
    for %%f in (%RESULTS%\*) do (
       echo Uploading %%f
       %PYTHON% %~dp0UploadResult.py %%f %%~nxf
    )
)
