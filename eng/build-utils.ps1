# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Import Arcade functions
. (Join-Path $PSScriptRoot "common\tools.ps1")

$VSSetupDir = Join-Path $ArtifactsDir "VSSetup\$configuration"
$PackagesDir = Join-Path $ArtifactsDir "packages\$configuration"
$PublishDataUrl = "https://raw.githubusercontent.com/dotnet/roslyn/main/eng/config/PublishData.json"

$binaryLog = if (Test-Path variable:binaryLog) { $binaryLog } else { $false }
$nodeReuse = if (Test-Path variable:nodeReuse) { $nodeReuse } else { $false }
$properties = if (Test-Path variable:properties) { $properties } else { @() }
$originalTemp = $env:TEMP;

function GetProjectOutputBinary([string]$fileName, [string]$projectName = "", [string]$configuration = $script:configuration, [string]$tfm = "net472", [string]$rid = "", [bool]$published = $false) {
  $projectName = if ($projectName -ne "") { $projectName } else { [System.IO.Path]::GetFileNameWithoutExtension($fileName) }
  $publishDir = if ($published) { "publish\" } else { "" }
  $ridDir = if ($rid -ne "") { "$rid\" } else { "" }
  return Join-Path $ArtifactsDir "bin\$projectName\$configuration\$tfm\$ridDir$publishDir$fileName"
}

function GetPublishData() {
  if (Test-Path variable:global:_PublishData) {
    return $global:_PublishData
  }

  Write-Host "Downloading $PublishDataUrl"
  $content = (Invoke-WebRequest -Uri $PublishDataUrl -UseBasicParsing).Content

  return $global:_PublishData = ConvertFrom-Json $content
}

function GetBranchPublishData([string]$branchName) {
  $data = GetPublishData

  if (Get-Member -InputObject $data.branches -Name $branchName) {
    return $data.branches.$branchName
  } else {
    return $null
  }
}

function GetFeedPublishData() {
  $data = GetPublishData
  return $data.feeds
}

function GetPackagesPublishData([string]$packageFeeds) {
  $data = GetPublishData
  if (Get-Member -InputObject $data.packages -Name $packageFeeds) {
    return $data.packages.$packageFeeds
  } else {
    return $null
  }
}

function GetReleasePublishData([string]$releaseName) {
  $data = GetPublishData

  if (Get-Member -InputObject $data.releases -Name $releaseName) {
    return $data.releases.$releaseName
  } else {
    return $null
  }
}

function Exec-CommandCore([string]$command, [string]$commandArgs, [switch]$useConsole = $true, [switch]$echoCommand = $true) {
  if ($echoCommand) {
    Write-Host "$command $commandArgs"
  }

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
    # If we didn't finish then an error occurred or the user hit ctrl-c.  Either
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
# The -useConsole argument controls if the process should re-use the current
# console for output or return output as a string
function Exec-Command([string]$command, [string]$commandArgs, [switch]$useConsole = $false, [switch]$echoCommand = $true) {
  if ($args -ne "") {
    throw "Extra arguments passed to Exec-Command: $args"
  }
  Exec-CommandCore -command $command -commandArgs $commandArgs -useConsole:$useConsole -echoCommand:$echoCommand
}

# Handy function for executing a dotnet command without having to track down the 
# proper dotnet executable or ensure it's on the path.
function Exec-DotNet([string]$commandArgs = "", [switch]$useConsole = $true, [switch]$echoCommand = $true) {
  if ($args -ne "") {
    throw "Extra arguments passed to Exec-DotNet: $args"
  }
  $dotnet = Ensure-DotNetSdk
  Exec-CommandCore -command $dotnet -commandArgs $commandArgs -useConsole:$useConsole -echoCommand:$echoCommand
}


# Ensure the proper .NET Core SDK is available. Returns the location to the dotnet.exe.
function Ensure-DotnetSdk() {
  $dotnetInstallDir = (InitializeDotNetCli -install:$true)
  $dotnetTestPath = Join-Path $dotnetInstallDir "dotnet.exe"
  if (Test-Path -Path $dotnetTestPath) {
    return $dotnetTestPath
  }

  $dotnetTestPath = Join-Path $dotnetInstallDir "dotnet"
  if (Test-Path -Path $dotnetTestPath) {
    return $dotnetTestPath
  }

  throw "Could not find dotnet executable in $dotnetInstallDir"
}

function Test-LastExitCode() {
  if ($LASTEXITCODE -ne 0) {
    throw "Last command failed with exit code $LASTEXITCODE"
  }
}

# Walks up the source tree, starting at the given file's directory, and returns a FileInfo object for the first .csproj file it finds, if any.
function Get-ProjectFile([object]$fileInfo) {
  Push-Location

  Set-Location $fileInfo.Directory
  try {
    while ($true) {
      # search up from the current file for a folder containing a project file
      $files = Get-ChildItem *.csproj,*.vbproj
      if ($files) {
        return $files[0]
      }
      else {
        $location = Get-Location
        Set-Location ..
        if ((Get-Location).Path -eq $location.Path) {
          # our location didn't change. We must be at the drive root, so give up
          return $null
        }
      }
    }
  }
  finally {
    Pop-Location
  }
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
  return Get-VersionCore $name (Join-Path $EngRoot "Versions.props")
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

function Subst-TempDir() {
  if ($ci) {
    Exec-Command "subst" "T: $TempDir"

    $env:TEMP='T:\'
    $env:TMP='T:\'
  }
}

function Unsubst-TempDir() {
  if ($ci) {
    Exec-Command "subst" "T: /d"

    # Restore the original temp directory
    $env:TEMP=$originalTemp
    $env:TMP=$originalTemp
  }
}
