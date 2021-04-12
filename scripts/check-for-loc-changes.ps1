<#
.SYNOPSIS

Checks if a merge will result in changes to .xlf files, and returns a failure
code if true.

.DESCRIPTION

The point of this script is to block changes to localized resources after we've
entered a Loc freeze. It's meant to be run in a CI system prior to merging a PR.

.PARAMETER base

Generally the branch a change will be merged into, but this could also be a
commit or tag.

.PARAMETER head

The commit that will be merged into $base. Again, this can be a branch, tag, or
commit.
#>

[CmdletBinding(PositionalBinding=$false)]
param(
    [Parameter(Mandatory=$true)]
    [string]$base,

    [Parameter(Mandatory=$true)]
    [string]$head
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $RepoRoot

    # Find the merge base, then the list of files that changed since then.
    $mergeBase = & git merge-base $base $head
    $changedFiles = @(& git diff --name-only $mergeBase $head)

    # Filter out everything that isn't a .xlf file.
    $changedXlfFiles = @($changedFiles | where { [IO.Path]::GetExtension($_) -eq ".xlf" })

    # Fail if there are any changed .xlf files.
    $changedXlfFiles | % { Write-Host "$_ has been modified" }
    if ($changedXlfFiles.Count -eq 0) {
        exit 0
    }
    else {
        exit 1
    }
}
catch [exception] {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}