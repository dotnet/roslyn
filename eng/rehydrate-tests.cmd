@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\rehydrate-tests.ps1" %* 