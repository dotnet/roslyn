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

if not exist %~dp0xunit.performance.run.exe (
    echo ERROR: xunit performance runner missing
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
set XUNIT_CONSOLE=%~dp0xunit.console.exe
set XUNIT_RUNNER=%~dp0xunit.performance.run.exe
set TEST_FOLDER=.

for %%f in (%*) do (
  if not exist %TEST_FOLDER%\%%f (
      echo ERROR: Cannot find %%f in %TEST_FOLDER%
  ) else (
    %XUNIT_RUNNER% %TEST_FOLDER%\%%f -verbose -runner %XUNIT_CONSOLE% -runnerargs "-verbose" -outdir %LOCALRESULTS% -runid %%~nf

    if exist %LOCALRESULTS%\%%~nf.etl (
      echo Zipping %%~nf.etl
       pushd %LOCALRESULTS%
       %PYTHONTOOLPATH% %~dp0ZipResult.py %%~nf.etl %%~nf.etl.zip
      del %%~nf.etl
      popd
    )
  )
)

rem ========================================================
rem Upload Results if running under HELIX
rem ========================================================
if not '%HELIX_RESULTS_CONTAINER_URI%' == '' (
  for %%f in (%LOCALRESULTS%\*) do (
     echo Uploading %%f
     %PYTHONTOOLPATH% UploadResult.py %%f %%~nxf
  )
)
