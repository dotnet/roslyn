Param ( 

    [string] $EngineeringDirectory = "eng" )


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


$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path
$TargetDir = "$EngineeringDirectory\shared"
echo "Copying $ScriptDir to $TargetDir"

if ( Test-Path $TargetDir ) {
  rd $TargetDir -Recurse -Force
}


Copy-Item $ScriptDir\* $TargetDir -Force -Recurse -Exclude ".git" 