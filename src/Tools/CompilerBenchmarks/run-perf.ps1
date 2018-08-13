[CmdletBinding(PositionalBinding=$false)]
param (
    # By default, the Roslyn dir is expected to be next to this dir
    [string]$roslynDir = "$PSScriptRoot/../../.."
)

Set-Variable -Name LastExitCode 0
Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

try
{
    . "$roslynDir/build/scripts/build-utils.ps1"
    $binariesDir = Resolve-Path "$roslynDir/Binaries"

    # Download dotnet if it isn't already available
    Ensure-DotnetSdk

    $reproPath = "$binariesDir/CodeAnalysisRepro"

    if (-not (Test-Path $reproPath)) {
        $tmpFile = [System.IO.Path]::GetTempFileName()
        Invoke-WebRequest -Uri "https://roslyninfra.blob.core.windows.net/perf-artifacts/CodeAnalysisRepro.zip" -UseBasicParsing -OutFile $tmpFile
        [Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem') | Out-Null
        [IO.Compression.ZipFile]::ExtractToDirectory($tmpFile, $binariesDir)
    }

    dotnet run -c Release $reproPath
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
