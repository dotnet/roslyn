# This script translates all the artifacts described by _all.ps1
# into commands that instruct Azure Pipelines to actually collect those artifacts.

param (
    [string]$ArtifactNameSuffix
)

& "$PSScriptRoot/_stage_all.ps1" -ArtifactNameSuffix $ArtifactNameSuffix |% {
    Write-Host "##vso[artifact.upload containerfolder=$($_.Name);artifactname=$($_.Name);]$($_.Path)"
}
