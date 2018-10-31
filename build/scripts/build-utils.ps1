# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Import Arcade functions
. (Join-Path $PSScriptRoot "tools.ps1")

[string]$binariesDir = Join-Path $RepoRoot "Binaries"

# Handy function for executing a command in powershell and throwing if it 
# fails.  
#
# Use this when the full command is known at script authoring time and 
# doesn't require any dynamic argument build up.  Example:
#
#   Exec-Block { & $msbuild Test.proj }
# 
# Original sample came from: http://jameskovacs.com/2010/02/25/the-exec-problem/
function Exec-Block([scriptblock]$cmd) {
    & $cmd

    # Need to check both of these cases for errors as they represent different items
    # - $?: did the powershell script block throw an error
    # - $lastexitcode: did a windows command executed by the script block end in error
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        throw "Command failed to execute: $cmd"
    } 
}

# This will exec a process using the console and return it's exit code. This will not 
# throw when the process fails.
function Exec-Process([string]$command, [string]$commandArgs) {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $command
    $startInfo.Arguments = $commandArgs
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = Get-Location

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $process.Start() | Out-Null

    $finished = $false
    try {
        while (-not $process.WaitForExit(100)) { 
            # Non-blocking loop done to allow ctr-c interrupts
        }

        $finished = $true
        $process.ExitCode
    }
    finally {
        # If we didn't finish then an error occured or the user hit ctrl-c.  Either
        # way kill the process
        if (-not $finished) {
            $process.Kill()
        }
    }
}

function Exec-CommandCore([string]$command, [string]$commandArgs, [switch]$useConsole = $true) {
    if ($useConsole) {
        $exitCode = Exec-Process $command $commandArgs
        if ($exitCode -ne 0) { 
            throw "Command failed to execute with exit code $($exitCode): $command $commandArgs" 
        }
        return
    }

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $command
    $startInfo.Arguments = $commandArgs

    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = Get-Location
    $startInfo.RedirectStandardOutput = $true
    $startInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $process.Start() | Out-Null

    $finished = $false
    try {
        # The OutputDataReceived event doesn't fire as events are sent by the 
        # process in powershell.  Possibly due to subtlties of how Powershell
        # manages the thread pool that I'm not aware of.  Using blocking
        # reading here as an alternative which is fine since this blocks 
        # on completion already.
        $out = $process.StandardOutput
        while (-not $out.EndOfStream) {
            $line = $out.ReadLine()
            Write-Output $line
        }

        while (-not $process.WaitForExit(100)) { 
            # Non-blocking loop done to allow ctr-c interrupts
        }

        $finished = $true
        if ($process.ExitCode -ne 0) { 
            throw "Command failed to execute with exit code $($process.ExitCode): $command $commandArgs" 
        }
    }
    finally {
        # If we didn't finish then an error occured or the user hit ctrl-c.  Either
        # way kill the process
        if (-not $finished) {
            $process.Kill()
        }
    }
}

# Handy function for executing a windows command which needs to go through 
# windows command line parsing.  
#
# Use this when the command arguments are stored in a variable.  Particularly 
# when the variable needs reparsing by the windows command line. Example:
#
#   $args = "/p:ManualBuild=true Test.proj"
#   Exec-Command $msbuild $args
# 
function Exec-Command([string]$command, [string]$commandArgs) {
    Exec-CommandCore -command $command -commandArgs $commandargs -useConsole:$false
}

# Functions exactly like Exec-Command but lets the process re-use the current 
# console. This means items like colored output will function correctly.
#
# In general this command should be used in place of
#   Exec-Command $msbuild $args | Out-Host
#
function Exec-Console([string]$command, [string]$commandArgs) {
    Exec-CommandCore -command $command -commandArgs $commandargs -useConsole:$true
}

# Handy function for executing a powershell script in a clean environment with 
# arguments.  Prefer this over & sourcing a script as it will both use a clean
# environment and do proper error checking
function Exec-Script([string]$script, [string]$scriptArgs = "") {
    Exec-Command "powershell" "-noprofile -executionPolicy RemoteSigned -file `"$script`" $scriptArgs"
}

# Ensure the proper SDK in installed in our %PATH%. This is how MSBuild locates the 
# SDK. Returns the location to the dotnet exe
function Ensure-DotnetSdk() {
  if (-not (Test-Path global:_dotNetExe)) {
    $global:_dotNetExe = Join-Path (InitializeDotNetCli -install:$true) "dotnet.exe"
  }

  return $global:_dotNetExe
}
# Ensure a basic tool used for building our Repo is installed and 
# return the path to it.
function Ensure-BasicTool([string]$name, [string]$version = "") {
    if ($version -eq "") { 
        $version = Get-PackageVersion $name
    }

    $p = Join-Path (Get-PackagesDir) "$($name)\$($version)"
    if (-not (Test-Path $p)) {
        $toolsetProject = Join-Path $RepoRoot "build\ToolsetPackages\RoslynToolset.csproj"
        Write-Host "Downloading $name"
        Restore-Project $toolsetProject
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

    return Join-Path $msbuildDir "msbuild.exe"
}

# Returns the msbuild exe path and directory as a single return. This makes it easy 
# to do one line MSBuild configuration in scripts
#   $msbuild, $msbuildDir = Ensure-MSBuildAndDir
function Ensure-MSBuildAndDir([string]$msbuildDir) {
    if ($msbuildDir -eq "") {
        $msbuild = Ensure-MSBuild
        $msbuildDir = Split-Path -parent $msbuild
    }
    else {
        $msbuild = Join-Path $msbuildDir "msbuild.exe"
    }

    return $msbuild, $msbuildDir
}

function Create-Directory([string]$dir) {
    New-Item $dir -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
}

function Get-VersionCore([string]$name, [string]$versionFile) {
    $name = $name.Replace(".", "")
    $name = $name.Replace("-", "")
    $nodeName = "$($name)Version"
    $x = [xml](Get-Content -raw $versionFile)
    $node = $x.SelectSingleNode("//Project/PropertyGroup/$nodeName")
    if ($node -ne $null) {
        return $node.InnerText
    }

    throw "Cannot find package $name in $versionFile"

}

# Return the version of the NuGet package as used in this repo
function Get-PackageVersion([string]$name) {
    return Get-VersionCore $name (Join-Path $RepoRoot "build\Targets\Packages.props")
}

# Return the version of the specified tool
function Get-ToolVersion([string]$name) {
    return Get-VersionCore $name (Join-Path $RepoRoot "build\Targets\Tools.props")
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
    $p = Join-Path $p $name.ToLowerInvariant()
    $p = Join-Path $p $version
    return $p
}

# The intent of this script is to locate and return the path to the MSBuild directory that
# we should use for bulid operations.  The preference order for MSBuild to use is as 
# follows:
#
#   1. MSBuild from an active VS command prompt
#   2. MSBuild from a compatible VS installation
#   3. MSBuild from the xcopy toolset 
#
# This function will return two values: the kind of MSBuild chosen and the MSBuild directory.
function Get-MSBuildKindAndDir([switch]$xcopy = $false) {

    if ($xcopy) { 
        Write-Output "xcopy"
        Write-Output (Get-MSBuildDirXCopy)
        return
    }

    # MSBuild from an active VS command prompt. Use the MSBuild here so long as it's from a 
    # compatible Visual Studio. If not though throw and error out. Given the number of 
    # environment variable changes in a developer command prompt it's hard to make guarantees
    # about subbing in a new MSBuild instance
    if (${env:VSINSTALLDIR} -ne $null) {
        $command = (Get-Command msbuild -ErrorAction SilentlyContinue)
        if ((Test-SupportedVisualStudioVersion ${env:VSCMD_VER}) -and ($command -ne $null) ) {
            $p = Split-Path -parent $command.Path
            Write-Output "vscmd"
            Write-Output $p
            return
        }
        else {
            $vsMinimumVersion = Get-ToolVersion "vsMinimum"
            throw "Developer Command Prompt for VS $(${env:VSCMD_VER}) is not recent enough. Please upgrade to {$vsMinimumVersion} or build from a normal CMD window"
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
    $p = Ensure-BasicTool "RoslynTools.MSBuild"
    $p = Join-Path $p "tools\MSBuild\15.0\Bin"
    return $p
}

function Get-MSBuildDir([switch]$xcopy = $false) {
    $both = Get-MSBuildKindAndDir -xcopy:$xcopy
    return $both[1]
}


# Dose this version of Visual Studio meet our minimum requirements for building.
function Test-SupportedVisualStudioVersion([string]$version) { 
    # This regex allows us to strip off any pre-release info that gets attached 
    # to the version string. VS uses NuGet style pre-release by suffing version
    # with -<pre-release info>
    if (-not ($version -match "^([\d.]+)(\+|-)?.*$")) { 
        return $false
    }

    $vsMinimumVersion = Get-ToolVersion "vsMinimum"
    $V = New-Object System.Version $matches[1]
    $min = New-Object System.Version $vsMinimumVersion
    return $v -ge $min;
}

# Get the directory and instance ID of the first Visual Studio version which 
# meets our minimal requirements for the Roslyn repo.
function Get-VisualStudioDirAndId() {
    $vswhere = Join-Path (Ensure-BasicTool "vswhere") "tools\vswhere.exe"
    $output = Exec-Command $vswhere "-prerelease -requires Microsoft.Component.MSBuild -format json" | Out-String
    $j = ConvertFrom-Json $output
    foreach ($obj in $j) { 

        # Need to be using at least Visual Studio 15.2 in order to have the appropriate
        # set of SDK fixes. Parsing the installationName is the only place where this is 
        # recorded in that form.
        $name = $obj.installationName
        if ($name -match "VisualStudio(Preview)?/(.*)") { 
            if (Test-SupportedVisualStudioVersion $matches[2]) {
                Write-Output $obj.installationPath
                Write-Output $obj.instanceId
                return
            }
        }
        else {
            Write-Host "Unrecognized installationName format $name"
        }
    }

    throw "Could not find a suitable Visual Studio Version"
}

# Get the directory of the first Visual Studio which meets our minimal 
# requirements for the Roslyn repo
function Get-VisualStudioDir() {
    $both = Get-VisualStudioDirAndId
    return $both[0]
}

# Clear out the NuGet package cache
function Clear-PackageCache() {
    $dotnet = Ensure-DotnetSdk
    Exec-Console $dotnet "nuget locals all --clear"
}

# Restore a single project
function Restore-Project([string]$projectFileName, [string]$logFilePath = "") {
    $projectFilePath = $projectFileName
    if (-not (Test-Path $projectFilePath)) {
        $projectFilePath = Join-Path $RepoRoot $projectFileName
    }

    $logArg = ""
    if ($logFilePath -ne "") {
        $logArg = " /bl:$logFilePath"
    }

    Exec-Console (Ensure-DotNetSdk) "restore --verbosity quiet $projectFilePath $logArg"
}

function Unzip-File([string]$zipFilePath, [string]$outputDir) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipFilePath, $outputDir)
}

