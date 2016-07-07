<#
.SYNOPSIS
Performs any post-build actions needed on the Roslyn build systems.

.PARAMETER BinariesDirectory
The root directory where the build outputs are written: e:\path\to\source\Binaries\Debug

.PARAMETER SourceDirectory
The root directory where the sources are checked out: e:\path\to\source

#>
param(
    [string]$binariesDirectory,
    [string]$sourceDirectory,
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    write-host "Post Build Steps"

    exit 0
}
catch [exception]
{
    write-error -Exception $_.Exception
    exit -1
}
