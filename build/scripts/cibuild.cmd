@if not defined EchoOn @echo off
REM This is a place holder script.  Eventually the cibuild.cmd in the root will be deleted
REM and this will be the primary file.
call "%~dp0\..\..\cibuild.cmd" %*
