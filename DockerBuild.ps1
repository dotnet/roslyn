# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$Interactive, # Opens an interactive PowerShell session
    [switch]$BuildImage, # Only builds the image, but does not build the product.
    [switch]$NoBuildImage, # Does not build the image.
    [switch]$NoClean, # Does not clean up.
    [switch]$NoNuGetCache, # Does not mount the host nuget cache in the container.
    [switch]$KeepEnv, # Does not override the env.g.json file.
    [string]$ImageName, # Image name (defaults to a name based on the directory).
    [string]$BuildAgentPath = 'C:\BuildAgent',
    [switch]$LoadEnvFromKeyVault, # Forces loading environment variables form the key vault.
    [switch]$VsDebug, # Enable the remote debugger.
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$BuildArgs   # Arguments passed to `Build.ps1` within the container.
)

####
# These settings are replaced by the generate-scripts command.
$EngPath = 'eng-Metalama'
$EnvironmentVariables = 'AWS_ACCESS_KEY_ID,AWS_SECRET_ACCESS_KEY,AZ_IDENTITY_USERNAME,AZURE_CLIENT_ID,AZURE_CLIENT_SECRET,AZURE_DEVOPS_TOKEN,AZURE_DEVOPS_USER,AZURE_TENANT_ID,DOC_API_KEY,DOWNLOADS_API_KEY,ENG_USERNAME,GIT_USER_EMAIL,GIT_USER_NAME,GITHUB_AUTHOR_EMAIL,GITHUB_REVIEWER_TOKEN,GITHUB_TOKEN,IS_POSTSHARP_OWNED,IS_TEAMCITY_AGENT,MetalamaLicense,NUGET_ORG_API_KEY,PostSharpLicense,SIGNSERVER_SECRET,TEAMCITY_CLOUD_TOKEN,TYPESENSE_API_KEY,VS_MARKETPLACE_ACCESS_TOKEN,VSS_NUGET_EXTERNAL_FEED_ENDPOINTS'
####

$ErrorActionPreference = "Stop"
$dockerContextDirectory = "$EngPath/docker-context"

# Function to create secrets JSON file
function New-EnvJson
{
    param(
        [string]$EnvironmentVariableList
    )

    # Parse comma-separated environment variable names
    $envVarNames = $EnvironmentVariableList -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

    # Build hashtable with environment variable values
    $envVariables = @{ }
    foreach ($envVarName in $envVarNames)
    {
        $value = [Environment]::GetEnvironmentVariable($envVarName)
        if (-not [string]::IsNullOrEmpty($value))
        {
            $envVariables[$envVarName] = $value
        }
    }

    # Add secrets from the PostSharpBuildEnv key vault, on our development machines.
    # On CI agents, these environment variables are supposed to be set by the host.
    if ($LoadEnvFromKeyVault -or ($env:IS_POSTSHARP_OWNED -and -not $env:IS_TEAMCITY_AGENT))
    {
        $moduleName = "Az.KeyVault"

        if (-not (Get-Module -ListAvailable -Name $moduleName)) {
            Write-Error "The required module '$moduleName' is not installed. Please install it with: Install-Module -Name $moduleName"
            exit 1
        }

        Import-Module $moduleName
        foreach ($secret in Get-AzKeyVaultSecret -VaultName "PostSharpBuildEnv")
        {
            $secretWithValue = Get-AzKeyVaultSecret -VaultName "PostSharpBuildEnv" -Name $secret.Name
            $envName = $secretWithValue.Name -Replace "-", "_"
            $envValue = (ConvertFrom-SecureString $secretWithValue.SecretValue -AsPlainText)
            $envVariables[$envName] = $envValue
        }
    }

    # Convert to JSON and save
    $jsonPath = Join-Path $dockerContextDirectory "env.g.json"

    # Write a test JSON file with GUID first
    @{ guid = [System.Guid]::NewGuid().ToString() } | ConvertTo-Json | Set-Content -Path $jsonPath -Encoding UTF8

    # Check if secrets file is tracked by git
    $gitStatus = git status --porcelain $jsonPath 2> $null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitStatus))
    {
        Write-Error "Secrets file '$jsonPath' is tracked by git. Please add it to .gitignore first."
        exit 1
    }

    $envVariables | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonPath -Encoding UTF8
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
if (-not $KeepEnv)
{
    if (-not $env:ENG_USERNAME)
    {
        $env:ENG_USERNAME = $env:USERNAME
    }

    # Add git identity to environment
    $env:GIT_USER_EMAIL = git config --global user.email
    $env:GIT_USER_NAME = git config --global user.name

    if ($env:IS_TEAMCITY_AGENT)
    {
        if (-not $env:GIT_USER_EMAIL)
        {
            $env:GIT_USER_EMAIL = 'teamcity@postsharp.net'
        }
        if (-not $env:GIT_USER_NAME)
        {
            $env:GIT_USER_NAME = 'teamcity'
        }
    }

    New-EnvJson -EnvironmentVariableList $EnvironmentVariables
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

# We must add a MountPoint anyway so the directory is created in the container.
$MountPoints += "c:\packages"

# Define static Git system directory for mapping
$gitSystemDir = "$BuildAgentPath\system\git"

if (Test-Path $gitSystemDir)
{
    $volumeMappings += @("-v", "${gitSystemDir}:${gitSystemDir}:ro")
    $MountPoints += $gitSystemDir
}

# Mount the host NuGet cache in the container.
if (-not $NoNuGetCache)
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

# Mount VS Remote Debugger
if ( $VsDebug)
{
    if ( -not $env:DevEnvDir)
    {
        Write-Host "Environment variable 'DevEnvDir' is not defined." -ForegroundColor Red
        exit 1
    }

    $remoteDebuggerHostDir = "$($env:DevEnvDir)Remote Debugger\x64"
    if ( -not (Test-Path $remoteDebuggerHostDir))
    {
        Write-Host "Directory '$remoteDebuggerHostDir' does not exist." -ForegroundColor Red
        exit 1
    }

    $remoteDebuggerContainerDir = "C:\msvsmon"
    $volumeMappings += @("-v", "${remoteDebuggerHostDir}:${remoteDebuggerContainerDir}:ro")
    $MountPoints += $remoteDebuggerContainerDir

}

# Discover symbolic links in source-dependencies and add their targets to mount points
$sourceDependenciesDir = Join-Path $SourceDirName "source-dependencies"
if (Test-Path $sourceDependenciesDir)
{
    $symbolicLinks = Get-ChildItem -Path $sourceDependenciesDir -Force | Where-Object { $_.LinkType -eq 'SymbolicLink' }

    foreach ($link in $symbolicLinks)
    {
        $targetPath = $link.Target
        if (-not [string]::IsNullOrEmpty($targetPath) -and (Test-Path $targetPath))
        {
            Write-Host "Found symbolic link '$( $link.Name )' -> '$targetPath'" -ForegroundColor Cyan
            $volumeMappings += @("-v", "${targetPath}:${targetPath}:ro")
            $MountPoints += $targetPath
        }
        else
        {
            Write-Host "Warning: Symbolic link '$( $link.Name )' target '$targetPath' does not exist or is invalid" -ForegroundColor Yellow
        }
    }
}

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

    # Delete now and not in the container because it's much faster and lock error messages are more relevant.
    Write-Host "Building the product in the container." -ForegroundColor Green

    # Prepare Build.ps1 arguments
    if ( $VsDebug )
    {
        $BuildArgs = @("-VsDebug") + $BuildArgs
    }

    if ( $Interactive )
    {
        $pwshArgs = "-NoExit"
        $BuildArgs = @("-Interactive") + $BuildArgs
        $dockerArgs = @("-it")
    }
    else
    {
        $pwshArgs = "-NonInteractive"
        $dockerArgs = @()
    }

    $buildArgsString = $BuildArgs -join " "
    $volumeMappingsAsString = $volumeMappings -join " "
    $dockerArgsAsString = $dockerArgs -join " "


    Write-Host "Executing: ``docker run --rm --memory=12g $volumeMappingsAsString -w $SourceDirName $dockerArgsAsString $ImageName pwsh $pwshArgs -Command `"& .\Build.ps1 $buildArgsString`"``." -ForegroundColor Cyan

    docker run --rm --memory=12g @volumeMappings -w $SourceDirName @dockerArgs $ImageName pwsh $pwshArgs -Command "& .\Build.ps1 $buildArgsString"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Docker run (build) failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
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
