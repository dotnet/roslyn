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

$NuGetPackages = "$RepoRoot/bin/Packages/$BuildConfiguration/NuGet"

if (!(Test-Path $NuGetPackages)) {
    Write-Warning "Skipping because NuGet packages haven't been built yet."
    return @{}
}

$result = @{
    "$NuGetPackages" = (Get-ChildItem $NuGetPackages -Recurse)
}

if ($env:IsOptProf) {
    $VSRepoPackages = "$RepoRoot/bin/Packages/$BuildConfiguration/VSRepo"
    $result["$VSRepoPackages"] = (Get-ChildItem "$VSRepoPackages\*.VSInsertionMetadata.*.nupkg");
}

$result
