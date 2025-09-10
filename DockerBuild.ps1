
param(
	[switch]$Interactive
)

# We must do a complete cleanup because obj files may point to the host filesystem.
Write-Host "Cleaning up." -ForegroundColor Green
Remove-Item artifacts -Force -Recurse
Get-ChildItem @("bin","obj") -Recurse | Remove-Item -Force -Recurse


# Start timing the entire process except cleaning
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$imageName = 'metalamacompiler20260'
$dockerContextDirectory = New-Item -ItemType Directory -Force -Path (Join-Path $env:TEMP "empty-docker-context")
cp eng-Metalama/docker.vsconfig $dockerContextDirectory

# Create local NuGet cache directory if it doesn't exist
$nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
if (-not (Test-Path $nugetCacheDir)) {
    New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
}

# Define static Git system directory for mapping
$gitSystemDir = "C:\BuildAgent\system\git"

Write-Host "Building the image." -ForegroundColor Green
Get-Content -Raw Dockerfile | docker build -t $imageName --build-arg GIT_SYSTEM_DIR=$gitSystemDir -f - $dockerContextDirectory

# Prepare volume mappings
$volumeMappings = @("-v", "${PWD}:c:\src", "-v", "${nugetCacheDir}:c:\nuget")

# Create Git system directory on host if it doesn't exist and add volume mapping
if (-not (Test-Path $gitSystemDir)) {
    Write-Host "Creating Git system directory on host: $gitSystemDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $gitSystemDir -Force | Out-Null
}
$volumeMappings += @("-v", "${gitSystemDir}:${gitSystemDir}:ro")
Write-Host "Adding Git system directory volume mapping (read-only): ${gitSystemDir}:${gitSystemDir}:ro" -ForegroundColor Yellow

if ($Interactive) {
	docker run --rm -it --memory=12g @volumeMappings -w c:\src $imageName pwsh
} else {
  # Delete now and not in the container because it's much faster and lock error messages are more relevant.
  Write-Host "Building the product in the container." -ForegroundColor Green
	docker run --rm --memory=12g @volumeMappings -w c:\src $imageName pwsh -File c:\src\Build.ps1 build
}

# Stop timing and display results
$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed
Write-Host ""
Write-Host "Total build time: $($elapsed.ToString('hh\:mm\:ss\.fff'))" -ForegroundColor Cyan
Write-Host "Build completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
