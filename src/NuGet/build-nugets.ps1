[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$release = $false,
    [switch]$restore = $true)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Pack-All([string]$nugetOutDir, [string]$packageKind) {
    foreach ($item in Get-ChildItem *.nuspec) {
        $name = Split-Path -leaf $item
        Write-Host "Packing $name"
        Exec-Console $dotnet "pack -nologo --no-build $packProject /p:EmptyDir=$emptyDir /p:NugetPackageKind=$packageKind /p:NuspecFile=$item /p:NuspecBasePath=$configDir -o $nugetOutDir -bl" 
    }
}

Push-Location $PSScriptRoot
try {
    . (Join-Path $PSScriptRoot "..\..\build\scripts\build-utils.ps1")

    $dotnet = Ensure-DotnetSdk
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $configDir = Join-Path $binariesDir $buildConfiguration
    $packProject = Join-Path $PSScriptroot NuGetProjectPackUtil.csproj

    if ($restore) { 
        Exec-Command $dotnet "restore $packProject"
    }

    # Empty directory for packing explicit empty items in the nuspec
    $emptyDir = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
    Create-Directory $emptyDir
    New-Item -Path (Join-Path $emptyDir "_._") -Type File | Out-Null

    Pack-All "e:\temp\nuget" "prerelease"

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
