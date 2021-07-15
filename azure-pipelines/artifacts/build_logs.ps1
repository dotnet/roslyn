if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $artifactsRoot = $env:BUILD_ARTIFACTSTAGINGDIRECTORY
} else {
    $RepoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..")
    $artifactsRoot = "$RepoRoot\bin"
}

if (!(Test-Path $artifactsRoot/build_logs)) { return }

@{
    "$artifactsRoot/build_logs" = (Get-ChildItem -Recurse "$artifactsRoot/build_logs")
}
