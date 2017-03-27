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
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        Write-Host $output
        throw $errorMessage 
    } 
}

# Ensure that NuGet is installed and return the path to the 
# executable to use.
function Ensure-NuGet() {
    Exec { & (Join-Path $PSScriptRoot "download-nuget.ps1") }
    $nuget = Join-Path $repoDir "NuGet.exe"
    return $nuget
}

# Ensure a basic tool used for building our Repo is installed and 
# return the path to it.
function Ensure-BasicTool([string]$name, [string]$version) {
    $p = Join-Path (Get-PackagesDir) "$($name).$($version)"
    if (-not (Test-Path $p)) {
        $nuget = Ensure-NuGet
        Exec { & $nuget install $name -OutputDirectory (Get-PackagesDir) -Version $version }
    }
    
    return $p
}

# Ensure that MSBuild is installed and return the path to the
# executable to use.
function Ensure-MSBuild([switch]$xcopy = $false) {
    $both = Get-MSBuildKindAndDir -xcopy:$xcopy
    $msbuildDir = $both[1]
    switch ($both[0]) {
        "xcopy" { break; }
        "vscmd" { break; }
        "vsinstall" { break; }
        default {
            throw "Unknown MSBuild installation type $($both[0])"
        }
    }

    $p = Join-Path $msbuildDir "msbuild.exe"
    return $p
}

function Create-Directory([string]$dir) {
    New-Item $dir -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
}

# Return the version of the NuGet package as used in this repo
function Get-PackageVersion([string]$name) {
    $name = $name.Replace(".", "")
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
function Get-PackagesDir() {
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

# Locate the directory of a specific NuGet package which is restored via our main 
# toolset values.
function Get-PackageDir([string]$name, [string]$version = "") {
    if ($version -eq "") {
        $version = Get-PackageVersion $name
    }

    $p = Get-PackagesDir
    $p = Join-Path $p $name
    $p = Join-Path $p $version
    return $p
}

# The intent of this script is to locate and return the path to the MSBuild directory that
# we should use for bulid operations.  The preference order for MSBuild to use is as 
# follows
#
#   1. MSBuild from an active VS command prompt
#   2. MSBuild from a machine wide VS install
#   3. MSBuild from the xcopy toolset 
#
# This function will return two values: the kind of MSBuild chosen and the MSBuild directory.
function Get-MSBuildKindAndDir([switch]$xcopy = $false) {

    if ($xcopy) { 
        Write-Output "xcopy"
        Write-Output (Get-MSBuildDirXCopy)
        return
    }

    # MSBuild from an active VS command prompt.  
    if (${env:VSINSTALLDIR} -ne $null) {

        # This line deliberately avoids using -ErrorAction.  Inside a VS command prompt
        # an MSBuild command should always be available.
        $command = (Get-Command msbuild -ErrorAction SilentlyContinue)
        if ($command -ne $null) {
            $p = Split-Path -parent $command.Path
            Write-Output "vscmd"
            Write-Output $p
            return
        }
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

    Write-Output "xcopy"
    Write-Output (Get-MSBuildDirXCopy)
    return
}

# Locate the xcopy version of MSBuild
function Get-MSBuildDirXCopy() {
    $version = "0.1.2"
    $name = "RoslynTools.MSBuild"
    $p = Ensure-BasicTool $name $version
    $p = Join-Path $p "tools\msbuild"
    return $p
}

function Get-MSBuildDir([switch]$xcopy = $false) {
    $both = Get-MSBuildKindAndDir -xcopy:$xcopy
    return $both[1]
}

# Get the directory of the first Visual Studio which meets our minimal 
# requirements for the Roslyn repo
function Get-VisualStudioDir() {
    $vswhere = Join-Path (Ensure-BasicTool "vswhere" "1.0.50") "tools\vswhere.exe"
    $output = & $vswhere -requires Microsoft.Component.MSBuild -format json | Out-String
    if (-not $?) {
        throw "Could not locate a valid Visual Studio"
    }

    $j = ConvertFrom-Json $output
    $p = $j[0].installationPath
    return $p
}

# Clear out the NuGet package cache
function Clear-PackageCache() {
    $nuget = Ensure-NuGet
    Exec { & $nuget locals all -clear }
}

# Restore a single project
function Restore-Project([string]$fileName, [string]$nuget, [string]$msbuildDir) {
    $nugetConfig = Join-Path $repoDir "nuget.config"
    $filePath = Join-Path $repoDir $fileName
    Exec { & $nuget restore -verbosity quiet -configfile $nugetConfig -MSBuildPath $msbuildDir -Project2ProjectTimeOut 1200 $filePath }
}

# Restore all of the projects that the repo consumes
function Restore-Packages([switch]$clean = $false, [string]$msbuildDir = "", [string]$project = "") {
    $nuget = Ensure-NuGet
    if ($msbuildDir -eq "") {
        $msbuildDir = Get-MSBuildDir
    }

    Write-Host "Restore using MSBuild at $msbuildDir"

    if ($clean) {
        Write-Host "Deleting project.lock.json files"
        Get-ChildItem $repoDir -re -in project.lock.json | Remove-Item
    }

    if ($project -ne "") {
        Write-Host "Restoring project $project"
        Restore-Project -fileName $project -nuget $nuget -msbuildDir $msbuildDir
    }
    else {
        $all = @(
            "Toolsets:build\ToolsetPackages\project.json",
            "Toolsets (Dev14 VS SDK build tools):build\ToolsetPackages\dev14.project.json",
            "Toolsets (Dev15 VS SDK RC build tools):build\ToolsetPackages\dev15rc.project.json",
            "Samples:src\Samples\Samples.sln",
            "Templates:src\Setup\Templates\Templates.sln",
            "Toolsets Compiler:build\Toolset\Toolset.csproj",
            "Roslyn:Roslyn.sln",
            "DevDivInsertionFiles:src\Setup\DevDivInsertionFiles\DevDivInsertionFiles.sln",
            "DevDiv Roslyn Packages:src\Setup\DevDivPackages\Roslyn\project.json",
            "DevDiv Debugger Packages:src\Setup\DevDivPackages\Debugger\project.json")

        foreach ($cur in $all) {
            $both = $cur.Split(':')
            Write-Host "Restoring $($both[0])"
            Restore-Project -fileName $both[1] -nuget $nuget -msbuildDir $msbuildDir
        }
    }
}

# Restore all of the projects that the repo consumes
function Restore-All([switch]$clean = $false, [string]$msbuildDir = "") {
    Restore-Packages -clean:$clean -msbuildDir $msbuildDir
}

