<#
    This script drives the Jenkins verification that our build is correct.  In particular: 

        - Our build has no double writes
        - Our project.json files are consistent
        - our build files are well structured
        - Our solution states are consistent

#>

Param(
    [string]$sourcePath = $null,
    [string]$binariesPath = $null
)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

function Get-PackagesPath {
    $packagesPath = $env:NUGET_PACKAGES
    if ($packagesPath -eq $null) {
        $packagesPath = join-path $env:UserProfile ".nuget\packages\"
    }

    return $packagesPath
}

pushd $sourcePath
try
{
    # Need to parse out the current NuGet package version of Structured Logger
    [xml]$deps = get-content (join-path $sourcePath "build\Targets\Dependencies.props")
    $structuredLoggerVersion = $deps.Project.PropertyGroup.MicrosoftBuildLoggingStructuredLoggerVersion
    $packagesPath = Get-PackagesPath
    $structuredLoggerPath = join-path $packagesPath "Microsoft.Build.Logging.StructuredLogger\$structuredLoggerVersion\lib\net46\StructuredLogger.dll"
    $logPath = join-path $binariesPath "build.xml"

    write-host "Building Roslyn.sln with logging support"
    & msbuild /v:m /m /logger:StructuredLogger`,$structuredLoggerPath`;$logPath /nodeReuse:false /p:DeployExtension=false Roslyn.sln
    if (-not $?) {
        exit 1
    }
    write-host ""

    # Verify the state of our various build artifacts
    write-host "Running BuildBoss"
    $buildBossPath = join-path $binariesPath "Exes\BuildBoss\BuildBoss.exe"
    & $buildBossPath Roslyn.sln Compilers.sln src\Samples\Samples.sln CrossPlatform.sln "build\Targets" $logPath
    if (-not $?) {
        write-host "See the README for more details on BuildBoss: $(join-path $sourcePath "src\Tools\BuildBoss\README.md")"
        exit 1
    }
    write-host ""

    # Verify the state of our project.jsons
    write-host "Running RepoUtil"
    $repoUtilPath = join-path $binariesPath "Exes\RepoUtil\RepoUtil.exe"
    & $repoUtilPath verify
    if (-not $?) {
        exit 1
    }
    write-host ""

    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
finally
{
    popd
}
