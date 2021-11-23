<#
.SYNOPSIS
    This script build the engineering tools.
#>

$ErrorActionPreference = "Stop"

trap
{
    Write-Error $PSItem.ToString()
    exit 1
}

& dotnet publish $PSScriptRoot/src/PostSharp.Engineering.BuildTools.sln
exit $LASTEXITCODE