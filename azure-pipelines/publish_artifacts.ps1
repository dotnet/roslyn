<#
.SYNOPSIS
    This script translates all the artifacts described by _all.ps1
    into commands that instruct Azure Pipelines to actually collect those artifacts.
#>

[CmdletBinding()]
param (
    [string]$ArtifactNameSuffix,
    [switch]$StageOnly,
    [switch]$AvoidSymbolicLinks
)

Function Set-PipelineVariable($name, $value) {
    if ((Test-Path "Env:\$name") -and (Get-Item "Env:\$name").Value -eq $value) {
        return # already set
    }

    #New-Item -LiteralPath "Env:\$name".ToUpper() -Value $value -Force | Out-Null
    Write-Host "##vso[task.setvariable variable=$name]$value"
}

Function Test-ArtifactUploaded($artifactName) {
    $varName = "ARTIFACTUPLOADED_$($artifactName.ToUpper())"
    Test-Path "env:$varName"
}

& "$PSScriptRoot/../tools/artifacts/_stage_all.ps1" -ArtifactNameSuffix $ArtifactNameSuffix -AvoidSymbolicLinks:$AvoidSymbolicLinks |% {
    # Set a variable which will out-live this script so that a subsequent attempt to collect and upload artifacts
    # will skip this one from a check in the _all.ps1 script.
    Set-PipelineVariable "ARTIFACTSTAGED_$($_.Name.ToUpper())" 'true'
    Write-Host "Staged artifact $($_.Name) to $($_.Path)"

    if (!$StageOnly) {
        if (Test-ArtifactUploaded $_.Name) {
            Write-Host "Skipping $($_.Name) because it has already been uploaded." -ForegroundColor DarkGray
        } else {
            Write-Host "##vso[artifact.upload containerfolder=$($_.Name);artifactname=$($_.Name);]$($_.Path)"

            # Set a variable which will out-live this script so that a subsequent attempt to collect and upload artifacts
            # will skip this one from a check in the _all.ps1 script.
            Set-PipelineVariable "ARTIFACTUPLOADED_$($_.Name.ToUpper())" 'true'
        }
    }
}
