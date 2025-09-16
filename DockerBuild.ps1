[CmdletBinding(PositionalBinding=$false)]
param(
	[switch]$Interactive,
	[switch]$BuildImage,
	[switch]$NoBuildImage,
  [switch]$NoClean,
	[string]$ImageName,
	[string]$EngPath = 'eng-Metalama',
	[string]$BuildAgentPath = 'C:\BuildAgent',
	[Parameter(ValueFromRemainingArguments)]
	[string[]]$BuildArgs
)

# Generate ImageName from script directory if not provided
if ([string]::IsNullOrEmpty($ImageName)) {
    # Get full path without drive name (e.g., "C:\src\Metalama.Compiler" becomes "src\Metalama.Compiler")
    $fullPath = $PSScriptRoot -replace '^[A-Za-z]:\\', ''
    # Sanitize path to valid Docker image name (lowercase alphanumeric and hyphens only)
    $ImageName = $fullPath.ToLower() -replace '[^a-z0-9\-]', '-' -replace '-+', '-' -replace '^-|-$', ''
    # Ensure it doesn't start with a hyphen and has at least one character
    if ([string]::IsNullOrEmpty($ImageName) -or $ImageName -match '^-') {
        $ImageName = "docker-build-image"
    }
    Write-Host "Generated image name from directory: $ImageName" -ForegroundColor Cyan
}

# When building locally (as opposed as on the build agent), we must do a complete cleanup because 
# obj files may point to the host filesystem.
if (-not $env:IS_TEAMCITY_AGENT -and -not $NoClean) {
    Write-Host "Cleaning up." -ForegroundColor Green
    if (Test-Path "artifacts") {
        Remove-Item artifacts -Force -Recurse -ProgressAction SilentlyContinue
    }
    Get-ChildItem @("bin","obj") -Recurse | Remove-Item -Force -Recurse -ProgressAction SilentlyContinue
}

# Get the source directory name from $PSScriptRoot
$SourceDirName = $PSScriptRoot

# Start timing the entire process except cleaning
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

$dockerContextDirectory = "$EngPath/docker-context"

# Ensure docker context directory exists and contains at least one file
if (-not (Test-Path $dockerContextDirectory)) {
    Write-Error "Docker context directory '$dockerContextDirectory' does not exist."
    exit 1
}

# Create local NuGet cache directory if it doesn't exist
$nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
Write-Host "NuGet cache directory: $nugetCacheDir" -ForegroundColor Cyan
if (-not (Test-Path $nugetCacheDir)) {
    Write-Host "Creating NuGet cache directory on host: $nugetCacheDir" 
    New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
}
# Define static Git system directory for mapping
$gitSystemDir = "$BuildAgentPath\system\git"

if (-not $NoBuildImage) {
    Write-Host "Building the image." -ForegroundColor Green
    Get-Content -Raw Dockerfile | docker build -t $ImageName -f - $dockerContextDirectory
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Docker build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} else {
    Write-Host "Skipping image build (-NoBuildImage specified)." -ForegroundColor Yellow
}

# Prepare volume mappings
$volumeMappings = @("-v", "${SourceDirName}:${SourceDirName}", "-v", "${nugetCacheDir}:c:\packages")
     Get-Content -Raw Dockerfile | docker build -t $ImageName  --build-arg SRC_DIR="$SourceDirName" -f - $dockerContextDirectory

# Create Git system directory on host if it doesn't exist and add volume mapping
if (Test-Path $gitSystemDir) {
    $volumeMappings += @("-v", "${gitSystemDir}:${gitSystemDir}:ro")
} 

Write-Host "Volume mappings: " @volumeMappings -ForegroundColor Gray


if (-not $BuildImage) {
    if ($Interactive) {
    	docker run --rm -it --memory=12g @volumeMappings -w $SourceDirName $ImageName pwsh
    	if ($LASTEXITCODE -ne 0) {
    		Write-Host "Docker run (interactive) failed with exit code $LASTEXITCODE" -ForegroundColor Red
    		exit $LASTEXITCODE
    	}
    } else {
      # Delete now and not in the container because it's much faster and lock error messages are more relevant.
      Write-Host "Building the product in the container." -ForegroundColor Green
    	
    	# Prepare Build.ps1 arguments
    	$buildCommand = "$SourceDirName\Build.ps1"
     	$buildArgsString = $BuildArgs -join " "
      $buildCommand += " $buildArgsString"
      Write-Host "Passing arguments to Build.ps1: `"$buildArgsString`"." -ForegroundColor Cyan

    	docker run --rm --memory=12g @volumeMappings -w $SourceDirName $ImageName pwsh -NonInteractive -Command $buildCommand
    	if ($LASTEXITCODE -ne 0) {
    		Write-Host "Docker run (build) failed with exit code $LASTEXITCODE" -ForegroundColor Red
    		exit $LASTEXITCODE
    	}
    }
} else {
    Write-Host "Skipping container run (BuildImage specified)." -ForegroundColor Yellow
}

# Stop timing and display results
$elapsed = $stopwatch.Elapsed
Write-Host ""
Write-Host "Total build time: $($elapsed.ToString('hh\:mm\:ss\.fff'))" -ForegroundColor Cyan
Write-Host "Build completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
