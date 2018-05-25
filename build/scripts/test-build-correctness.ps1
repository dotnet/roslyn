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
    [switch]$release = $false,
    [switch]$cibuild = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir
    $buildConfiguration = if ($release) { "Release" } else { "Debug" }
    $releaseArg = if ($release) { "-release" } else { "" }
    $configDir = Join-Path $binariesDir $buildConfiguration

    Write-Host "Building Roslyn"
    Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -restore -build -cibuild:$cibuild -release:$release -pack -binaryLog }


    # Verify the state of our various build artifacts
    Write-Host "Running BuildBoss"
    $buildBossPath = Join-Path $configDir "Exes\BuildBoss\BuildBoss.exe"
    Exec-Console $buildBossPath "Roslyn.sln Compilers.sln SourceBuild.sln -r $repoDir $releaseArg"
    Write-Host ""

    # Verify the state of our generated syntax files
    Write-Host "Checking generated compiler files"
    Exec-Block { & (Join-Path $PSScriptRoot "generate-compiler-code.ps1") -test }

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
