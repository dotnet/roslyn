@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\prepare-tests.ps1" %* 

