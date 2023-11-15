# This artifact captures everything needed to insert into VS (NuGet packages, insertion metadata, etc.)

[CmdletBinding()]
Param (
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

if (!(Test-Path $PackagesRoot)) {
    Write-Warning "Skipping because packages haven't been built yet."
    return @{}
}

@{
    "$PackagesRoot" = (Get-ChildItem $PackagesRoot -Recurse)
}
