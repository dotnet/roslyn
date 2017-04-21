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
    [string]$config = "",
    [string]$msbuild = ""
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $repoDir

    # Need to parse out the current NuGet package version of Structured Logger
    [xml]$deps = Get-Content (Join-Path $repoDir "build\Targets\Dependencies.props")
    $structuredLoggerVersion = $deps.Project.PropertyGroup.MicrosoftBuildLoggingStructuredLoggerVersion
    $structuredLoggerPath = Join-Path (Get-PackagesDir) "Microsoft.Build.Logging.StructuredLogger\$structuredLoggerVersion\lib\net46\StructuredLogger.dll"
    $configDir = Join-Path $binariesDir $config
    $logPath = Join-Path $configDir "build.xml"

    if ($msbuild -eq "") {
        $msbuild = Ensure-MSBuild
    }

    Write-Host "Building Roslyn.sln with logging support"
    Exec-Command $msbuild "/v:m /m /logger:StructuredLogger,$structuredLoggerPath;$logPath /nodeReuse:false /p:DeployExtension=false Roslyn.sln"
    Write-Host ""

    # Verify the state of our various build artifacts
    Write-Host "Running BuildBoss"
    $buildBossPath = Join-Path $configDir "Exes\BuildBoss\BuildBoss.exe"
    Exec-Command $buildBossPath "Roslyn.sln Compilers.sln src\Samples\Samples.sln CrossPlatform.sln build\Targets $logPath"
    Write-Host ""

    # Verify the state of our project.jsons
    Write-Host "Running RepoUtil"
    $repoUtilPath = Join-Path $configDir "Exes\RepoUtil\RepoUtil.exe"
    Exec-Command $repoUtilPath verify 
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
