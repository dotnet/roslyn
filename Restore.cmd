@if not defined EchoOn @echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\restore-legacy.ps1" %*

