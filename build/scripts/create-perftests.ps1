# Create the PerfTests directory under Binaries\$(Configuration).  There are still a number
# of tools (in roslyn and roslyn-internal) that depend on this combined directory.
[CmdletBinding(PositionalBinding=$false)]
param ([string]$buildDir)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    [string]$target = Join-Path $buildDir "PerfTests"
    Write-Host "PerfTests: $target"
    if (-not (test-path $target)) {
        Create-Directory $target
    }

    Push-Location $buildDir
    foreach ($subDir in @("Dlls", "UnitTests")) {
        Push-Location $subDir
        foreach ($path in Get-ChildItem -re -in "PerfTests") {
            Write-Host "`tcopying $path"
            Copy-Item -force -recurse "$path\*" $target
        }
        Pop-Location
    }
    Pop-Location
    exit 0
}
catch {
    Write-Host "Error: $($_.Exception.Message)"
    exit 1
}

