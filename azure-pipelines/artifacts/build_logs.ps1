$ArtifactStagingFolder = & "$PSScriptRoot/../Get-ArtifactsStagingDirectory.ps1"

if (!(Test-Path $ArtifactStagingFolder/build_logs)) { return }

@{
    "$ArtifactStagingFolder/build_logs" = (Get-ChildItem -Recurse "$ArtifactStagingFolder/build_logs")
}
