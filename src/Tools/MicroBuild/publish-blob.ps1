# Publishes our assets to our blob containers
#
# Repeatable is important here because we have to assume that publishes can and will fail with some 
# degree of regularity. 
[CmdletBinding(PositionalBinding=$false)]
Param(
    # Standard options
    [string]$configDir = "",

    # Credentials 
    [string]$blobFeedUrl = "",
    [string]$blobFeedKey = ""
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")
    $msbuild, $msbuildDir = Ensure-MSBuildAndDir -msbuildDir $msbuildDir

    if ($blobFeedUrl -eq "") {
        Write-Host "Need a value for -blobFeedUrl"
        exit 1
    }

    if ($blobFeedKey -eq "") {
        Write-Host "Need a value for -blobFeedKey"
        exit 1
    }

    if ($configDir -eq "") {
        Write-Host "Need a value for -configDir"
        exit 1
    }

    Exec-Console $msbuild "/p:ConfigDir=$configDir /p:ExpectedFeedUrl=$blobFeedUrl /p:AccountKey=$blobFeedKey /p:OutputPath=$configDir"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
