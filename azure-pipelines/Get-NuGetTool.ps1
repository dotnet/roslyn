<#
.SYNOPSIS
    Downloads the NuGet.exe tool and returns the path to it.
.PARAMETER NuGetVersion
    The version of the NuGet tool to acquire.
#>
Param(
    [Parameter()]
    [string]$NuGetVersion='6.4.0'
)

$toolsPath = & "$PSScriptRoot\Get-TempToolsPath.ps1"
$binaryToolsPath = Join-Path $toolsPath $NuGetVersion
if (!(Test-Path $binaryToolsPath)) { $null = mkdir $binaryToolsPath }
$nugetPath = Join-Path $binaryToolsPath nuget.exe

if (!(Test-Path $nugetPath)) {
    Write-Host "Downloading nuget.exe $NuGetVersion..." -ForegroundColor Yellow
    (New-Object System.Net.WebClient).DownloadFile("https://dist.nuget.org/win-x86-commandline/v$NuGetVersion/NuGet.exe", $nugetPath)
}

return (Resolve-Path $nugetPath).Path
