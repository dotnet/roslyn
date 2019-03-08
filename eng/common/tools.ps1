# Initialize variables if they aren't already defined.
# These may be defined as parameters of the importing script, or set after importing this script.

# CI mode - set to true on CI server for PR validation build or official build.
[bool]$ci = if (Test-Path variable:ci) { $ci } else { $false }

# Build configuration. Common values include 'Debug' and 'Release', but the repository may use other names.
[string]$configuration = if (Test-Path variable:configuration) { $configuration } else { "Debug" }

# Set to true to output binary log from msbuild. Note that emitting binary log slows down the build.
# Binary log must be enabled on CI.
[bool]$binaryLog = if (Test-Path variable:binaryLog) { $binaryLog } else { $ci }

# Turns on machine preparation/clean up code that changes the machine state (e.g. kills build processes).
[bool]$prepareMachine = if (Test-Path variable:prepareMachine) { $prepareMachine } else { $false }

# True to restore toolsets and dependencies.
[bool]$restore = if (Test-Path variable:restore) { $restore } else { $true }

# Adjusts msbuild verbosity level.
[string]$verbosity = if (Test-Path variable:verbosity) { $verbosity } else { "minimal" }

# Set to true to reuse msbuild nodes. Recommended to not reuse on CI.
[bool]$nodeReuse = if (Test-Path variable:nodeReuse) { $nodeReuse } else { !$ci }

# Configures warning treatment in msbuild.
[bool]$warnAsError = if (Test-Path variable:warnAsError) { $warnAsError } else { $true }

# Specifies which msbuild engine to use for build: 'vs', 'dotnet' or unspecified (determined based on presence of tools.vs in global.json).
[string]$msbuildEngine = if (Test-Path variable:msbuildEngine) { $msbuildEngine } else { $null }

# True to attempt using .NET Core already that meets requirements specified in global.json 
# installed on the machine instead of downloading one.
[bool]$useInstalledDotNetCli = if (Test-Path variable:useInstalledDotNetCli) { $useInstalledDotNetCli } else { $true }

# True to use global NuGet cache instead of restoring packages to repository-local directory.
[bool]$useGlobalNuGetCache = if (Test-Path variable:useGlobalNuGetCache) { $useGlobalNuGetCache } else { !$ci }

# An array of names of processes to stop on script exit if prepareMachine is true.
$processesToStopOnExit = if (Test-Path variable:processesToStopOnExit) { $processesToStopOnExit } else { @("msbuild", "dotnet", "vbcscompiler") }

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

# This will exec a process using the console and return it's exit code.
# This will not throw when the process fails.
# Returns process exit code.
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
    return $global:LASTEXITCODE = $process.ExitCode
  }
  finally {
    # If we didn't finish then an error occured or the user hit ctrl-c.  Either
    # way kill the process
    if (-not $finished) {
      $process.Kill()
    }
  }
}

function InitializeDotNetCli([bool]$install) {
  if (Test-Path variable:global:_DotNetInstallDir) {
    return $global:_DotNetInstallDir
  }

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  $env:DOTNET_MULTILEVEL_LOOKUP=0

  # Disable first run since we do not need all ASP.NET packages restored.
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Disable telemetry on CI.
  if ($ci) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT=1
  }

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

  # Add dotnet to PATH. This prevents any bare invocation of dotnet in custom
  # build steps from using anything other than what we've downloaded.
  # It also ensures that VS msbuild will use the downloaded sdk targets.
  $env:PATH = "$dotnetRoot;$env:PATH"

  if ($ci) {
    # Make Sure that our bootstrapped dotnet cli is avaliable in future steps of the Azure Pipelines build
    Write-Host "##vso[task.prependpath]$dotnetRoot"
    Write-Host "##vso[task.setvariable variable=DOTNET_MULTILEVEL_LOOKUP]0"
    Write-Host "##vso[task.setvariable variable=DOTNET_SKIP_FIRST_TIME_EXPERIENCE]1"
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

function InstallDotNetSdk([string] $dotnetRoot, [string] $version, [string] $architecture = "") {
  $installScript = GetDotNetInstallScript $dotnetRoot
  $archArg = if ($architecture) { $architecture } else { "<auto>" }
  & $installScript -Version $version -InstallDir $dotnetRoot -Architecture $archArg
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
function InitializeVisualStudioMSBuild([bool]$install, [object]$vsRequirements = $null) {
  if (Test-Path variable:global:_MSBuildExe) {
    return $global:_MSBuildExe
  }

  if (!$vsRequirements) { $vsRequirements = $GlobalJson.tools.vs }
  $vsMinVersionStr = if ($vsRequirements.version) { $vsRequirements.version } else { "15.9" }
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
  $vsInfo = LocateVisualStudio $vsRequirements
  if ($vsInfo -ne $null) {
    $vsInstallDir = $vsInfo.installationPath
    $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

    InitializeVisualStudioEnvironmentVariables $vsInstallDir $vsMajorVersion
  } else {

    if (Get-Member -InputObject $GlobalJson.tools -Name "xcopy-msbuild") {
      $xcopyMSBuildVersion = $GlobalJson.tools.'xcopy-msbuild'
      $vsMajorVersion = $xcopyMSBuildVersion.Split('.')[0]
    } else {
      $vsMajorVersion = $vsMinVersion.Major
      $xcopyMSBuildVersion = "$vsMajorVersion.$($vsMinVersion.Minor).0-alpha"
    }

    $vsInstallDir = InitializeXCopyMSBuild $xcopyMSBuildVersion $install
    if ($vsInstallDir -eq $null) {
      throw "Unable to find Visual Studio that has required version and components installed"
    }
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

function InstallXCopyMSBuild([string]$packageVersion) {
  return InitializeXCopyMSBuild $packageVersion -install $true
}

function InitializeXCopyMSBuild([string]$packageVersion, [bool]$install) {
  $packageName = "RoslynTools.MSBuild"
  $packageDir = Join-Path $ToolsDir "msbuild\$packageVersion"
  $packagePath = Join-Path $packageDir "$packageName.$packageVersion.nupkg"

  if (!(Test-Path $packageDir)) {
    if (!$install) {
      return $null
    }

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
function LocateVisualStudio([object]$vsRequirements = $null){
  if (Get-Member -InputObject $GlobalJson.tools -Name "vswhere") {
    $vswhereVersion = $GlobalJson.tools.vswhere
  } else {
    $vswhereVersion = "2.5.2"
  }

  $vsWhereDir = Join-Path $ToolsDir "vswhere\$vswhereVersion"
  $vsWhereExe = Join-Path $vsWhereDir "vswhere.exe"

  if (!(Test-Path $vsWhereExe)) {
    Create-Directory $vsWhereDir
    Write-Host "Downloading vswhere"
    Invoke-WebRequest "https://github.com/Microsoft/vswhere/releases/download/$vswhereVersion/vswhere.exe" -OutFile $vswhereExe
  }

  if (!$vsRequirements) { $vsRequirements = $GlobalJson.tools.vs }
  $args = @("-latest", "-prerelease", "-format", "json", "-requires", "Microsoft.Component.MSBuild")
  
  if (Get-Member -InputObject $vsRequirements -Name "version") {
    $args += "-version"
    $args += $vsRequirements.version
  }

  if (Get-Member -InputObject $vsRequirements -Name "components") {
    foreach ($component in $vsRequirements.components) {
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
  if (Test-Path variable:global:_BuildTool) {
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

function GetNuGetPackageCachePath() {
  if ($env:NUGET_PACKAGES -eq $null) {
    # Use local cache on CI to ensure deterministic build,
    # use global cache in dev builds to avoid cost of downloading packages.
    if ($useGlobalNuGetCache) {
      $env:NUGET_PACKAGES = Join-Path $env:UserProfile ".nuget\packages"
    } else {
      $env:NUGET_PACKAGES = Join-Path $RepoRoot ".packages"
    }
  }

  return $env:NUGET_PACKAGES
}

# Returns a full path to an Arcade SDK task project file.
function GetSdkTaskProject([string]$taskName) {
  return Join-Path (Split-Path (InitializeToolset) -Parent) "SdkTasks\$taskName.proj"
}

function InitializeToolset() {
  if (Test-Path variable:global:_ToolsetBuildProj) {
    return $global:_ToolsetBuildProj
  }

  $nugetCache = GetNuGetPackageCachePath

  $toolsetVersion = $GlobalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Sdk'
  $toolsetLocationFile = Join-Path $ToolsetDir "$toolsetVersion.txt"

  if (Test-Path $toolsetLocationFile) {
    $path = Get-Content $toolsetLocationFile -TotalCount 1
    if (Test-Path $path) {
      return $global:_ToolsetBuildProj = $path
    }
  }

  if (-not $restore) {
    Write-Host "Toolset version $toolsetVersion has not been restored." -ForegroundColor Red
    ExitWithExitCode 1
  }

  $buildTool = InitializeBuildTool

  $proj = Join-Path $ToolsetDir "restore.proj"
  $bl = if ($binaryLog) { "/bl:" + (Join-Path $LogDir "ToolsetRestore.binlog") } else { "" }

  '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' | Set-Content $proj
  MSBuild $proj $bl /t:__WriteToolsetLocation /clp:ErrorsOnly`;NoSummary /p:__ToolsetLocationOutputFile=$toolsetLocationFile

  $path = Get-Content $toolsetLocationFile -TotalCount 1
  if (!(Test-Path $path)) {
    throw "Invalid toolset path: $path"
  }

  return $global:_ToolsetBuildProj = $path
}

function ExitWithExitCode([int] $exitCode) {
  if ($ci -and $prepareMachine) {
    Stop-Processes
  }
  exit $exitCode
}

function Stop-Processes() {
  Write-Host "Killing running build processes..."
  foreach ($processName in $processesToStopOnExit) {
    Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process
  }
}

#
# Executes msbuild (or 'dotnet msbuild') with arguments passed to the function.
# The arguments are automatically quoted.
# Terminates the script if the build fails.
#
function MSBuild() {
  if ($ci) {
    if (!$binaryLog) {
      throw "Binary log must be enabled in CI build."
    }

    if ($nodeReuse) {
      throw "Node reuse must be disabled in CI build."
    }
  }

  $buildTool = InitializeBuildTool

  $cmdArgs = "$($buildTool.Command) /m /nologo /clp:Summary /v:$verbosity /nr:$nodeReuse /p:ContinuousIntegrationBuild=$ci"

  if ($warnAsError) { 
    $cmdArgs += " /warnaserror /p:TreatWarningsAsErrors=true" 
  }

  foreach ($arg in $args) {
    if ($arg -ne $null -and $arg.Trim() -ne "") {
      $cmdArgs += " `"$arg`""
    }
  }
  
  $exitCode = Exec-Process $buildTool.Path $cmdArgs

  if ($exitCode -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red

    $buildLog = GetMSBuildBinaryLogCommandLineArgument $args
    if ($buildLog -ne $null) {      
      Write-Host "See log: $buildLog" -ForegroundColor DarkGray 
    }

    ExitWithExitCode $exitCode
  }
}

function GetMSBuildBinaryLogCommandLineArgument($arguments) {  
  foreach ($argument in $arguments) {
    if ($argument -ne $null) {
      $arg = $argument.Trim()
      if ($arg.StartsWith("/bl:", "OrdinalIgnoreCase")) {
        return $arg.Substring("/bl:".Length)
      } 
        
      if ($arg.StartsWith("/binaryLogger:", "OrdinalIgnoreCase")) {
        return $arg.Substring("/binaryLogger:".Length)
      }
    }
  }

  return $null
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$EngRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$ToolsetDir = Join-Path $ArtifactsDir "toolset"
$ToolsDir = Join-Path $RepoRoot ".tools"
$LogDir = Join-Path (Join-Path $ArtifactsDir "log") $configuration
$TempDir = Join-Path (Join-Path $ArtifactsDir "tmp") $configuration
$GlobalJson = Get-Content -Raw -Path (Join-Path $RepoRoot "global.json") | ConvertFrom-Json

Create-Directory $ToolsetDir
Create-Directory $TempDir
Create-Directory $LogDir

if ($ci) {
  $env:TEMP = $TempDir
  $env:TMP = $TempDir
}
