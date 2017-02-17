@setlocal enabledelayedexpansion

pushd %~dp0

REM Ensure we are in a known-clean state before running the build
call "..\..\..\build\scripts\LoadNuGetInfo.cmd" || goto :BuildFailed
call "%NugetExe%" locals all -clear || goto :BuildFailed

..\..\..\Binaries\Toolset\msbuild\msbuild.exe /nodereuse:false /p:Configuration=Release /p:SkipTest=true Build.proj || goto :BuildFailed
popd

exit /b 0

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1
