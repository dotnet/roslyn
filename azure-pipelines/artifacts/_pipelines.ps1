# This script translates all the artifacts described by _all.ps1
# into commands that instruct Azure Pipelines to actually collect those artifacts.

param (
    [string]$ArtifactNameSuffix
)

$RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $ArtifactStagingFolder = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
} else {
    $ArtifactStagingFolder = "$RepoRoot\obj\_artifacts"
    if (Test-Path $ArtifactStagingFolder) {
        Remove-Item $ArtifactStagingFolder -Recurse -Force
    }
}

function Create-SymbolicLink {
    param (
        $Link,
        $Target
    )

    if ($Link -eq $Target) {
        return
    }

    if (Test-Path $Link) { Remove-Item $Link }
    $LinkContainer = Split-Path $Link -Parent
    if (!(Test-Path $LinkContainer)) { mkdir $LinkContainer }
    Write-Verbose "Linking $Link to $Target"
    if ($IsMacOS -or $IsLinux) {
        ln $Target $Link
    } else {
        cmd /c mklink $Link $Target
    }
}

# Stage all artifacts
$Artifacts = & "$PSScriptRoot\_all.ps1"
$Artifacts |% {
    $DestinationFolder = (Join-Path (Join-Path $ArtifactStagingFolder "$($_.ArtifactName)$ArtifactNameSuffix") $_.ContainerFolder).TrimEnd('\')
    $Name = "$(Split-Path $_.Source -Leaf)"

    #Write-Host "$($_.Source) -> $($_.ArtifactName)\$($_.ContainerFolder)" -ForegroundColor Yellow

    if (-not (Test-Path $DestinationFolder)) { New-Item -ItemType Directory -Path $DestinationFolder | Out-Null }
    if (Test-Path -PathType Leaf $_.Source) { # skip folders
        Create-SymbolicLink -Link "$DestinationFolder\$Name" -Target $_.Source
    }
}

$Artifacts |% { $_.ArtifactName } | Get-Unique |% {
    Write-Host "##vso[artifact.upload containerfolder=$_$ArtifactNameSuffix;artifactname=$_$ArtifactNameSuffix;]$ArtifactStagingFolder/$_$ArtifactNameSuffix"
}
