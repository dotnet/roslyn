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

# Ensure the proper .NET Core SDK is available. Returns the location to the dotnet.exe.
function Ensure-DotnetSdk() {
  if (-not (Test-Path global:_dotNetExe)) {
    $global:_dotNetExe = Join-Path (InitializeDotNetCli -install:$true) "dotnet.exe"
  }

  return $global:_dotNetExe
}

# Ensure the proper VS msbuild is available. Returns the locaqtion of the msbuild.exe.
function Ensure-MSBuild() {
  if (-not (Test-Path global:_msbuildExe)) {
    $global:_msbuildExe = InitializeVisualStudioMSBuild
  }

  return $global:_msbuildExe
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

