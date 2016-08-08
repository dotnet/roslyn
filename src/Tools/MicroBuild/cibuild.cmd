@setlocal enabledelayedexpansion

pushd %~dp0
call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat" || goto :BuildFailed
msbuild /nodereuse:false /p:Configuration=Release /p:SkipTest=true Build.proj || goto :BuildFailed
popd

exit /b 0

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1
