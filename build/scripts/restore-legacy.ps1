# This is a script to convert from our legacy CMD file / format for arguments
# to the Powershell - version.

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$newArgs = @()
foreach ($arg in $args) { 
    if (($arg.Length -gt 0) -and ($arg[0] -eq '/')) {
        $arg = '-' + $arg.Substring(1)
    }

    $newArgs += $arg
}

Write-Host "New Args are $newArgs"
$script = Join-Path $PSScriptRoot "restore.ps1"
Invoke-Expression "$script $newArgs"
