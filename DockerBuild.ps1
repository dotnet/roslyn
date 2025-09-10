
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

# Check for Git LFS storage directory in .git/config
$lfsStorageDir = $null
$gitConfigPath = ".git\config"
if (Test-Path $gitConfigPath) {
    $gitConfig = Get-Content $gitConfigPath
    foreach ($line in $gitConfig) {
        if ($line.Trim() -match "^\s*storage\s*=\s*(.+)$") {
            $lfsStorageDir = $matches[1].Trim()
            Write-Host "Found Git LFS storage directory: $lfsStorageDir" -ForegroundColor Yellow
            break
        }
    }
}

Write-Host "Building the image." -ForegroundColor Green
$buildArgs = @()
if ($lfsStorageDir) {
    $buildArgs += @("--build-arg", "LFS_STORAGE_DIR=$lfsStorageDir")
    Write-Host "Building image with LFS storage directory: $lfsStorageDir" -ForegroundColor Yellow
}
Get-Content -Raw Dockerfile | docker build -t $imageName -f - @buildArgs $dockerContextDirectory

# Prepare volume mappings
$volumeMappings = @("-v", "${PWD}:c:\src", "-v", "${nugetCacheDir}:c:\nuget")
if ($lfsStorageDir -and (Test-Path $lfsStorageDir)) {
    $volumeMappings += @("-v", "${lfsStorageDir}:${lfsStorageDir}")
    Write-Host "Adding LFS storage volume mapping: ${lfsStorageDir}:${lfsStorageDir}" -ForegroundColor Yellow
}

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
