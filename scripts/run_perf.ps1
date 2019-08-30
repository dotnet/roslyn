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

Invoke-WebRequest -Uri http://dotnetci.blob.core.windows.net/roslyn-perf/cpc.zip -OutFile cpc.zip -UseBasicParsing
[Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem') | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory('cpc.zip', $CPCLocation)

./build/scripts/cibuild.cmd -configuration Release -testPerfRun

if ( -not $? )
{
    echo "perf run failed"
    exit 1
}

./build/scripts/cleanup_perf.ps1 $CPCLocation -ShouldArchive
