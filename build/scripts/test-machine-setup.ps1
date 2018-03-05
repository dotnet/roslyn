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
    Write-Host "Calling Get-MSBuildKindAndDir"
    Get-MSBuildKindAndDir
    Write-Host "Calling Get-MSBuildKindAndDir -xcopy"
    Get-MSBuildKindAndDir -xcopy
    Write-Host "Calling Get-MSBuildDir"
    Get-MSBuildDir
    Write-Host "Calling Get-MSBuildDir -xcopy"
    Get-MSBuildDir -xcopy
    
    try {
        Write-Host "Calling Get-VisualStudioDir"
        Get-VisualStudioDir
    }
    catch { 
        Write-Host "Unable to find Visual Studio (expected on a machine without VS)"
        Write-Host $_
        Write-Host $_.Exception
    }

    try { 

        $vswhere = Ensure-BasicTool "vswhere" "1.0.50"
        Write-Host "VSWhere is $vswhere"
    }
    catch { 
        Write-Host "Error gettin vswhere"
        Write-Host $_
        Write-Host $_.Exception
    }

    Write-Host "Enumerate packages dir"
    $d = Get-PackagesDir
    Get-ChildItem $d

}
catch {
    Write-Host $_.Exception.Message
    exit 1
}
finally {
    Set-PSDebug -Trace 0
}
