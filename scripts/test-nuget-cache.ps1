<#

This script exists to help us track down the NuGet cache corruption bug that is showing up 
in Jenkins.  It will look for critical files in the NuGet which have been zerod out.

https://github.com/dotnet/roslyn/issues/19882

#>

[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Test-ZeroFile([string]$filePath) {
    $bytes = [IO.File]::ReadAllBytes($filePath)
    if ($bytes.Length -eq 0) {
        return $false
    }

    foreach ($b in $bytes) { 
        if ($b -ne 0) {
            return $false
        }
    }

    return $true
}

function Go() {
    Push-Location (Get-PackagesDir)
    try {
        $failed = @()
        foreach ($filePath in Get-ChildItem -re -in *.nuspec,*proj,*settings,*targets) {
            if (Test-Path $filePath -PathType Container) {
                continue;
            }

            if (Test-ZeroFile $filePath) { 
                Write-Host "Testing $filePath FAILED"
                $failed += $filePath
            } 
            else { 
                Write-Host "Testing $filePath passed"
            }
        }

        if ($failed.Length -gt 0) { 
            Write-Host "Detected zero'd out files in Nuget cache"
            foreach ($f in $failed) { 
                Write-Host "`t$f"
            }

            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

try { 
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Go
    exit 0
}
catch {
    Write-Host $_
    exit 1
}
