<#
.SYNOPSIS
    Downloads the NuGet.exe tool and returns the path to it.
.PARAMETER NuGetVersion
    The version of the NuGet tool to acquire.
#>
Param(
    [Parameter()]
    [string]$NuGetVersion='5.2.0'
)

$toolsPath = & "$PSScriptRoot\Get-TempToolsPath.ps1"
$binaryToolsPath = Join-Path $toolsPath $NuGetVersion
if (!(Test-Path $binaryToolsPath)) { $null = mkdir $binaryToolsPath }
$nugetPath = Join-Path $binaryToolsPath nuget.exe

if (!(Test-Path $nugetPath)) {
    Write-Host "Downloading nuget.exe $NuGetVersion..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/v$NuGetVersion/NuGet.exe" -OutFile $nugetPath | Out-Null
}

return (Resolve-Path $nugetPath).Path
