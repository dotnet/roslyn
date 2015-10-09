rem Prep-install required packages
set ROSLYN_ROOT=%~dp0..\..\..
set PERF_DIR=%~dp0
set PERFTOOLS_NAME=Microsoft.DotNet.PerfTools
set PERFTOOLS_VERSION=0.0.1-prerelease-00050
set NUGET_EXE=%ROSLYN_ROOT%\nuget.exe
%NUGET_EXE% install %PERFTOOLS_NAME% -PreRelease -Version %PERFTOOLS_VERSION% -OutputDirectory %ROSLYN_ROOT%\packages -Source https://www.myget.org/F/dotnet-buildtools/api/v2
set PERFTOOLS_DIR=%ROSLYN_ROOT%\packages\Microsoft.DotNet.PerfTools.%PERFTOOLS_VERSION%\tools
set ROSLYN_DIR=%ROSLYN_ROOT%\Binaries\Release

if not exist %ROSLYN_DIR%\csc.exe (
	echo Please build a release version of Roslyn before running this.
	exit /b 1
)

cd %PERF_DIR%

if exist Profile.etl del Profile.etl
if exist Report.xml del Report.xml

rem Warmup iteration
%ROSLYN_DIR%\csc.exe /target:exe HelloWorld.cs
if errorlevel 1 exit /b 1

rem Run ten iterations

%PERFTOOLS_DIR%\EventTracer.exe -M Start -T "C# Compiler Throughput. Compile HelloWorld.cs 10 times" -D Profile.etl
if errorlevel 1 exit /b 1

FOR /L %%i IN (1,1,10) DO (
    %ROSLYN_DIR%\csc.exe /target:exe HelloWorld.cs
	if errorlevel 1 exit /b 1
)

%PERFTOOLS_DIR%\EventTracer.exe -M Stop -T "C# Compiler Throughput. Compile HelloWorld.cs 10 times" -D Profile.etl -P csc -X Report.xml
if errorlevel 1 exit /b 1