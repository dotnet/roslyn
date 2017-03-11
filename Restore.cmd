@if not defined EchoOn @echo off
@setlocal enabledelayedexpansion

set RepoRoot=%~dp0
:ParseArguments
if /I "%1" == "/clean" set RestoreClean=-clean&&shift&& goto :ParseArguments
if /I "%1" == "/fast" set RestoreFast=-fast&&shift&& goto :ParseArguments
goto :DoneParsing

:DoneParsing

powershell -noprofile -executionPolicy RemoteSigned -file "%RepoRoot%\build\scripts\restore.ps1" %RestoreFast% %RestoreClean% %*

