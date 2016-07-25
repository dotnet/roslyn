@if "%_echo%" neq "on" echo off
setlocal

set INIT_TOOLS_LOG=%~dp0init-tools.log
if [%PACKAGES_DIR%]==[] set PACKAGES_DIR=%~dp0..\..\packages\
if [%TOOLRUNTIME_DIR%]==[] set TOOLRUNTIME_DIR=%~dp0..\..\Binaries\Tools
set DOTNET_PATH=%TOOLRUNTIME_DIR%\dotnetcli\
if [%DOTNET_CMD%]==[] set DOTNET_CMD=%DOTNET_PATH%dotnet.exe
if [%BUILDTOOLS_SOURCE%]==[] set BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json
set /P BUILDTOOLS_VERSION=< "%~dp0BuildToolsVersion.txt"
set BUILD_TOOLS_PATH=%PACKAGES_DIR%Microsoft.DotNet.BuildTools\%BUILDTOOLS_VERSION%\lib\
set PROJECT_JSON_PATH=%TOOLRUNTIME_DIR%\%BUILDTOOLS_VERSION%
set PROJECT_JSON_FILE=%PROJECT_JSON_PATH%\project.json
set PROJECT_JSON_CONTENTS={ "dependencies": { "Microsoft.DotNet.BuildTools": "%BUILDTOOLS_VERSION%" }, "frameworks": { "netcoreapp1.0": { } } }
set BUILD_TOOLS_SEMAPHORE=%PROJECT_JSON_PATH%\init-tools.completed

:: if force option is specified then clean the tool runtime and build tools package directory to force it to get recreated
if [%1]==[force] (
  if exist "%TOOLRUNTIME_DIR%" rmdir /S /Q "%TOOLRUNTIME_DIR%"
  if exist "%PACKAGES_DIR%Microsoft.DotNet.BuildTools" rmdir /S /Q "%PACKAGES_DIR%Microsoft.DotNet.BuildTools"
)

:: If sempahore exists do nothing
if exist "%BUILD_TOOLS_SEMAPHORE%" (
  echo Tools are already initialized.
  goto :EOF
)

if exist "%TOOLRUNTIME_DIR%" rmdir /S /Q "%TOOLRUNTIME_DIR%"

if NOT exist "%PROJECT_JSON_PATH%" mkdir "%PROJECT_JSON_PATH%"
echo %PROJECT_JSON_CONTENTS% > "%PROJECT_JSON_FILE%"
echo Running %0 > "%INIT_TOOLS_LOG%"

if exist "%DOTNET_CMD%" goto :afterdotnetrestore

echo Installing dotnet cli...
if NOT exist "%DOTNET_PATH%" mkdir "%DOTNET_PATH%"
set /p DOTNET_VERSION=< "%~dp0DotnetCLIVersion.txt"
set DOTNET_ZIP_NAME=dotnet-dev-win-x64.%DOTNET_VERSION%.zip
set DOTNET_REMOTE_PATH=https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/%DOTNET_VERSION%/%DOTNET_ZIP_NAME%
set DOTNET_LOCAL_PATH=%DOTNET_PATH%%DOTNET_ZIP_NAME%
echo Installing '%DOTNET_REMOTE_PATH%' to '%DOTNET_LOCAL_PATH%' >> "%INIT_TOOLS_LOG%"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "$retryCount = 0; $success = $false; do { try { (New-Object Net.WebClient).DownloadFile('%DOTNET_REMOTE_PATH%', '%DOTNET_LOCAL_PATH%'); $success = $true; } catch { if ($retryCount -ge 6) { throw; } else { $retryCount++; Start-Sleep -Seconds (5 * $retryCount); } } } while ($success -eq $false); Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors; if ($AddTypeErrors.Count -eq 0) { [System.IO.Compression.ZipFile]::ExtractToDirectory('%DOTNET_LOCAL_PATH%', '%DOTNET_PATH%') } else { (New-Object -com shell.application).namespace('%DOTNET_PATH%').CopyHere((new-object -com shell.application).namespace('%DOTNET_LOCAL_PATH%').Items(),16) }" >> "%INIT_TOOLS_LOG%"
if NOT exist "%DOTNET_LOCAL_PATH%" (
  echo ERROR: Could not install dotnet cli correctly. See '%INIT_TOOLS_LOG%' for more details.
  exit /b 1
)

:afterdotnetrestore

if exist "%BUILD_TOOLS_PATH%" goto :afterbuildtoolsrestore
echo Restoring BuildTools version %BUILDTOOLS_VERSION%...
echo Running: "%DOTNET_CMD%" restore "%PROJECT_JSON_FILE%" --no-cache --packages %PACKAGES_DIR% --source "%BUILDTOOLS_SOURCE%" >> "%INIT_TOOLS_LOG%"
call "%DOTNET_CMD%" restore "%PROJECT_JSON_FILE%" --no-cache --packages %PACKAGES_DIR% --source "%BUILDTOOLS_SOURCE%" >> "%INIT_TOOLS_LOG%"
if NOT exist "%BUILD_TOOLS_PATH%init-tools.cmd" (
  echo ERROR: Could not restore build tools correctly. See '%INIT_TOOLS_LOG%' for more details.
  exit /b 1
)

:afterbuildtoolsrestore

echo Initializing BuildTools ...
echo Running: "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0..\..\" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%" >> "%INIT_TOOLS_LOG%"
call "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0..\..\" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%" >> "%INIT_TOOLS_LOG%"
set INIT_TOOLS_ERRORLEVEL=%ERRORLEVEL%
if not [%INIT_TOOLS_ERRORLEVEL%]==[0] (
	echo ERROR: An error occured when trying to initialize the tools. Please check '%INIT_TOOLS_LOG%' for more details.
	exit /b %INIT_TOOLS_ERRORLEVEL%
)

:: Create sempahore file
echo Done initializing tools.
echo Init-Tools.cmd completed for BuildTools Version: %BUILDTOOLS_VERSION% > "%BUILD_TOOLS_SEMAPHORE%"
