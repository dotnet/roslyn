<#
    This script drives the Jenkins verification that our build is correct.  In particular:

        - Our build has no double writes
        - Our project.json files are consistent
        - Our build files are well structured
        - Our solution states are consistent
        - Our generated files are consistent

#>

[CmdletBinding(PositionalBinding=$false)]
param(
    [string]$configuration = "Debug",
    [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
    Write-Host "Usage: test-build-correctness.ps1"
    Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
}

try {
    if ($help) {
        Print-Usage
        exit 0
    }

    $ci = $true

    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $RepoRoot

    Write-Host "Building Roslyn"
    Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -restore -build -ci:$ci -configuration:$configuration -pack -binaryLog -useGlobalNuGetCache:$false -warnAsError:$false -properties "/p:RoslynEnforceCodeStyle=true"}


    # Verify the state of our various build artifacts
    Write-Host "Running BuildBoss"
    $buildBossPath = GetProjectOutputBinary "BuildBoss.exe"
    Exec-Console $buildBossPath "-r `"$RepoRoot`" -c $configuration"
    Write-Host ""

    # Verify the state of our generated syntax files
    Write-Host "Checking generated compiler files"
    Exec-Block { & (Join-Path $PSScriptRoot "generate-compiler-code.ps1") -test -configuration:$configuration }
    Write-Host ""
    
    # Verfiy the state of creating run settings for optprof
    Write-Host "Checking run generation for optprof"

    # set environment variables
    if (-not (Test-Path env:SYSTEM_TEAMPROJECT)) { $env:SYSTEM_TEAMPROJECT = "DevDiv" }
    Write-Host "SYSTEM_TEAMPROJECT = '$env:SYSTEM_TEAMPROJECT'"
    if (-not (Test-Path env:BUILD_REPOSITORY_NAME)) { $env:BUILD_REPOSITORY_NAME = "dotnet/roslyn" }
    Write-Host "SYSTEM_TEAMPROJECT = '$env:BUILD_REPOSITORY_NAME'"
    if (-not (Test-Path env:BUILD_SOURCEBRANCHNAME)) { $env:BUILD_SOURCEBRANCHNAME = "test" }
    Write-Host "BUILD_SOURCEBRANCHNAME = '$env:BUILD_SOURCEBRANCHNAME'"
    if (-not (Test-Path env:BUILD_BUILDID)) { $env:BUILD_BUILDID = "42.42.42.42" }
    Write-Host "BUILD_BUILDID = '$env:BUILD_BUILDID'"
    if (-not (Test-Path env:BUILD_SOURCESDIRECTORY)) { $env:BUILD_SOURCESDIRECTORY = $RepoRoot }
    Write-Host "BUILD_SOURCESDIRECTORY = '$env:BUILD_SOURCESDIRECTORY'"
    if (-not (Test-Path env:BUILD_STAGINGDIRECTORY)) { $env:BUILD_STAGINGDIRECTORY = $ArtifactsDir }
    Write-Host "BUILD_STAGINGDIRECTORY = '$env:BUILD_STAGINGDIRECTORY'"

    # create a fake BootstrapperInfo.json file
    $bootstrapperInfoFolder = Join-Path $env:BUILD_STAGINGDIRECTORY "MicroBuild\Output"
    Create-Directory $bootstrapperInfoFolder
    
    $bootstrapperInfoPath = Join-Path $bootstrapperInfoFolder "BootstrapperInfo.json"
    $bootstrapperInfoContent = "[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Products/42.42.42.42/42.42.42.42""}]"
    $bootstrapperInfoContent >> $bootstrapperInfoPath

    # generate run settings
    Exec-Block { & (Join-Path $PSScriptRoot "createrunsettings.ps1") }
    
    exit 0
}
catch [exception] {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}
