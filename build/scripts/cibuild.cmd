@echo off
REM PROTOTYPE(NullableDogfood): Removed -bootstrap since `master` does not support annotations.
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build.ps1" -cibuild -build -restore -binaryLog %*
