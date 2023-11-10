<#
.SYNOPSIS
    This script links all the artifacts described by _all.ps1
    into a staging directory, reading for uploading to a cloud build artifact store.
    It returns a sequence of objects with Name and Path properties.
#>

[CmdletBinding()]
param (
    [string]$ArtifactNameSuffix
)

$ArtifactStagingFolder = & "$PSScriptRoot/../Get-ArtifactsStagingDirectory.ps1" -CleanIfLocal

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
    if ($IsMacOS -or $IsLinux) {
        ln $Target $Link | Out-Null
    } else {
        cmd /c "mklink `"$Link`" `"$Target`"" | Out-Null
    }
}

# Stage all artifacts
$Artifacts = & "$PSScriptRoot\_all.ps1" -ArtifactNameSuffix $ArtifactNameSuffix
$Artifacts |% {
    $DestinationFolder = [System.IO.Path]::GetFullPath("$ArtifactStagingFolder/$($_.ArtifactName)$ArtifactNameSuffix/$($_.ContainerFolder)").TrimEnd('\')
    $Name = "$(Split-Path $_.Source -Leaf)"

    #Write-Host "$($_.Source) -> $($_.ArtifactName)\$($_.ContainerFolder)" -ForegroundColor Yellow

    if (-not (Test-Path $DestinationFolder)) { New-Item -ItemType Directory -Path $DestinationFolder | Out-Null }
    if (Test-Path -PathType Leaf $_.Source) { # skip folders
        Create-SymbolicLink -Link (Join-Path $DestinationFolder $Name) -Target $_.Source
    }
}

$ArtifactNames = $Artifacts |% { "$($_.ArtifactName)$ArtifactNameSuffix" }
$ArtifactNames += Get-ChildItem env:ARTIFACTSTAGED_* |% {
    # Return from ALLCAPS to the actual capitalization used for the artifact.
    $artifactNameAllCaps = "$($_.Name.Substring('ARTIFACTSTAGED_'.Length))"
    (Get-ChildItem $ArtifactStagingFolder\$artifactNameAllCaps* -Filter $artifactNameAllCaps).Name
}
$ArtifactNames | Get-Unique |% {
    $artifact = New-Object -TypeName PSObject
    Add-Member -InputObject $artifact -MemberType NoteProperty -Name Name -Value $_
    Add-Member -InputObject $artifact -MemberType NoteProperty -Name Path -Value (Join-Path $ArtifactStagingFolder $_)
    Write-Output $artifact
}
