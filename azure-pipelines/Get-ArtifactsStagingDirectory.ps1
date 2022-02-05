Param(
    [switch]$CleanIfLocal
)
if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $ArtifactStagingFolder = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
} else {
    $ArtifactStagingFolder = [System.IO.Path]::GetFullPath("$PSScriptRoot/../obj/_artifacts")
    if ($CleanIfLocal -and (Test-Path $ArtifactStagingFolder)) {
        Remove-Item $ArtifactStagingFolder -Recurse -Force
    }
}

$ArtifactStagingFolder
