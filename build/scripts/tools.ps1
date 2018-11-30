# 
# TODO: This file is currently a subset of Arcade's tools.ps1.
# 

# Initialize variables if they aren't already defined

$restore = if (Test-Path variable:restore) { $restore } else { $true }
$msbuildEngine = if (Test-Path variable:msbuildEngine) { $msbuildEngine } else { $null }
$useInstalledDotNetCli = if (Test-Path variable:useInstalledDotNetCli) { $useInstalledDotNetCli } else { $true }

set-strictmode -version 2.0
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Create-Directory([string[]] $path) {
  if (!(Test-Path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function Unzip([string]$zipfile, [string]$outpath) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

function InitializeDotNetCli([bool]$install) {
  if (Test-Path global:_DotNetInstallDir) {
    return $global:_DotNetInstallDir
  }

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  $env:DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we do not need all ASP.NET packages restored.
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Source Build uses DotNetCoreSdkDir variable
  if ($env:DotNetCoreSdkDir -ne $null) {
    $env:DOTNET_INSTALL_DIR = $env:DotNetCoreSdkDir
  }

  # Find the first path on %PATH% that contains the dotnet.exe
  if ($useInstalledDotNetCli -and ($env:DOTNET_INSTALL_DIR -eq $null)) {
    $dotnetCmd = Get-Command "dotnet.exe" -ErrorAction SilentlyContinue
    if ($dotnetCmd -ne $null) {
      $env:DOTNET_INSTALL_DIR = Split-Path $dotnetCmd.Path -Parent
    }
  }

  $dotnetSdkVersion = $GlobalJson.tools.dotnet

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version,
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if (($env:DOTNET_INSTALL_DIR -ne $null) -and (Test-Path(Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetSdkVersion"))) {
    $dotnetRoot = $env:DOTNET_INSTALL_DIR
  } else {
    $dotnetRoot = Join-Path $RepoRoot ".dotnet"

    if (-not (Test-Path(Join-Path $dotnetRoot "sdk\$dotnetSdkVersion"))) {
      if ($install) {
        InstallDotNetSdk $dotnetRoot $dotnetSdkVersion
      } else {
        Write-Host "Unable to find dotnet with SDK version '$dotnetSdkVersion'" -ForegroundColor Red
        ExitWithExitCode 1
      }
    }

    $env:DOTNET_INSTALL_DIR = $dotnetRoot
  }

  return $global:_DotNetInstallDir = $dotnetRoot
}

function GetDotNetInstallScript([string] $dotnetRoot) {
  $installScript = "$dotnetRoot\dotnet-install.ps1"
  if (!(Test-Path $installScript)) {
    Create-Directory $dotnetRoot
    Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
  }

  return $installScript
}

function InstallDotNetSdk([string] $dotnetRoot, [string] $version) {
  $installScript = GetDotNetInstallScript $dotnetRoot
  & $installScript -Version $version -InstallDir $dotnetRoot
  if ($lastExitCode -ne 0) {
    Write-Host "Failed to install dotnet cli (exit code '$lastExitCode')." -ForegroundColor Red
    ExitWithExitCode $lastExitCode
  }
}

#
# Locates Visual Studio MSBuild installation. 
# The preference order for MSBuild to use is as follows:
#
#   1. MSBuild from an active VS command prompt
#   2. MSBuild from a compatible VS installation
#   3. MSBuild from the xcopy tool package
#
# Returns full path to msbuild.exe.
# Throws on failure.
#
function InitializeVisualStudioMSBuild([bool]$install) {
  if (Test-Path global:_MSBuildExe) {
    return $global:_MSBuildExe
  }

  $vsMinVersionStr = if (!$GlobalJson.tools.vs.version) { $GlobalJson.tools.vs.version } else { "15.9" }
  $vsMinVersion = [Version]::new($vsMinVersionStr) 

  # Try msbuild command available in the environment.
  if ($env:VSINSTALLDIR -ne $null) {
    $msbuildCmd = Get-Command "msbuild.exe" -ErrorAction SilentlyContinue
    if ($msbuildCmd -ne $null) {
      if ($msbuildCmd.Version -ge $vsMinVersion) {
        return $global:_MSBuildExe = $msbuildCmd.Path
      }

      # Report error - the developer environment is initialized with incompatible VS version.
      throw "Developer Command Prompt for VS $($env:VisualStudioVersion) is not recent enough. Please upgrade to $vsMinVersionStr or build from a plain CMD window"
    }
  }

  # Locate Visual Studio installation or download x-copy msbuild.
  $vsInfo = LocateVisualStudio  
  if ($vsInfo -ne $null) {
    $vsInstallDir = $vsInfo.installationPath
    $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

    InitializeVisualStudioEnvironmentVariables $vsInstallDir $vsMajorVersion
  } elseif ($install) {

    if (Get-Member -InputObject $GlobalJson.tools -Name "xcopy-msbuild") {
      $xcopyMSBuildVersion = $GlobalJson.tools.'xcopy-msbuild'
      $vsMajorVersion = $xcopyMSBuildVersion.Split('.')[0]
    } else {
      $vsMajorVersion = $vsMinVersion.Major
      $xcopyMSBuildVersion = "$vsMajorVersion.$($vsMinVersion.Minor).0-alpha"
    }

    $vsInstallDir = InstallXCopyMSBuild $xcopyMSBuildVersion
  } else {
    throw "Unable to find Visual Studio that has required version and components installed"
  }

  $msbuildVersionDir = if ([int]$vsMajorVersion -lt 16) { "$vsMajorVersion.0" } else { "Current" }
  return $global:_MSBuildExe = Join-Path $vsInstallDir "MSBuild\$msbuildVersionDir\Bin\msbuild.exe"
}

function InitializeVisualStudioEnvironmentVariables([string] $vsInstallDir, [string] $vsMajorVersion) {
  $env:VSINSTALLDIR = $vsInstallDir
  Set-Item "env:VS$($vsMajorVersion)0COMNTOOLS" (Join-Path $vsInstallDir "Common7\Tools\")
  
  $vsSdkInstallDir = Join-Path $vsInstallDir "VSSDK\"
  if (Test-Path $vsSdkInstallDir) {
    Set-Item "env:VSSDK$($vsMajorVersion)0Install" $vsSdkInstallDir
    $env:VSSDKInstall = $vsSdkInstallDir
  }
}

function InstallXCopyMSBuild([string] $packageVersion) {
  $packageName = "RoslynTools.MSBuild"
  $packageDir = Join-Path $ToolsDir "msbuild\$packageVersion"
  $packagePath = Join-Path $packageDir "$packageName.$packageVersion.nupkg"

  if (!(Test-Path $packageDir)) {
    Create-Directory $packageDir
    Write-Host "Downloading $packageName $packageVersion"
    Invoke-WebRequest "https://dotnet.myget.org/F/roslyn-tools/api/v2/package/$packageName/$packageVersion/" -OutFile $packagePath
    Unzip $packagePath $packageDir
  }

  return Join-Path $packageDir "tools"
}

#
# Locates Visual Studio instance that meets the minimal requirements specified by tools.vs object in global.json.
#
# The following properties of tools.vs are recognized:
#   "version": "{major}.{minor}"    
#       Two part minimal VS version, e.g. "15.9", "16.0", etc.
#   "components": ["componentId1", "componentId2", ...] 
#       Array of ids of workload components that must be available in the VS instance.
#       See e.g. https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-enterprise?view=vs-2017
#
# Returns JSON describing the located VS instance (same format as returned by vswhere), 
# or $null if no instance meeting the requirements is found on the machine.
#
function LocateVisualStudio {
  $vswhereVersion = Get-Member -InputObject $GlobalJson.tools -Name "vswhere"
  if ($vsWhereVersion -eq $null) {
    $vswhereVersion = "2.5.2"
  }

  $vsWhereDir = Join-Path $ToolsDir "vswhere\$vswhereVersion"
  $vsWhereExe = Join-Path $vsWhereDir "vswhere.exe"

  if (!(Test-Path $vsWhereExe)) {
    Create-Directory $vsWhereDir
    Write-Host "Downloading vswhere"
    Invoke-WebRequest "https://github.com/Microsoft/vswhere/releases/download/$vswhereVersion/vswhere.exe" -OutFile $vswhereExe
  }

  $vs = $GlobalJson.tools.vs
  $args = @("-latest", "-prerelease", "-format", "json", "-requires", "Microsoft.Component.MSBuild")
  
  if (Get-Member -InputObject $vs -Name "version") { 
    $args += "-version"
    $args += $vs.version
  }

  if (Get-Member -InputObject $vs -Name "components") { 
    foreach ($component in $vs.components) {
      $args += "-requires"
      $args += $component
    }    
  }

  $vsInfo =& $vsWhereExe $args | ConvertFrom-Json

  if ($lastExitCode -ne 0) {
    return $null
  }

  # use first matching instance
  return $vsInfo[0]
}

function InitializeBuildTool() {
  if (Test-Path global:_BuildTool) {
    return $global:_BuildTool
  }

  if (-not $msbuildEngine) {
    $msbuildEngine = GetDefaultMSBuildEngine
  }

  # Initialize dotnet cli if listed in 'tools'
  $dotnetRoot = $null
  if (Get-Member -InputObject $GlobalJson.tools -Name "dotnet") {
    $dotnetRoot = InitializeDotNetCli -install:$restore
  }

  if ($msbuildEngine -eq "dotnet") {
    if (!$dotnetRoot) {
      Write-Host "/global.json must specify 'tools.dotnet'." -ForegroundColor Red
      ExitWithExitCode 1
    }

    $buildTool = @{ Path = Join-Path $dotnetRoot "dotnet.exe"; Command = "msbuild" }
  } elseif ($msbuildEngine -eq "vs") {
    try {
      $msbuildPath = InitializeVisualStudioMSBuild -install:$restore
    } catch {
      Write-Host $_ -ForegroundColor Red
      ExitWithExitCode 1
    }

    $buildTool = @{ Path = $msbuildPath; Command = "" }
  } else {
    Write-Host "Unexpected value of -msbuildEngine: '$msbuildEngine'." -ForegroundColor Red
    ExitWithExitCode 1
  }

  return $global:_BuildTool = $buildTool
}

function GetDefaultMSBuildEngine() {
  # Presence of tools.vs indicates the repo needs to build using VS msbuild on Windows.
  if (Get-Member -InputObject $GlobalJson.tools -Name "vs") {
    return "vs"
  }
  
  if (Get-Member -InputObject $GlobalJson.tools -Name "dotnet") {
    return "dotnet"
  }

  Write-Host "-msbuildEngine must be specified, or /global.json must specify 'tools.dotnet' or 'tools.vs'." -ForegroundColor Red
  ExitWithExitCode 1
}

function ExitWithExitCode([int] $exitCode) {
  exit $exitCode
}

try {
  $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..") 
  $ArtifactsDir = Join-Path $RepoRoot "Binaries"                      # TODO: update layout
  $ToolsDir = Join-Path $RepoRoot ".tools"
  $LogDir = Join-Path (Join-Path $ArtifactsDir $configuration) "Logs" # TODO: update layout
  $TempDir = Join-Path (Join-Path $ArtifactsDir "tmp") $configuration
  $GlobalJson = Get-Content -Raw -Path (Join-Path $RepoRoot "global.json") | ConvertFrom-Json
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}