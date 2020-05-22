# This script links all the artifacts described by _all.ps1
# into a staging directory, reading for uploading to a cloud build artifact store.
# It returns a sequence of objects with Name and Path properties.

param (
    [string]$ArtifactNameSuffix
)

$RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot (Join-Path .. ..)))
if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $ArtifactStagingFolder = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
} else {
    $ArtifactStagingFolder = Join-Path $RepoRoot (Join-Path obj _artifacts)
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
        ln $Target $Link | Out-Null
    } else {
        cmd /c "mklink `"$Link`" `"$Target`"" | Out-Null
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
        Create-SymbolicLink -Link (Join-Path $DestinationFolder $Name) -Target $_.Source
    }
}

$Artifacts |% { "$($_.ArtifactName)$ArtifactNameSuffix" } | Get-Unique |% {
    $artifact = New-Object -TypeName PSObject
    Add-Member -InputObject $artifact -MemberType NoteProperty -Name Name -Value $_
    Add-Member -InputObject $artifact -MemberType NoteProperty -Name Path -Value (Join-Path $ArtifactStagingFolder $_)
    Write-Output $artifact
}
