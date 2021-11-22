Param ( [Parameter(Mandatory=$True)] [string] $Solution, [string] $Params = "" )

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

"Cleaning ${Solution} ${Params}"

& ./eng/shared/tools/Restore.ps1 JetBrains.Resharper.GlobalTools

& ./tools/jb cleanupcode -p=Custom $Solution $Params --toolset=16.0 --disable-settings-layers:"GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal"
