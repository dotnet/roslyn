# Take care to return nothing if we don't yet have any vsman files
# as will occur at the start of the build.
# This will allow us to set it after the build when called again.

$vsmanpath = "artifacts/VSSetup/$Env:BuildConfiguration/Insertion"
if (Test-Path $vsmanpath) {
    $SetupManifests = [string]::Join(',', (Get-ChildItem "artifacts/VSSetup/$Env:BuildConfiguration/Insertion/*.vsman"))
    Write-Host "Using the following manifests '$SetupManifests'"
    Write-Host "SetupManifests=$SetupManifests"
    Write-Host "##vso[task.setvariable variable=SetupManifests;]$SetupManifests"
    Set-Item -Path "env:SetupManifests" -Value $SetupManifests
}
else {
    Write-Host "Unable to find manifest files"
}