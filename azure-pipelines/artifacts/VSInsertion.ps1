# This artifact captures everything needed to insert into VS (NuGet packages, insertion metadata, etc.)

<#
.PARAMETER SbomNotRequired
    Indicates that returning the artifacts available is preferable to nothing at all when the SBOM has not yet been generated.
#>
Param (
    [switch]$SbomNotRequired
)

$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
$BuildConfiguration = $env:BUILDCONFIGURATION
if (!$BuildConfiguration) {
    $BuildConfiguration = 'Debug'
}

$PackagesRoot = "$RepoRoot/bin/Packages/$BuildConfiguration/NuGet"

# This artifact is not ready if we're running on the devdiv AzDO account and we don't have an SBOM yet.
if ($env:SYSTEM_COLLECTIONID -eq '011b8bdf-6d56-4f87-be0d-0092136884d9' -and -not (Test-Path $PackagesRoot/_manifest) -and -not $SbomNotRequired) { return @{} }

if (!(Test-Path $PackagesRoot))  { return @{} }

@{
    "$PackagesRoot" = (Get-ChildItem $PackagesRoot -Recurse)
}
