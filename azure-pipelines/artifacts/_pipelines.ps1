# This script translates all the artifacts described by _all.ps1
# into commands that instruct Azure Pipelines to actually collect those artifacts.

param (
    [string]$ArtifactNameSuffix
)

& "$PSScriptRoot/_stage_all.ps1" -ArtifactNameSuffix $ArtifactNameSuffix |% {
    Write-Host "##vso[artifact.upload containerfolder=$($_.Name);artifactname=$($_.Name);]$($_.Path)"

    # Set a variable which will out-live this script so that a subsequent attempt to collect and upload artifacts
    # will skip this one from a check in the _all.ps1 script.
    $varName = "ARTIFACTUPLOADED_$($_.Name.ToUpper())"
    Write-Host "##vso[task.setvariable variable=$varName]true"
}
