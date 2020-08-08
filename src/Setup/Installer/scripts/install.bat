@echo off
powershell -noprofile -executionPolicy Unrestricted -command "& """%~dp0tools\install.ps1""" %*"
