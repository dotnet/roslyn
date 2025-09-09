
param(
	[switch]$Interactive
)

$imageName = 'metalamacompiler20260'
$dockerContextDirectory = New-Item -ItemType Directory -Force -Path (Join-Path $env:TEMP "empty-docker-context")
cp eng-Metalama/docker.vsconfig $dockerContextDirectory

# Create local NuGet cache directory if it doesn't exist
$nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
if (-not (Test-Path $nugetCacheDir)) {
    New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
}

Write-Host "Building the image." -ForegroundColor Green
Get-Content -Raw Dockerfile | docker build -t $imageName -f - $dockerContextDirectory

# We must do a complete cleanup because obj files may point to the host filesystem.
Write-Host "Cleaning up." -ForegroundColor Green
Remove-Item artifacts -Force -Recurse
Get-ChildItem @("bin","obj") -Recurse | Remove-Item -Force -Recurse

if ($Interactive) {
	docker run --rm -it --memory=12g -v "${PWD}:c:\src" -v "${nugetCacheDir}:c:\nuget" -w c:\src $imageName pwsh
} else {
  # Delete now and not in the container because it's much faster and lock error messages are more relevant.
  Write-Host "Building the product in the container." -ForegroundColor Green
	docker run --rm --memory=12g -v "${PWD}:c:\src" -v "${nugetCacheDir}:c:\nuget" -w c:\src $imageName pwsh -File c:\src\Build.ps1 build
}
