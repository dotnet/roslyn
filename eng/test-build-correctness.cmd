@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\test-build-correctness.ps1" %*
