# Checks for localizable resources that have not yet been translated.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    $srcDir = Join-Path $repoDir "src"

    # Look for two patterns: one for completely new items that have no translation,
    # and one for items with a translation that is out-of-date with respect to the
    # original English.
    $searchPatterns = 'state="new"', 'state="needs-review-translation"'

    $foundUntranslatedResources = $false

    # Find all the .xlf files.
    $xliffFiles = Get-ChildItem -Recurse -Path $srcDir -Include *.xlf

    # Loop over the .xlf files. Print out the paths of the ones that require
    # translation, and set the flag indicating we have untranslated resources.
    $xliffFiles |
    where { $_ | Select-String -Pattern $searchPatterns } |
    % {
        Write-Host "Untranslated resources in $($_.FullName)"
        $foundUntranslatedResources = $true
    }

    # If we have untranslated resources, fail the script.
    if ($foundUntranslatedResources) {
        Write-Host "Found untranslated resources!"
        exit 1
    }
    else {
        Write-Host "Found no untranslated resources."
        exit 0
    }
}
catch {
    Write-Host $_.Exception.Message
    exit 1
}
