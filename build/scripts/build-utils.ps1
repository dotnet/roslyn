# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Declare a number of useful variables for other scripts to use
[string]$repoDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")
[string]$binariesDir = Join-Path $repoDir "Binaries"

# Handy function for executing a command in powershell and throwing if it 
# fails.  
# 
# Original sample came from: http://jameskovacs.com/2010/02/25/the-exec-problem/
function Exec([scriptblock]$cmd, [string]$errorMessage = "Error executing command: " + $cmd) { 
    $output = & $cmd 
    if (-not $?) {
        Write-Host $output
        throw $errorMessage 
    } 
}

# Ensure that NuGet is installed 
function Ensure-NuGet() {
    $nuget = Join-Path $repoDir "NuGet.exe"
    if (-not (Test-Path $nuget)) {
        Exec { & (Join-Path $PSScriptRoot "download-nuget.ps1") }
    }

    return $nuget
}

function Create-Directory([string]$dir) {
    New-Item $dir -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
}

# Return the version of the NuGet package as used in this repo
function Get-PackageVersion([string]$name) {
    $deps = Join-Path $repoDir "build\Targets\Dependencies.props"
    $nodeName = "$($name)Version"
    $x = [xml](Get-Content -raw $deps)
    $node = $x.Project.PropertyGroup[$nodeName]
    if ($node -eq $null) { 
        throw "Cannot find package $name in Dependencies.props"
    }

    return $node.InnerText
}

# Locate the directory where our NuGet packages will be deployed.  Needs to be kept in sync
# with the logic in Version.props
function Get-PackagesDir {
    $d = $null
    if ($env:NUGET_PACKAGES -ne $null) {
        $d = $env:NUGET_PACKAGES
    }
    else {
        $d = Join-Path $env:UserProfile ".nuget\packages\"
    }

    Create-Directory $d
    return $d
}

# The intent of this script is to locate and return the path to the MSBuild directory that
# we should use for bulid operations.  The preference order for MSBuild to use is as 
# follows
#
#   1. MSBuild from an active VS command prompt
#   2. MSBuild from a machine wide VS install
#   3. MSBuild from Dev14 (legacy microbuild scenario)
#
# This function will return two values: the kind of MSBuild chosen and the MSBuild directory.
function Get-MSBuildDirCore() {

    # MSBuild from an active VS command prompt.  
    if (${env:VSINSTALLDIR} -ne $null) {

        # This line deliberately avoids using -ErrorAction.  Inside a VS command prompt
        # an MSBuild command should always be available.
        $p = (Get-Command msbuild).Path
        $p = Split-Path -parent $p
        Write-Output "vscmd"
        Write-Output $p
        return
    }

    # Look for a valid VS installation
    try {
        $p = Get-VisualStudioDir
        $p = Join-Path $p "MSBuild\15.0\Bin"
        Write-Output "vsinstall"
        Write-Output $p
        return
    }
    catch { 
        # Failures are expected here when no VS installation is present on the 
        # machine.
    }

    throw "Unable to find MSBuild"
}

function Get-MSBuildDir() {
    $both = Get-MSBuildDirCore
    return $both[1]
}

# Get the directory of the first Visual Studio which meets our minimal 
# requirements for the Roslyn repo
function Get-VisualStudioDir() {
    $vswhereVersion = "1.0.50"
    $vswhere = Join-Path (Get-PackagesDir) "vswhere\$($vswhereVersion)\tools\vswhere.exe"
    if (-not (Test-path $vswhere)) {
        $nuget = Ensure-NuGet
        Exec { & $nuget install "vswhere" -OutputDirectory (Get-PackagesDir) -Version $vswhereVersion }
    }

    $output = & $vswhere -requires Microsoft.Component.MSBuild -format json | Out-String
    if (-not $?) {
        throw "Could not locate a valid Visual Studio"
    }

    $j = ConvertFrom-Json $output
    $p = $j[0].installationPath
    return $p
}

