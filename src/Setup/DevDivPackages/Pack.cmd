@echo on
@setlocal

set PACK_FILENAME=%1
set PACK_VERSION=%2
set BASE_DIR=%3
set OUT_DIR=%4

REM Get the information about NuGet.exe
call "%~dp0..\..\..\build\scripts\LoadNuGetInfo.cmd" || goto :LoadNuGetInfoFailed

call %NugetExe% pack %PACK_FILENAME% -Version %PACK_VERSION% -BasePath %BASE_DIR% -OutputDirectory %OUT_DIR% -NoPackageAnalysis

