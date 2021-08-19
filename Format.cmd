@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\format.ps1""" %*"
