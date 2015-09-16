@echo off
setlocal

echo Hello from the Roslyn XUnit Performance test.
echo Command line: %0 %*
echo Running from %CD%
ver

rem ========================================================
rem Create a new local Results folder (delete any existing)
rem ========================================================
set LOCALRESULTS=Results
if exist %LOCALRESULTS% rd /s /q %LOCALRESULTS%
md %LOCALRESULTS%

set > %LOCALRESULTS%\Environment.txt

if '%1'=='' (
    echo ERROR: Must specify at least one test assembly on the command line.
    goto :eof
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
rem However, Python uses PYTHONPATH to locate additional imports
rem ========================================================
if '%PYTHONPATH:~-4%' == '.exe' (
  set PYTHONTOOLPATH=%PYTHONPATH%
  set PYTHONPATH=%HELIX_SCRIPT_ROOT%
)

rem ========================================================
rem Prepare to run XUNIT tests
rem ========================================================
if not '%HELIX_CORRELATION_PAYLOAD%' == '' (
  rem The HELIX_CORRELATION_PAYLOAD contains the extended path prefix \\?\
  if '%HELIX_CORRELATION_PAYLOAD:~0,4%' == '\\?\' (
    set XUNIT_CONSOLE=%HELIX_CORRELATION_PAYLOAD:~4%\xunit.console.exe
    set XUNIT_RUNNER=%HELIX_CORRELATION_PAYLOAD:~4%\xunit.performance.run.exe
  ) else (
    set XUNIT_CONSOLE=%HELIX_CORRELATION_PAYLOAD%\xunit.console.exe
    set XUNIT_RUNNER=%HELIX_CORRELATION_PAYLOAD%\xunit.performance.run.exe
  )
) else (
  set XUNIT_CONSOLE="%~dp0xunit.console.exe"
  set XUNIT_RUNNER="%~dp0xunit.performance.run.exe"
)

if not exist %XUNIT_RUNNER% (
    echo ERROR: xunit.performance.run.exe missing
    goto :eof
)

echo xunit performance runner found at %XUNIT_RUNNER%
echo xunit console runner found at %XUNIT_CONSOLE%

echo Running tests...

for %%f in (%*) do (
  if not exist %%f (
      echo ERROR: Cannot find %%f in %CD%
  ) else (
    %XUNIT_RUNNER% %%f -verbose -runner %XUNIT_CONSOLE% -runnerargs "-verbose" -outdir %LOCALRESULTS% -runid %%~nf

    if exist %LOCALRESULTS%\%%~nf.etl (
      echo Zipping %%~nf.etl
       pushd %LOCALRESULTS%
       %PYTHONTOOLPATH% %~dp0ZipResult.py %%~nf.etl %%~nf.etl.zip
       del %%~nf.etl
       popd
    )
  )
)

echo All tests done.

rem ========================================================
rem Upload Results if running under HELIX
rem ========================================================
if not '%HELIX_RESULTS_CONTAINER_URI%' == '' (
  for %%f in (%LOCALRESULTS%\*) do (
     echo Uploading %%f
     %PYTHONTOOLPATH% %~dp0UploadResult.py %%f %%~nxf
  )
)
