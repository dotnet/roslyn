<#
.SYNOPSIS
    Checks whether a given .NET Core runtime is installed.
#>
[CmdletBinding()]
Param (
    [Parameter()]
    [ValidateSet('Microsoft.AspNetCore.App','Microsoft.NETCore.App')]
    [string]$Runtime='Microsoft.NETCore.App',
    [Parameter(Mandatory=$true)]
    [Version]$Version
)

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (!$dotnet) {
    # Nothing is installed.
    Write-Output $false
    exit 1
}

Function IsVersionMatch {
    Param(
        [Parameter()]
        $actualVersion
    )
    return $actualVersion -and
           $Version.Major -eq $actualVersion.Major -and
           $Version.Minor -eq $actualVersion.Minor -and
           (($Version.Build -eq -1) -or ($Version.Build -eq $actualVersion.Build)) -and
           (($Version.Revision -eq -1) -or ($Version.Revision -eq $actualVersion.Revision))
}

$installedRuntimes = dotnet --list-runtimes |? { $_.Split()[0] -ieq $Runtime } |% { $v = $null; [Version]::tryparse($_.Split()[1], [ref] $v); $v }
$matchingRuntimes = $installedRuntimes |? { IsVersionMatch -actualVersion $_ }
if (!$matchingRuntimes) {
    Write-Output $false
    exit 1
}

Write-Output $true
exit 0
