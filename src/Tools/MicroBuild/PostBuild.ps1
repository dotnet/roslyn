<#
.SYNOPSIS
Performs any post-build actions needed on the Roslyn build systems.

.PARAMETER binariesPath
The root directory where the build outputs are written: e:\path\to\source\Binaries\Debug

.PARAMETER sourcePath
The root directory where the sources are checked out: e:\path\to\source

.PARAMETER platform
The platform being built

.PARAMETER configuration
The configuration being built

#>
param(
    [string]$binariesDirectory,
    [string]$sourceDirectory
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    write-host "Post Build Steps"

    write-host "Building the NuGets"
    $msbuildExe = & (join-path $sourcePath "build\scripts\get-msbuildpath.ps1")
    $nugetProj = join-path $sourcePath "src\Nuget\NuGet.proj"

    & $msbuildExe /v:m /nodereuse:false /p:Platform=$platform /p:Configuration=$configuration $nugetProj
    if ($lastExitCode -ne 0) {
        throw "Build failed"
    }

    exit 0
}
catch [exception]
{
    write-error -Exception $_.Exception
    exit -1
}
