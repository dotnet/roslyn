if ($env:SYSTEM_TEAMPROJECT) {
    "ProfilingInputs/$env:SYSTEM_TEAMPROJECT/$env:BUILD_REPOSITORY_NAME/$env:BUILD_SOURCEBRANCHNAME/$env:BUILD_BUILDID"
} else {
    Write-Warning "No Azure Pipelines build detected. No Azure Pipelines drop name will be computed."
}
