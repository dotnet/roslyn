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
    [string]$sourcePath = $null,
    [string]$binariesPath = $null
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Get-PackagesPath {
    $packagesPath = $env:NUGET_PACKAGES
    if ($packagesPath -eq $null) {
        $packagesPath = Join-Path $env:UserProfile ".nuget\packages\"
    }

    return $packagesPath
}

Push-Location $sourcePath
try {

    # Need to parse out the current NuGet package version of Structured Logger
    [xml]$deps = Get-Content (Join-Path $sourcePath "build\Targets\Dependencies.props")
    $structuredLoggerVersion = $deps.Project.PropertyGroup.MicrosoftBuildLoggingStructuredLoggerVersion
    $packagesPath = Get-PackagesPath
    $structuredLoggerPath = Join-Path $packagesPath "Microsoft.Build.Logging.StructuredLogger\$structuredLoggerVersion\lib\net46\StructuredLogger.dll"
    $logPath = Join-Path $binariesPath "build.xml"

    Write-Host "Building Roslyn.sln with logging support"
    Exec { & msbuild /v:m /m /logger:StructuredLogger`,$structuredLoggerPath`;$logPath /nodeReuse:false /p:DeployExtension=false Roslyn.sln }
    Write-Host ""

    # Verify the state of our various build artifacts
    Write-Host "Running BuildBoss"
    $buildBossPath = Join-Path $binariesPath "Exes\BuildBoss\BuildBoss.exe"
    Exec { & $buildBossPath Roslyn.sln Compilers.sln src\Samples\Samples.sln CrossPlatform.sln "build\Targets" $logPath }
    Write-Host ""

    # Verify the state of our project.jsons
    Write-Host "Running RepoUtil"
    $repoUtilPath = Join-Path $binariesPath "Exes\RepoUtil\RepoUtil.exe"
    Exec { & $repoUtilPath verify }
    Write-Host ""

    # Verify the state of our generated syntax files
    Write-Host "Checking generated compiler files"
    Exec { & (Join-Path $PSScriptRoot "generate-compiler-code.ps1") -test }

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
