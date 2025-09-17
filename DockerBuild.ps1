# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$Interactive,  # Opens an interactive PowerShell session
    [switch]$BuildImage,   # Only builds the image, but does not build the product.
    [switch]$NoBuildImage, # Does not build the image.
    [switch]$NoClean,      # Does not clean up.
    [switch]$NoNuGetCache, # Does not mount the host nuget cache in the container.
    [switch]$KeepSecrets,  # Does not override the secrets.g.json file.
    [string]$ImageName,    # Image name (defaults to a name based on the directory).
    [string]$BuildAgentPath = 'C:\BuildAgent',
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$BuildArgs   # Arguments passed to `Build.ps1` within the container.
)

# This setting is replaced by the generate-scripts command.
$EngPath = 'eng-Metalama'
$EnvironmentVariables = 'AWS_ACCESS_KEY_ID,AWS_SECRET_ACCESS_KEY,AZ_IDENTITY_USERNAME,AZURE_DEVOPS_TOKEN,AZURE_DEVOPS_USER,ENG_USERNAME,GITHUB_AUTHOR_EMAIL,GITHUB_REVIEWER_TOKEN,GITHUB_TOKEN,IS_POSTSHARP_OWNED,IS_TEAMCITY_AGENT,NUGET_ORG_API_KEY,SIGNSERVER_SECRET,TEAMCITY_CLOUD_TOKEN,TYPESENSE_API_KEY,VS_MARKETPLACE_ACCESS_TOKEN,VSS_NUGET_EXTERNAL_FEED_ENDPOINTS'

$dockerContextDirectory = "$EngPath/docker-context"

# Function to create secrets JSON file
function New-SecretsJson
{
    param(
        [string]$EnvironmentVariableList
    )

    # Parse comma-separated environment variable names
    $envVarNames = $EnvironmentVariableList -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

    # Build hashtable with environment variable values
    $secrets = @{ }
    foreach ($envVarName in $envVarNames)
    {
        $value = [Environment]::GetEnvironmentVariable($envVarName)
        if (-not [string]::IsNullOrEmpty($value))
        {
            $secrets[$envVarName] = $value
        }
    }

    # Convert to JSON and save
    $jsonPath = Join-Path $dockerContextDirectory "secrets.g.json"

    # Write a test JSON file with GUID first
    @{ guid = [System.Guid]::NewGuid().ToString() } | ConvertTo-Json | Set-Content -Path $jsonPath -Encoding UTF8

    # Check if secrets file is tracked by git
    $gitStatus = git status --porcelain $jsonPath 2> $null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitStatus))
    {
        Write-Error "Secrets file '$jsonPath' is tracked by git. Please add it to .gitignore first."
        exit 1
    }

    $secrets | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonPath -Encoding UTF8
    Write-Host "Created secrets file: $jsonPath" -ForegroundColor Cyan


    return $jsonPath
}

# Generate ImageName from script directory if not provided
if ( [string]::IsNullOrEmpty($ImageName))
{
    # Get full path without drive name (e.g., "C:\src\Metalama.Compiler" becomes "src\Metalama.Compiler")
    $fullPath = $PSScriptRoot -replace '^[A-Za-z]:\\', ''
    # Sanitize path to valid Docker image name (lowercase alphanumeric and hyphens only)
    $ImageName = $fullPath.ToLower() -replace '[^a-z0-9\-]', '-' -replace '-+', '-' -replace '^-|-$', ''
    # Ensure it doesn't start with a hyphen and has at least one character
    if ([string]::IsNullOrEmpty($ImageName) -or $ImageName -match '^-')
    {
        $ImageName = "docker-build-image"
    }
    Write-Host "Generated image name from directory: $ImageName" -ForegroundColor Cyan
}

# When building locally (as opposed as on the build agent), we must do a complete cleanup because 
# obj files may point to the host filesystem.
if (-not $env:IS_TEAMCITY_AGENT -and -not $NoClean)
{
    Write-Host "Cleaning up." -ForegroundColor Green
    if (Test-Path "artifacts")
    {
        Remove-Item artifacts -Force -Recurse -ProgressAction SilentlyContinue
    }
    Get-ChildItem @("bin", "obj") -Recurse | Remove-Item -Force -Recurse -ProgressAction SilentlyContinue
}

# Create secrets JSON file.
if ( -not $KeepSecrets )
{
    New-SecretsJson -EnvironmentVariableList $EnvironmentVariables
}

# Get the source directory name from $PSScriptRoot
$SourceDirName = $PSScriptRoot

# Start timing the entire process except cleaning
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Ensure docker context directory exists and contains at least one file
if (-not (Test-Path $dockerContextDirectory))
{
    Write-Error "Docker context directory '$dockerContextDirectory' does not exist."
    exit 1
}


# Prepare volume mappings
$volumeMappings = @("-v", "${SourceDirName}:${SourceDirName}")
$MountPoints = @($SourceDirName)

# Define static Git system directory for mapping
$gitSystemDir = "$BuildAgentPath\system\git"

if (Test-Path $gitSystemDir)
{
    $volumeMappings += @("-v", "${gitSystemDir}:${gitSystemDir}:ro")
    $MountPoints += $gitSystemDir
}

# Mount the host NuGet cache in the container.
if ( -not $NoNuGetCache )
{
    $nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
    Write-Host "NuGet cache directory: $nugetCacheDir" -ForegroundColor Cyan
    if (-not (Test-Path $nugetCacheDir))
    {
        Write-Host "Creating NuGet cache directory on host: $nugetCacheDir"
        New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
    }

    $volumeMappings += @("-v", "${nugetCacheDir}:c:\packages")
}

# We must add a MountPoint anyway so the directory is created in the container.
$MountPoints += "c:\packages"


# Execute auto-generated DockerMounts.g.ps1 script to add more directory mounts.
$dockerMountsScript = Join-Path $EngPath 'DockerMounts.g.ps1'
if (Test-Path $dockerMountsScript)
{
    Write-Host "Importing Docker mount points from $dockerMountsScript" -ForegroundColor Cyan
    . $dockerMountsScript
}

$mountPointsArg = $MountPoints -Join ";"

Write-Host "Volume mappings: " @volumeMappings -ForegroundColor Gray
Write-Host "Mount points: " $mountPointsArg -ForegroundColor Gray

# Building the image.
if (-not $NoBuildImage)
{
    Write-Host "Building the image." -ForegroundColor Green
    Get-Content -Raw Dockerfile | docker build -t $ImageName  --build-arg SRC_DIR="$SourceDirName"  --build-arg MOUNTPOINTS="$mountPointsArg"  -f - $dockerContextDirectory
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Docker build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
else
{
    Write-Host "Skipping image build (-NoBuildImage specified)." -ForegroundColor Yellow
}


# Run the build within the container
if (-not $BuildImage)
{
    if ($Interactive)
    {
        docker run --rm -it --memory=12g @volumeMappings -w $SourceDirName $ImageName pwsh
        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Docker run (interactive) failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
    else
    {
        # Delete now and not in the container because it's much faster and lock error messages are more relevant.
        Write-Host "Building the product in the container." -ForegroundColor Green

        # Prepare Build.ps1 arguments
        $buildCommand = "$SourceDirName\Build.ps1"
        $buildArgsString = $BuildArgs -join " "
        $buildCommand += " $buildArgsString"
        Write-Host "Passing arguments to Build.ps1: `"$buildArgsString`"." -ForegroundColor Cyan

        docker run --rm --memory=12g @volumeMappings -w $SourceDirName $ImageName pwsh -NonInteractive -Command $buildCommand
        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Docker run (build) failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}
else
{
    Write-Host "Skipping container run (BuildImage specified)." -ForegroundColor Yellow
}

# Stop timing and display results
$elapsed = $stopwatch.Elapsed
Write-Host ""
Write-Host "Total build time: $($elapsed.ToString('hh\:mm\:ss\.fff') )" -ForegroundColor Cyan
Write-Host "Build completed at: $( Get-Date -Format 'yyyy-MM-dd HH:mm:ss' )" -ForegroundColor Cyan
