# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Declare a number of useful variables for other scripts to use
[string]$repoDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")
[string]$binariesDir = Join-Path $repoDir "Binaries"
[string]$scriptDir = $PSScriptRoot

function Create-Directory([string]$dir) {
    New-Item $dir -ItemType Directory -ErrorAction SilentlyContinue | out-null
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
