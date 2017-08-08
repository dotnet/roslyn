# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Declare a number of useful variables for other scripts to use
[string]$repoDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")
[string]$binariesDir = Join-Path $repoDir "Binaries"

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

function Exec-CommandCore([string]$command, [string]$commandArgs, [switch]$useConsole = $true) {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $command
    $startInfo.Arguments = $commandArgs

    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = Get-Location

    if (-not $useConsole) {
       $startInfo.RedirectStandardOutput = $true
       $startInfo.CreateNoWindow = $true
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $process.Start() | Out-Null

    $finished = $false
    try {
        if (-not $useConsole) { 
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
        }

        while (-not $process.WaitForExit(100)) { 
            # Non-blocking loop done to allow ctr-c interrupts
        }

        $finished = $true
        if ($process.ExitCode -ne 0) { 
            throw "Command failed to execute: $command $commandArgs" 
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

# Ensure that NuGet is installed and return the path to the 
# executable to use.
function Ensure-NuGet() {
    $nugetVersion = "4.1.0"
    $toolsDir = Join-Path $binariesDir "Tools"
    Create-Directory $toolsDir

    $destFile = Join-Path $toolsDir "NuGet.exe"
    $versionFile = Join-Path $toolsDir "NuGet.exe.version"

    # Check and see if we already have a NuGet.exe which exists and is the correct
    # version.
    if ((Test-Path $destFile) -and (Test-Path $versionFile)) {
        $scratchVersion = Get-Content $versionFile
        if ($scratchVersion -eq $nugetVersion) {
            return $destFile
        }
    }

    Write-Host "Downloading NuGet.exe"
    $webClient = New-Object -TypeName "System.Net.WebClient"
    $webClient.DownloadFile("https://dist.nuget.org/win-x86-commandline/v$nugetVersion/NuGet.exe", $destFile)
    $nugetVersion | Out-File $versionFile
    return $destFile
}

# Checks to see if a particular version of the SDK is available on %PATH%. This is 
# how MSBuild locates the SDK. 
function Test-SdkInPath([string]$version) {
    foreach ($part in ${env:PATH}.Split(';')) {
        $dotnetExe = Join-Path $part "dotnet.exe"
        if (Test-Path $dotnetExe) {
            $sdkPath = Join-Path $part "sdk"
            $sdkPath = Join-Path $sdkPath $version
            return Test-Path $sdkPath
        }
    }

    return $false
}

# Ensure the proper SDK in installed in our %PATH%. This is how MSBuild locates the 
# SDK.
function Ensure-SdkInPath() { 
    $sdkVersion = "2.0.0-preview3-006923"
    if (Test-SdkInPath $sdkVersion) {
        return        
    }

    $toolsDir = Join-Path $binariesDir "Tools"
    $cliDir = Join-Path $toolsDir "dotnet"
    $dotnetExe = Join-Path $cliDir "dotnet.exe"
    if (-not (Test-Path $dotnetExe)) { 
        Write-Host "Downloading CLI $sdkVersion"
        Create-Directory $cliDir
        Create-Directory $toolsDir
        $destFile = Join-Path $toolsDir "dotnet-install.ps1"
        $webClient = New-Object -TypeName "System.Net.WebClient"
        $webClient.DownloadFile("https://dot.net/v1/dotnet-install.ps1", $destFile)
        Exec-Block { & $destFile -Version $sdkVersion -InstallDir $cliDir }
    }

    ${env:PATH} = "$cliDir;${env:PATH}"
}

# Ensure a basic tool used for building our Repo is installed and 
# return the path to it.
function Ensure-BasicTool([string]$name, [string]$version = "") {
    if ($version -eq "") { 
        $version = Get-PackageVersion $name
    }

    $p = Join-Path (Get-PackagesDir) "$($name).$($version)"
    if (-not (Test-Path $p)) {
        $nuget = Ensure-NuGet
        Exec-Block { & $nuget install $name -OutputDirectory (Get-PackagesDir) -Version $version } | Out-Null
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
    Ensure-SdkInPath
    return $p
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

# Return the version of the NuGet package as used in this repo
function Get-PackageVersion([string]$name) {
    $name = $name.Replace(".", "")
    $deps = Join-Path $repoDir "build\Targets\Packages.props"
    $nodeName = "$($name)Version"
    $x = [xml](Get-Content -raw $deps)
    $node = $x.Project.PropertyGroup.FirstChild
    while ($node -ne $null) {
        if ($node.Name -eq $nodeName) {
            return $node.InnerText
        }
        $node = $node.NextSibling
    }

    throw "Cannot find package $name in Packages.props"
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

    # MSBuild from an active VS command prompt.  
    if (${env:VSINSTALLDIR} -ne $null) {
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
    $p = Ensure-BasicTool "RoslynTools.MSBuild"
    $p = Join-Path $p "tools\msbuild"
    return $p
}

function Get-MSBuildDir([switch]$xcopy = $false) {
    $both = Get-MSBuildKindAndDir -xcopy:$xcopy
    return $both[1]
}

# Get the directory and instance ID of the first Visual Studio version which 
# meets our minimal requirements for the Roslyn repo.
function Get-VisualStudioDirAndId() {
    $vswhere = Join-Path (Ensure-BasicTool "vswhere") "tools\vswhere.exe"
    $output = Exec-Command $vswhere "-requires Microsoft.Component.MSBuild -format json" | Out-String
    $j = ConvertFrom-Json $output
    foreach ($obj in $j) { 

        # Need to be using at least Visual Studio 15.2 in order to have the appropriate
        # set of SDK fixes. Parsing the installationName is the only place where this is 
        # recorded in that form.
        $name = $obj.installationName
        if ($name -match "VisualStudio(Preview)?/([\d.]+)(\+|-).*") { 
            $minVersion = New-Object System.Version "15.1.0"
            $version = New-Object System.Version $matches[2]
            if ($version -ge $minVersion) {
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
    $nuget = Ensure-NuGet
    Exec-Block { & $nuget locals all -clear } | Out-Host
}

# Restore a single project
function Restore-Project([string]$fileName, [string]$nuget, [string]$msbuildDir) {
    $nugetConfig = Join-Path $repoDir "nuget.config"

    $filePath = $fileName
    if (-not (Test-Path $filePath)) {
        $filePath = Join-Path $repoDir $fileName
    }

    Exec-Block { & $nuget restore -verbosity quiet -configfile $nugetConfig -MSBuildPath $msbuildDir -Project2ProjectTimeOut 1200 $filePath } | Write-Host
}

# Restore all of the projects that the repo consumes
function Restore-Packages([string]$msbuildDir = "", [string]$project = "") {
    $nuget = Ensure-NuGet
    if ($msbuildDir -eq "") {
        $msbuildDir = Get-MSBuildDir
    }

    Write-Host "Restore using MSBuild at $msbuildDir"

    if ($project -ne "") {
        Write-Host "Restoring project $project"
        Restore-Project -fileName $project -msbuildDir $msbuildDir -nuget $nuget
    }
    else {
        $all = @(
            "Base Toolset:build\ToolsetPackages\BaseToolset.csproj",
            "Roslyn:Roslyn.sln",
            "Samples:src\Samples\Samples.sln",
            "Templates:src\Setup\Templates\Templates.sln",
            "DevDivInsertionFiles:src\Setup\DevDivInsertionFiles\DevDivInsertionFiles.sln")

        foreach ($cur in $all) {
            $both = $cur.Split(':')
            Write-Host "Restoring $($both[0])"
            Restore-Project -fileName $both[1] -msbuildDir $msbuildDir -nuget $nuget
        }
    }
}

# Restore all of the projects that the repo consumes
function Restore-All([string]$msbuildDir = "") {
    Restore-Packages -msbuildDir $msbuildDir
}

