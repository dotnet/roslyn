@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build\scripts\build.ps1" -testDesktop %* 
