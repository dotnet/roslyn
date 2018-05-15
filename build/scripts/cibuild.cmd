@echo off
if "%1"=="-testBuildCorrectness" ( 
    REM Temporary work around until the netci.groovy change takes
    call %~dp0\test-build-correctness.cmd -cibuild -release
) else (
    powershell -noprofile -executionPolicy RemoteSigned -file "%~dp0\build.ps1" -cibuild -build -restore -bootstrap -binaryLog %*
)
