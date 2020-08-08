@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0build.ps1""" -ci -restore -build -bootstrap -pack -sign -publish -binaryLog %*"
