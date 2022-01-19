@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\build.ps1""" -build -restore -rebuild -pack -test -runAnalyzers -warnAsError %*"
