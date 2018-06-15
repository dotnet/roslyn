@echo off
powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build.ps1" -cibuild -build -restore -binaryLog %*

REM PROTOTYPE(NullableReferenceTypes): Removed bootstrap because cycles crash CI
REM powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build.ps1" -cibuild -build -restore -bootstrap -binaryLog %*
