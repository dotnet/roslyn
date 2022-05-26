@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\check-for-loc-changes.ps1" %*
