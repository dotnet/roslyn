# This artifact captures everything needed to insert into VS (NuGet packages, insertion metadata, etc.)

<#
.PARAMETER SbomNotRequired
    Indicates that returning the artifacts available is preferable to nothing at all when the SBOM has not yet been generated.
#>
[CmdletBinding()]
Param (
    [switch]$SbomNotRequired
)

if ($IsMacOS -or $IsLinux) {
    # We only package up for insertions on Windows agents since they are where optprof can happen.
    Write-Verbose "Skipping VSInsertion artifact since we're not on Windows."
    return @{}
}

$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$PackagesRoot = "$RepoRoot/bin/Packages/$BuildConfiguration/NuGet"

# This artifact is not ready if we're running on the devdiv AzDO account and we don't have an SBOM yet.
if ($env:SYSTEM_COLLECTIONID -eq '011b8bdf-6d56-4f87-be0d-0092136884d9' -and -not (Test-Path $PackagesRoot/_manifest) -and -not $SbomNotRequired) {
    Write-Host "Skipping because SBOM isn't generated yet."
    return @{}
}

if (!(Test-Path $PackagesRoot)) {
    Write-Warning "Skipping because packages haven't been built yet."
    return @{}
}

@{
    "$PackagesRoot" = (Get-ChildItem $PackagesRoot -Recurse)
}
