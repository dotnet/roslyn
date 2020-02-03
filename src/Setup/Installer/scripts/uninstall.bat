@echo off
powershell -noprofile -executionPolicy Unrestricted -command "& """%~dp0tools\uninstall.ps1""" %*"
