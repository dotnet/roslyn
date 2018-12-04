@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0build\scripts\build.ps1""" -restore %*"
