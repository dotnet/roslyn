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

# TODO: unify with locate-vs.ps1
function Get-PackagesPath {
    $packagesPath = $env:NUGET_PACKAGES
    if ($packagesPath -eq $null) {
        $packagesPath = join-path $env:UserProfile ".nuget\packages\"
    }

    return $packagesPath
}

try
{
    # TODO: RepoUtil needs to generate a powershell file that can be imported for 
    # values like this.
    $structuredLoggerVersion = "1.0.58"
    $packagesPath = Get-PackagesPath

    write-host "Building Roslyn.sln with logging support"

    $structuredLoggerPath = join-path $packagesPath "Microsoft.Build.Logging.StructuredLogger\$structuredLoggerVersion\lib\net46\StructuredLogger.dll"
    $roslynSlnPath = join-path $sourcePath "Roslyn.sln"
    $logPath = join-path $binariesPath "build.xml"

    & msbuild /v:m /m /logger:StructuredLogger`,$structuredLoggerPath`;$logPath $roslynSlnPath 

}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
