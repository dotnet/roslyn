[CmdletBinding(PositionalBinding=$false)]
param (
    # Configuration
    [switch]$release = $false,
    [switch]$restore = $true,

    # Which packages to create
    [switch]$packPreRelease = $true,
    [switch]$packRelease = $false,
    [switch]$packPerBuildPreRelease = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Pack-All([string]$packageKind) {

    $packDir = Join-Path $nugetOutDir $packageKind
    Create-Directory $packDir

    Write-Host "Packing for $packageKind"

    foreach ($item in Get-ChildItem *.nuspec) {
        $name = Split-Path -leaf $item
        Write-Host "`tPacking $name"
        Exec-Command $dotnet "pack -nologo --no-build $packProject /p:EmptyDir=$emptyDir /p:NugetPackageKind=$packageKind /p:NuspecFile=$item /p:NuspecBasePath=$configDir -o $packDir" | Out-Null
    }
}

Push-Location $PSScriptRoot
try {
    . (Join-Path $PSScriptRoot "..\..\build\scripts\build-utils.ps1")

    $dotnet = Ensure-DotnetSdk
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDir $buildConfiguration
    $nugetOutDir = Join-Path $configDir "NuGet"
    $packProject = Join-Path $PSScriptroot NuGetProjectPackUtil.csproj

    Create-Directory $nugetOutDir

    if ($restore) { 
        Exec-Command $dotnet "restore $packProject"
    }

    # Empty directory for packing explicit empty items in the nuspec
    $emptyDir = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
    Create-Directory $emptyDir
    New-Item -Path (Join-Path $emptyDir "_._") -Type File | Out-Null

    if ($packPreRelease) {
        Pack-All "PreRelease"
    }
    if ($packRelease) { 
        Pack-All "Release"
    }
    if ($packPerBuildPreRelease) {
        Pack-All "PerBuildPreRelease"
    }

    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}
