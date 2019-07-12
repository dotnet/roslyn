<#
.SYNOPSIS
    Downloads the NuGet.exe tool and returns the path to it.
.PARAMETER NuGetVersion
    The version of the NuGet tool to acquire.
#>
Param(
    [Parameter()]
    [string]$NuGetVersion='5.0.2'
)

$binaryToolsPath = "$PSScriptRoot\..\obj\tools\nuget.$NuGetVersion"
if (!(Test-Path $binaryToolsPath)) { $null = New-Item -Type Directory -Path $binaryToolsPath }
$nugetPath = "$binaryToolsPath\nuget.exe"

if (!(Test-Path $nugetPath)) {
    Write-Host "Downloading nuget.exe $NuGetVersion..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/v$NuGetVersion/NuGet.exe" -OutFile $nugetPath | Out-Null
}

Write-Output (Resolve-Path $nugetPath).Path
