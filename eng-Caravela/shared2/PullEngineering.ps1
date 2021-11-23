# Stop after first error.
$ErrorActionPreference = "Stop"

trap
{
    Write-Error $PSItem.ToString()
    exit 1
}

# Check that we are in the root of a GIT repository.
If ( -Not ( Test-Path -Path ".\.git" ) ) {
    throw "This script has to run in a GIT repository root!"
}

# Update/initialize the engineering subtree.
$EngineeringDirectory = "eng/shared"

& git subtree pull --prefix $EngineeringDirectory https://postsharp@dev.azure.com/postsharp/Caravela/_git/Caravela.Engineering master --squash
