<#
.SYNOPSIS
    Checks whether the .NET Core SDK required by this repo is installed.
#>
[CmdletBinding()]
Param (
)

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (!$dotnet) {
    # Nothing is installed.
    Write-Output $false
    exit 1
}

# We need to set the current directory so dotnet considers the SDK required by our global.json file.
Push-Location "$PSScriptRoot\.."
try {
    dotnet -h 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 129) {
        # This error code indicates no matching SDK exists.
        Write-Output $false
        exit 2
    }

    # The required SDK is already installed!
    Write-Output $true
    exit 0
} finally {
    Pop-Location
}
