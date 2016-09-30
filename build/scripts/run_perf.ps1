set-variable -name LastExitCode 0
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

$CPCLocation="C:/CPC"

./build/scripts/cleanup_perf.ps1 $CPCLocation

if ( -not $? )
{
    echo "cleanup failed"
    exit 1
}

Invoke-WebRequest -Uri http://dotnetci.blob.core.windows.net/roslyn-perf/cpc.zip -OutFile cpc.zip
[Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem') | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory('cpc.zip', $CPCLocation)

# Preview 4 specific
[Environment]::SetEnvironmentVariable("VS150COMNTOOLS", "C:\\Program Files (x86)\\Microsoft Visual Studio\\VS15Preview\\Common7\\Tools", "Process")

./cibuild.cmd /testPerfRun /release
