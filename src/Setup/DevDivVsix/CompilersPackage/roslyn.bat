
set __VSCMD_script_err_count=0
if "%VSCMD_TEST%" NEQ "" goto :test
if "%VSCMD_ARG_CLEAN_ENV%" NEQ "" goto :clean_env

@REM ------------------------------------------------------------------------
:start

set "PATH=%VSINSTALLDIR%MSBuild\Current\bin\Roslyn;%PATH%"

goto :end

@REM ------------------------------------------------------------------------
:test

setlocal

@REM Test whether csc.exe is now on PATH
where csc.exe > nul 2>&1
if "%ERRORLEVEL%" NEQ "0" (
    @echo [ERROR:%~nx0] 'where csc.exe' failed
    set /A __VSCMD_script_err_count=__VSCMD_script_err_count+1
)


@REM exports the value of _vscmd_script_err_count from the 'setlocal' region
endlocal & set __VSCMD_script_err_count=%__VSCMD_script_err_count%

goto :end

@REM ------------------------------------------------------------------------
:clean_env

@REM Since this script only adds to PATH, the vsdevcmd.bat core infrastructure
@REM will handle clean-up of PATH.

goto :end

@REM ------------------------------------------------------------------------
:end

@REM return value other than 0 if tests failed.
if "%__VSCMD_script_err_count%" NEQ "0" (
   set __VSCMD_script_err_count=
   exit /B 1
)

set __VSCMD_script_err_count=
exit /B 0
