@echo off
SETLOCAL
set PS1UnderCmd=1

:: Get the datetime in a format that can go in a filename.
set _my_datetime=%date%_%time%
set _my_datetime=%_my_datetime: =_%
set _my_datetime=%_my_datetime::=%
set _my_datetime=%_my_datetime:/=_%
set _my_datetime=%_my_datetime:.=_%
set CmdEnvScriptPath=%temp%\envvarscript_%_my_datetime%.cmd

powershell.exe -NoProfile -NoLogo -ExecutionPolicy bypass -Command "try { & '%~dpn0.ps1' %*; exit $LASTEXITCODE } catch { write-host $_; exit 1 }"

:: Set environment variables in the parent cmd.exe process.
IF EXIST "%CmdEnvScriptPath%" (
    ENDLOCAL
    CALL "%CmdEnvScriptPath%"
    DEL "%CmdEnvScriptPath%"
)
