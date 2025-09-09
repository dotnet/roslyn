
param(
	[switch]$Interactive
)

$imageName = 'metalamacompiler20260'
$emptyDockerContext = New-Item -ItemType Directory -Force -Path (Join-Path $env:TEMP "empty-docker-context")

# Create local NuGet cache directory if it doesn't exist
$nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
if (-not (Test-Path $nugetCacheDir)) {
    New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
}

Get-Content -Raw Dockerfile | docker build -t $imageName -f - $emptyDockerContext

if ($Interactive) {
	docker run --rm -it -v "${PWD}:c:\src" -v "${nugetCacheDir}:c:\nuget" -w c:\src $imageName pwsh
} else {
  # Delete now and not in the container because it's much faster and lock error messages are more relevant.
  rd artifacts -Recursive -Force
	docker run --rm -v "${PWD}:c:\src" -v "${nugetCacheDir}:c:\nuget" -w c:\src $imageName pwsh -File c:\src\Build.ps1 build
}
