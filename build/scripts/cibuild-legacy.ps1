# This is a script to convert from our legacy CMD file / format for arguments
# to the Powershell - version.

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$newArgs = @()
foreach ($arg in $args) { 
    if (($arg.Length -gt 0) -and ($arg[0] -eq '/')) {
        $arg = '-' + $arg.Substring(1)
    }

    if ($arg -eq "-debug") {
        $arg = "-release:`$false"
    }

    if ($arg -eq "-test32") {
        $arg = "-test64:`$false"
    }

    $newArgs += $arg
}

Write-Host "!!!This script is legacy and will be deleted.  Please call build/scripts/cibuild.cmd directly!!!"
Write-Host "New Args are $newArgs"
$script = Join-Path $PSScriptRoot "cibuild.ps1"
Invoke-Expression "$script $newArgs"
Write-Host "!!!This script is legacy and will be deleted.  Please call build/scripts/cibuild.cmd directly!!!"
