# Script to run standard developer operations: restore, build and test.  
[CmdletBinding(PositionalBinding=$false)]
param (
    [switch]$build = $false, 
    [switch]$restore = $false,
    [switch]$test = $false,
    [switch]$test64 = $false,
    [switch]$clean = $false,
    [switch]$clearPackageCache = $false,
    [string]$project = "",
    [string]$msbuildDir = "",
    [parameter(ValueFromRemainingArguments=$true)] $badArgs)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
    Write-Host "Build.ps1"
    Write-Host "`t-build                Run a build operation (default false)"
    Write-Host "`t-restore              Run a restore operation (default false)"
    Write-Host "`t-test                 Run unit tests (default false)"
    Write-Host "`t-test64               Run unit tests in 64 bit mode"
    Write-Host "`t-clean                Do a clean build / restore (default false)"
    Write-Host "`t-clearPackageCache    Clear package cache before restoring"
    Write-Host "`t-project <path>       Project the build or restore should target"
    Write-Host "`t-msbuildDir <path>    MSBuild which should be used"
}

function Run-Build() {
    $buildArgs = "/v:m /m"
    if ($clean) {
        $buildArgs = "$buildArgs /t:Rebuild"
    }

    $target = if ($project -ne "") { $project } else { Join-Path $repoDir "Roslyn.sln" }
    $buildArgs = "$buildArgs $target"

    Exec-Command $msbuild $buildArgs | Out-Host
}

function Run-Test() {
    $proj = Join-Path $repoDir "BuildAndTest.proj"
    $args = "/v:m /p:ManualTest=true /t:Test /p:TestDesktop=true $proj"
    if ($test64) { 
        $args += " /p:Test64=true"
    }
    Exec-Command $msbuild $args | Out-Host
}

try {
    if ($badArgs -ne $null) {
        Write-Host "Bad arguments: $badArgs"
        Print-Usage
        exit 1
    }

    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $nuget = Ensure-NuGet
    if ($clearPackageCache) {
        Clear-PackageCache
    }

    if ($msbuildDir -eq "") {
        $msbuildDir = Get-MSBuildDir
    }
    $msbuild = Join-Path $msbuildDir "msbuild.exe"

    if ($restore) {
        Restore-Packages -clean:$clean -msbuildDir $msbuildDir -project $project
    }

    if ($build) {
        Run-Build
    }

    if ($test -or $test64) { 
        Run-Test
    }
}
catch {
  Write-Host $_
  exit 1
}
