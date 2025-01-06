Param(
    [switch]$CleanIfLocal
)
if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $ArtifactStagingFolder = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
} elseif ($env:RUNNER_TEMP) {
    $ArtifactStagingFolder = Join-Path $env:RUNNER_TEMP _artifacts
} else {
    $ArtifactStagingFolder = [System.IO.Path]::GetFullPath("$PSScriptRoot/../obj/_artifacts")
    if ($CleanIfLocal -and (Test-Path $ArtifactStagingFolder)) {
        Remove-Item $ArtifactStagingFolder -Recurse -Force
    }
}

$ArtifactStagingFolder
