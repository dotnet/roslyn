set ROSLYN_DIR=%~dp0..\..\..\Binaries\Release

rem Generate the HelloWorld.cs file

if exist HelloWorld.cs del HelloWorld.cs

echo using static System.Console;			>> HelloWorld.cs
echo class Hello {					>> HelloWorld.cs
echo     static void Main(string[] args) {		>> HelloWorld.cs
echo         WriteLine("Hello, World!");		>> HelloWorld.cs
echo     }						>> HelloWorld.cs
echo }							>> HelloWorld.cs

if exist Profile.etl del Profile.etl
if exist Report.xml del Report.xml

rem Warmup iteration
%ROSLYN_DIR%\csc.exe /target:exe HelloWorld.cs

rem Run ten iterations

EventTracer.exe -M Start -T "C# Compiler Throughput. Compile HelloWorld.cs 10 times" -D Profile.etl

FOR /L %%i IN (1,1,10) DO (
    %ROSLYN_DIR%\csc.exe /target:exe HelloWorld.cs
)

EventTracer.exe -M Stop -T "C# Compiler Throughput. Compile HelloWorld.cs 10 times" -D Profile.etl -P csc -X Report.xml
