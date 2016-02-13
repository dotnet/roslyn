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

rem ========================================================
rem This script is running from the root of the drop.
rem We could use HELIX_CORRELATION_PAYLOAD, but that usually
rem has the extended path prefix \\?\ on it which breaks all
rem sorts of scenarios.
rem ========================================================
for /f %%i in ("%~dp0.") do set DROP=%%~fi

rem ========================================================
rem Run the tests from the root of the drop directory.
rem This allows us to use relative path names in the test
rem runner command line making it considerably shorter.
rem ========================================================
pushd %DROP%

rem ========================================================
rem Prepare to run XUNIT tests
rem ========================================================
set FIXTURE=Performance
if not exist %FIXTURE% (

    rem ===================================================
    rem Try to create the xunit fixture ourselves. This is
    rem useful for local runs.
    rem ===================================================
    if exist ..\..\nuget.exe (
        echo Note: xunit performance fixture not found. Trying to create it.
        set PACKAGES=%TEMP%\xunit%RANDOM%
        ..\..\nuget.exe install -OutputDirectory !PACKAGES! -NonInteractive -ExcludeVersion xunit.extensibility.execution -Version 2.1.0 -Source https://www.nuget.org/api/v2/
        ..\..\nuget.exe install -OutputDirectory !PACKAGES! -NonInteractive -ExcludeVersion Microsoft.DotNet.xunit.performance.runner.Windows -Version 1.0.0-alpha-build0023 -Source https://www.myget.org/F/dotnet-buildtools/

        xcopy /s /i /y !PACKAGES!\xunit.runner.console\tools\* %FIXTURE%
        xcopy /s /i /y !PACKAGES!\Microsoft.DotNet.xunit.performance.runner.Windows\tools\* %FIXTURE%
        copy /y !PACKAGES!\xunit.extensibility.core\lib\dotnet\xunit.core.dll %FIXTURE%
        copy /y !PACKAGES!\xunit.extensibility.execution\lib\net45\xunit.execution.desktop.dll %FIXTURE%

    ) else (
        echo ERROR: xunit performance fixture not found
        popd
        goto :eof
    )
)

if not exist %FIXTURE%\xunit.performance.run.exe (
    echo ERROR: xunit.performance.run.exe not found
    popd
    goto :eof
)

if not exist %FIXTURE%\xunit.console.exe (
    echo ERROR: xunit.console.exe not found
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
        %FIXTURE%\xunit.performance.run.exe %%f -verbose -runnerargs "-verbose" -outdir %RESULTS% -outfile %%~nf -runid %RUNID%

        if exist %RESULTS%\%%~nf.etl (
            if defined HELIX_PYTHONPATH (
                if defined HELIX_SCRIPT_ROOT (
                    echo Zipping %%~nf.etl
                    pushd %RESULTS%
                    %HELIX_PYTHONPATH% %HELIX_SCRIPT_ROOT%\zip_script.py -zipFile %%~nf.etl.zip %%~nf.etl
                    del %%~nf.etl
                    popd
                )
            )
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
    if defined HELIX_PYTHONPATH (
        if defined HELIX_SCRIPT_ROOT (
            for %%f in (%RESULTS%\*) do (
                echo Uploading %%f
                %HELIX_PYTHONPATH% %HELIX_SCRIPT_ROOT%\upload_result.py -result %%f -result_name %%~nxf -upload_client_type Blob
            )
        )
    )
)
