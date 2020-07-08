# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Import Arcade functions
. (Join-Path $PSScriptRoot "common\tools.ps1")

$VSSetupDir = Join-Path $ArtifactsDir "VSSetup\$configuration"
$PackagesDir = Join-Path $ArtifactsDir "packages\$configuration"
$PublishDataUrl = "https://raw.githubusercontent.com/dotnet/roslyn/master/eng/config/PublishData.json"

$binaryLog = if (Test-Path variable:binaryLog) { $binaryLog } else { $false }
$nodeReuse = if (Test-Path variable:nodeReuse) { $nodeReuse } else { $false }
$bootstrapDir = if (Test-Path variable:bootstrapDir) { $bootstrapDir } else { "" }
$bootstrapConfiguration = if (Test-Path variable:bootstrapConfiguration) { $bootstrapConfiguration } else { "Release" }
$properties = if (Test-Path variable:properties) { $properties } else { @() }

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

function Run-MSBuild([string]$projectFilePath, [string]$buildArgs = "", [string]$logFileName = "", [switch]$parallel = $true, [switch]$summary = $true, [switch]$warnAsError = $true, [string]$configuration = $script:configuration, [switch]$runAnalyzers = $false) {
  # Because we override the C#/VB toolset to build against our LKG package, it is important
  # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
  # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
  # MSBuildAdditionalCommandLineArgs=
  $args = "/p:TreatWarningsAsErrors=true /nologo /nodeReuse:false /p:Configuration=$configuration ";

  if ($warnAsError) {
    $args += " /warnaserror"
  }

  if ($summary) {
    $args += " /consoleloggerparameters:Verbosity=minimal;summary"
  } else {        
    $args += " /consoleloggerparameters:Verbosity=minimal"
  }

  if ($parallel) {
    $args += " /m"
  }

  if ($runAnalyzers) {
    $args += " /p:UseRoslynAnalyzers=true"
  }

  if ($binaryLog) {
    if ($logFileName -eq "") {
      $logFileName = [IO.Path]::GetFileNameWithoutExtension($projectFilePath)
    }
    $logFileName = [IO.Path]::ChangeExtension($logFileName, ".binlog")
    $logFilePath = Join-Path $LogDir $logFileName
    $args += " /bl:$logFilePath"
  }

  if ($officialBuildId) {
    $args += " /p:OfficialBuildId=" + $officialBuildId
  }

  if ($ci) {
    $args += " /p:ContinuousIntegrationBuild=true"
  }

  if ($bootstrapDir -ne "") {
    $args += " /p:BootstrapBuildPath=$bootstrapDir"
  }

  $args += " $buildArgs"
  $args += " $projectFilePath"
  $args += " $properties"

  $buildTool = InitializeBuildTool
  Exec-Console $buildTool.Path "$($buildTool.Command) $args"
}

# Create a bootstrap build of the compiler.  Returns the directory where the bootstrap build
# is located.
#
# Important to not set $script:bootstrapDir here yet as we're actually in the process of
# building the bootstrap.
function Make-BootstrapBuild([switch]$force32 = $false) {
  Write-Host "Building bootstrap compiler"

  $dir = Join-Path $ArtifactsDir "Bootstrap"
  Remove-Item -re $dir -ErrorAction SilentlyContinue
  Create-Directory $dir

  $packageName = "Microsoft.Net.Compilers.Toolset"
  $projectPath = "src\NuGet\$packageName\$packageName.Package.csproj"
  $force32Flag = if ($force32) { " /p:BOOTSTRAP32=true" } else { "" }

  Run-MSBuild $projectPath "/restore /t:Pack /p:RoslynEnforceCodeStyle=false /p:UseRoslynAnalyzers=false /p:DotNetUseShippingVersions=true /p:InitialDefineConstants=BOOTSTRAP /p:PackageOutputPath=`"$dir`" /p:EnableNgenOptimization=false /p:PublishWindowsPdb=false $force32Flag" -logFileName "Bootstrap" -configuration $bootstrapConfiguration -runAnalyzers
  $packageFile = Get-ChildItem -Path $dir -Filter "$packageName.*.nupkg"
  Unzip "$dir\$packageFile" $dir

  Write-Host "Cleaning Bootstrap compiler artifacts"
  Run-MSBuild $projectPath "/t:Clean" -logFileName "BootstrapClean"

  return $dir
}
