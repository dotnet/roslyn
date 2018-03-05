@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\mark-shipped.ps1" 
