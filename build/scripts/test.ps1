# Script for testing out the various functions on a given machine.  Useful for
# debugging Jeknins issues.

param ([switch]$simple = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    if (-not $simple) { 
        Set-PSDebug -Trace 2
    }

    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Write-Host "Calling Get-MSBuildDirCore"
    Get-MSBuildDirCore
    Write-Host "Calling Get-MSBuildDir"
    Get-MSBuildDir
    
    try {
        Write-Host "Calling Get-VisualStudioDir"
        Get-VisualStudioDir
    }
    catch { 
        Write-Host "Unable to find Visual Studio (expected on a machine without VS)"
        Write-Host $_
    }

}
catch {
    Write-Host $_.Exception.Message
    exit 1
}
finally {
    Set-PSDebug -Trace 0
}
