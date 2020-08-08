@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\test-determinism.ps1" %*
