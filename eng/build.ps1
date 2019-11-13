#
# This script controls the Roslyn build process. This encompasess everything from build, testing to
# publishing of NuGet packages. The intent is to structure it to allow for a simple flow of logic
# between the following phases:
#
#   - restore
#   - build
#   - sign
#   - pack
#   - test
#   - publish
#
# Each of these phases has a separate command which can be executed independently. For instance
# it's fine to call `build.ps1 -build -testDesktop` followed by repeated calls to
# `.\build.ps1 -testDesktop`.

[CmdletBinding(PositionalBinding=$false)]
param (
  [string][Alias('c')]$configuration = "Debug",
  [string][Alias('v')]$verbosity = "m",
  [string]$msbuildEngine = "vs",

  # Actions
  [switch][Alias('r')]$restore,
  [switch][Alias('b')]$build,
  [switch]$rebuild,
  [switch]$sign,
  [switch]$pack,
  [switch]$publish,
  [switch]$launch,
  [switch]$help,

  # Options
  [switch]$bootstrap,
  [string]$bootstrapConfiguration = "Release",
  [switch][Alias('bl')]$binaryLog,
  [switch]$buildServerLog,
  [switch]$ci,
  [switch]$procdump,
  [switch]$skipAnalyzers,
  [switch][Alias('d')]$deployExtensions,
  [switch]$prepareMachine,
  [switch]$useGlobalNuGetCache = $true,
  [switch]$warnAsError = $false,
  [switch]$sourceBuild = $false,

  # official build settings
  [string]$officialBuildId = "",
  [string]$officialSkipApplyOptimizationData = "",
  [string]$officialSkipTests = "",
  [string]$officialSourceBranchName = "",
  [string]$officialIbcSourceBranchName = "",
  [string]$officialIbcDropId = "",

  # Test actions
  [switch]$test32,
  [switch]$test64,
  [switch]$testVsi,
  [switch][Alias('test')]$testDesktop,
  [switch]$testCoreClr,
  [switch]$testIOperation,

  [parameter(ValueFromRemainingArguments=$true)][string[]]$properties)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
  Write-Host "Common settings:"
  Write-Host "  -configuration <value>    Build configuration: 'Debug' or 'Release' (short: -c)"
  Write-Host "  -verbosity <value>        Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]"
  Write-Host "  -deployExtensions         Deploy built vsixes (short: -d)"
  Write-Host "  -binaryLog                Create MSBuild binary log (short: -bl)"
  Write-Host "  -buildServerLog           Create Roslyn build server log"
  Write-Host ""
  Write-Host "Actions:"
  Write-Host "  -restore                  Restore packages (short: -r)"
  Write-Host "  -build                    Build main solution (short: -b)"
  Write-Host "  -rebuild                  Rebuild main solution"
  Write-Host "  -pack                     Build NuGet packages, VS insertion manifests and installer"
  Write-Host "  -sign                     Sign our binaries"
  Write-Host "  -publish                  Publish build artifacts (e.g. symbols)"
  Write-Host "  -launch                   Launch Visual Studio in developer hive"
  Write-Host "  -help                     Print help and exit"
  Write-Host ""
  Write-Host "Test actions"
  Write-Host "  -test32                   Run unit tests in the 32-bit runner"
  Write-Host "  -test64                   Run units tests in the 64-bit runner"
  Write-Host "  -testDesktop              Run Desktop unit tests (short: -test)"
  Write-Host "  -testCoreClr              Run CoreClr unit tests"
  Write-Host "  -testVsi                  Run all integration tests"
  Write-Host "  -testIOperation           Run extra checks to validate IOperations"
  Write-Host ""
  Write-Host "Advanced settings:"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -bootstrap                Build using a bootstrap compilers"
  Write-Host "  -bootstrapConfiguration   Build configuration for bootstrap compiler: 'Debug' or 'Release'"
  Write-Host "  -msbuildEngine <value>    Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
  Write-Host "  -procdump                 Monitor test runs with procdump"
  Write-Host "  -skipAnalyzers            Do not run analyzers during build operations"
  Write-Host "  -prepareMachine           Prepare machine for CI run, clean up processes after build"
  Write-Host "  -useGlobalNuGetCache      Use global NuGet cache."
  Write-Host "  -warnAsError              Treat all warnings as errors"
  Write-Host "  -sourceBuild              Simulate building source-build"
  Write-Host ""
  Write-Host "Official build settings:"
  Write-Host "  -officialBuildId                            An official build id, e.g. 20190102.3"
  Write-Host "  -officialSkipTests <bool>                   Pass 'true' to not run tests"
  Write-Host "  -officialSkipApplyOptimizationData <bool>   Pass 'true' to not apply optimization data"
  Write-Host "  -officialSourceBranchName <string>          The source branch name"
  Write-Host "  -officialIbcDropId <string>                 IBC data drop to use (e.g. '20190210.1/935479/1')."
  Write-Host "                                              'default' for the most recent available for the branch."
  Write-Host "  -officialIbcSourceBranchName <string>       IBC source branch (e.g. 'master-vs-deps')"
  Write-Host "                                              'default' to select branch based on eng/config/PublishData.json."
  Write-Host ""
  Write-Host "Command line arguments starting with '/p:' are passed through to MSBuild."
}

# Process the command line arguments and establish defaults for the values which are not
# specified.
#
# In this function it's okay to use two arguments to extend the effect of another. For
# example it's okay to look at $testVsi and infer $skipAnalyzers. It's not okay though to infer
# $build based on say $testDesktop. It's possible the developer wanted only for testing
# to execute, not any build.
function Process-Arguments() {
  function OfficialBuildOnly([string]$argName) {
    if ((Get-Variable $argName -Scope Script).Value) {
      if (!$officialBuildId) {
        Write-Host "$argName can only be specified for official builds"
        exit 1
      }
    } else {
      if ($officialBuildId) {
        Write-Host "$argName must be specified in official builds"
        exit 1
      }
    }
  }

  if ($help -or (($properties -ne $null) -and ($properties.Contains("/help") -or $properties.Contains("/?")))) {
       Print-Usage
       exit 0
  }

  OfficialBuildOnly "officialSkipTests"
  OfficialBuildOnly "officialSkipApplyOptimizationData"
  OfficialBuildOnly "officialSourceBranchName"
  OfficialBuildOnly "officialIbcDropId"
  OfficialBuildOnly "officialIbcSourceBranchName"

  if ($officialBuildId) {
    $script:useGlobalNuGetCache = $false
    $script:procdump = $true
    $script:testDesktop = ![System.Boolean]::Parse($officialSkipTests)
    $script:applyOptimizationData = ![System.Boolean]::Parse($officialSkipApplyOptimizationData)
  } else {
    $script:applyOptimizationData = $false
  }

  if ($ci) {
    $script:binaryLog = $true
    if ($bootstrap) {
      $script:buildServerLog = $true
    }
  }

  if ($test32 -and $test64) {
    Write-Host "Cannot combine -test32 and -test64"
    exit 1
  }

  $anyUnit = $testDesktop -or $testCoreClr
  if ($anyUnit -and $testVsi) {
    Write-Host "Cannot combine unit and VSI testing"
    exit 1
  }

  if ($testVsi) {
    # Avoid spending time in analyzers when requested, and also in the slowest integration test builds
    $script:skipAnalyzers = $true
    $script:bootstrap = $false
  }

  if ($build -and $launch -and -not $deployExtensions) {
    Write-Host -ForegroundColor Red "Cannot combine -build and -launch without -deployExtensions"
    exit 1
  }

  $script:test32 = -not $test64

  foreach ($property in $properties) {
    if (!$property.StartsWith("/p:", "InvariantCultureIgnoreCase")) {
      Write-Host "Invalid argument: $property"
      Print-Usage
      exit 1
    }
  }
}

function BuildSolution() {
  # Roslyn.sln can't be built with dotnet due to WPF and VSIX build task dependencies
  $solution = if ($msbuildEngine -eq 'dotnet') { "Compilers.sln" } else { "Roslyn.sln" }

  Write-Host "$($solution):"

  $bl = if ($binaryLog) { "/bl:" + (Join-Path $LogDir "Build.binlog") } else { "" }

  if ($buildServerLog) {
    ${env:ROSLYNCOMMANDLINELOGFILE} = Join-Path $LogDir "Build.Server.log"
  }

  $projects = Join-Path $RepoRoot $solution
  $enableAnalyzers = !$skipAnalyzers
  $toolsetBuildProj = InitializeToolset

  $testTargetFrameworks = if ($testCoreClr) { "netcoreapp3.0%3Bnetcoreapp2.1" } else { "" }
  
  $ibcSourceBranchName = GetIbcSourceBranchName
  $ibcDropId = if ($officialIbcDropId -ne "default") { $officialIbcDropId } else { "" }

  # Do not set this property to true explicitly, since that would override values set in projects.
  $suppressExtensionDeployment = if (!$deployExtensions) { "/p:DeployExtension=false" } else { "" } 

  # The warnAsError flag for MSBuild will promote all warnings to errors. This is true for warnings
  # that MSBuild output as well as ones that custom tasks output.
  #
  # In all cases we pass /p:TreatWarningsAsErrors=true to promote compiler warnings to errors
  $msbuildWarnAsError = if ($warnAsError) { "/warnAsError" } else { "" }

  # Workaround for some machines in the AzDO pool not allowing long paths (%5c is msbuild escaped backslash)
  $ibcDir = Join-Path $RepoRoot ".o%5c"

  # Set DotNetBuildFromSource to 'true' if we're simulating building for source-build.
  $buildFromSource = if ($sourceBuild) { "/p:DotNetBuildFromSource=true" } else { "" }

  try {
    EnableFusionLogging

    MSBuild $toolsetBuildProj `
      $bl `
      /p:Configuration=$configuration `
      /p:Projects=$projects `
      /p:RepoRoot=$RepoRoot `
      /p:Restore=$restore `
      /p:Build=$build `
      /p:Test=$testCoreClr `
      /p:Rebuild=$rebuild `
      /p:Pack=$pack `
      /p:Sign=$sign `
      /p:Publish=$publish `
      /p:ContinuousIntegrationBuild=$ci `
      /p:OfficialBuildId=$officialBuildId `
      /p:UseRoslynAnalyzers=$enableAnalyzers `
      /p:BootstrapBuildPath=$bootstrapDir `
      /p:TestTargetFrameworks=$testTargetFrameworks `
      /p:TreatWarningsAsErrors=true `
      /p:VisualStudioIbcSourceBranchName=$ibcSourceBranchName `
      /p:VisualStudioIbcDropId=$ibcDropId `
      /p:EnableNgenOptimization=$applyOptimizationData `
      /p:IbcOptimizationDataDir=$ibcDir `
      $suppressExtensionDeployment `
      $msbuildWarnAsError `
      $buildFromSource `
      @properties
  }
  finally {
    DisableFusionLogging
    ${env:ROSLYNCOMMANDLINELOGFILE} = $null
  }
}

function EnableFusionLogging() {
  $registryPath = "HKLM:\SOFTWARE\Microsoft\Fusion"
  Set-ItemProperty -Path $registryPath -Name ForceLog         -Value 1       -Type DWord
  Set-ItemProperty -Path $registryPath -Name LogFailures      -Value 1       -Type DWord
  Set-ItemProperty -Path $registryPath -Name LogResourceBinds -Value 1       -Type DWord
  Set-ItemProperty -Path $registryPath -Name LogPath          -Value $LogDir -Type String
}

function DisableFusionLogging() {
  $registryPath = "HKLM:\SOFTWARE\Microsoft\Fusion"
  Remove-ItemProperty -Path $registryPath -Name ForceLog
  Remove-ItemProperty -Path $registryPath -Name LogFailures
  Remove-ItemProperty -Path $registryPath -Name LogResourceBinds
  Remove-ItemProperty -Path $registryPath -Name LogPath
}


# Get the branch that produced the IBC data this build is going to consume.
# IBC data are only merged in official built, but we want to test some of the logic in CI builds as well.
function GetIbcSourceBranchName() {
  if (Test-Path variable:global:_IbcSourceBranchName) {
      return $global:_IbcSourceBranchName
  }

  function calculate {
    $fallback = "master-vs-deps"

    if (!$officialIbcSourceBranchName) {
      return $fallback
    }  

    if ($officialIbcSourceBranchName -ne "default") {
      return $officialIbcSourceBranchName
    }

    $branchData = GetBranchPublishData $officialSourceBranchName
    if ($branchData -eq $null) {
      Write-Host "Warning: Branch $officialSourceBranchName is not listed in PublishData.json. Using IBC data from '$fallback'." -ForegroundColor Yellow
      Write-Host "Override by setting IbcSourceBranchName build variable." -ForegroundColor Yellow
      return $fallback
    }

    if (Get-Member -InputObject $branchData -Name "ibcSourceBranch") {
      return $branchData.ibcSourceBranch 
    }

    return $officialSourceBranchName
  }

  return $global:_IbcSourceBranchName = calculate
}

# Set VSO variables used by MicroBuildBuildVSBootstrapper pipeline task
function SetVisualStudioBootstrapperBuildArgs() {
  $fallbackBranch = "master-vs-deps"

  $branchName = if ($officialSourceBranchName) { $officialSourceBranchName } else { $fallbackBranch }
  $branchData = GetBranchPublishData $branchName

  if ($branchData -eq $null) {
    Write-Host "Warning: Branch $officialSourceBranchName is not listed in PublishData.json. Using VS bootstrapper for branch '$fallbackBranch'. " -ForegroundColor Yellow
    $branchData = GetBranchPublishData $fallbackBranch
  }

  # VS branch name is e.g. "lab/d16.0stg", "rel/d15.9", "lab/ml", etc.
  $vsBranchSimpleName = $branchData.vsBranch.Split('/')[-1]
  $vsMajorVersion = $branchData.vsMajorVersion
  $vsChannel = "int.$vsBranchSimpleName"

  Write-Host "##vso[task.setvariable variable=VisualStudio.MajorVersion;]$vsMajorVersion"        
  Write-Host "##vso[task.setvariable variable=VisualStudio.ChannelName;]$vsChannel"

  $insertionDir = Join-Path $VSSetupDir "Insertion"
  $manifestList = [string]::Join(',', (Get-ChildItem "$insertionDir\*.vsman"))
  Write-Host "##vso[task.setvariable variable=VisualStudio.SetupManifestList;]$manifestList"
}

# Core function for running our unit / integration tests tests
function TestUsingOptimizedRunner() {

  # Tests need to locate .NET Core SDK
  $dotnet = InitializeDotNetCli

  if ($testVsi) {
    Deploy-VsixViaTool

    if ($ci) {
      # Minimize all windows to avoid interference during integration test runs
      $shell = New-Object -ComObject "Shell.Application"
      $shell.MinimizeAll()
    }
  }

  if ($testIOperation) {
    $env:ROSLYN_TEST_IOPERATION = "true"
  }

  $secondaryLogDir = Join-Path (Join-Path $ArtifactsDir "log2") $configuration
  Create-Directory $secondaryLogDir
  $testResultsDir = Join-Path $ArtifactsDir "TestResults\$configuration"
  $binDir = Join-Path $ArtifactsDir "bin" 
  $runTests = GetProjectOutputBinary "RunTests.exe"

  if (!(Test-Path $runTests)) {
    Write-Host "Test runner not found: '$runTests'. Run Build.cmd first." -ForegroundColor Red 
    ExitWithExitCode 1
  }

  $xunitDir = Join-Path (Get-PackageDir "xunit.runner.console") "tools\net472"
  $args = "`"$xunitDir`""
  $args += " `"-out:$testResultsDir`""
  $args += " `"-logs:$LogDir`""
  $args += " `"-secondaryLogs:$secondaryLogDir`""
  $args += " -nocache"
  $args += " -tfm:net472"

  if ($testDesktop -or $testIOperation) {
    if ($test32) {
      $dlls = Get-ChildItem -Recurse -Include "*.UnitTests.dll" $binDir
    } else {
      $dlls = Get-ChildItem -Recurse -Include "*.UnitTests.dll" -Exclude "*InteractiveHost*" $binDir
    }
  } elseif ($testVsi) {
    # Since they require Visual Studio to be installed, ensure that the MSBuildWorkspace tests run along with our VS
    # integration tests in CI.
    if ($ci) {
      $dlls += @(Get-Item (GetProjectOutputBinary "Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll"))
    }

    $dlls += @(Get-ChildItem -Recurse -Include "*.IntegrationTests.dll" $binDir)
    $args += " -testVsi"
  } else {
    $dlls = Get-ChildItem -Recurse -Include "*.IntegrationTests.dll" $binDir
    $args += " -trait:Feature=NetCore"
  }

  # Exclude out the multi-targetted netcore app projects
  $dlls = $dlls | ?{ -not ($_.FullName -match ".*netcoreapp.*") }

  # Exclude out the ref assemblies
  $dlls = $dlls | ?{ -not ($_.FullName -match ".*\\ref\\.*") }
  $dlls = $dlls | ?{ -not ($_.FullName -match ".*/ref/.*") }

  if ($ci) {
    $args += " -xml"
    if ($testVsi) {
      $args += " -timeout:110"
    } else {
      $args += " -timeout:65"
    }
  }

  $procdumpPath = Ensure-ProcDump
  $args += " -procdumppath:$procDumpPath"
  if ($procdump) {
    $args += " -useprocdump";
  }

  if ($test64) {
    $args += " -test64"
  }

  foreach ($dll in $dlls) {
    $args += " $dll"
  }

  try {
    Exec-Console $runTests $args
  } finally {
    Get-Process "xunit*" -ErrorAction SilentlyContinue | Stop-Process
    if ($testIOperation) {
      Remove-Item env:\ROSLYN_TEST_IOPERATION
    }
  }
}

function EnablePreviewSdks() {
  $vsInfo = LocateVisualStudio
  if ($vsInfo -eq $null) {
    # Preview SDKs are allowed when no Visual Studio instance is installed
    return
  }

  $vsId = $vsInfo.instanceId
  $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

  $instanceDir = Join-Path ${env:USERPROFILE} "AppData\Local\Microsoft\VisualStudio\$vsMajorVersion.0_$vsId"
  Create-Directory $instanceDir
  $sdkFile = Join-Path $instanceDir "sdk.txt"
  'UsePreviews=True' | Set-Content $sdkFile
}

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to
# deploying at build time.
function Deploy-VsixViaTool() { 
  $vsixDir = Get-PackageDir "RoslynTools.VSIXExpInstaller"
  $vsixExe = Join-Path $vsixDir "tools\VsixExpInstaller.exe"
  
  $vsInfo = LocateVisualStudio
  if ($vsInfo -eq $null) {
    throw "Unable to locate required Visual Studio installation"
  }

  $vsDir = $vsInfo.installationPath.TrimEnd("\")
  $vsId = $vsInfo.instanceId
  $vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]

  $hive = "RoslynDev"
  Write-Host "Using VS Instance $vsId at `"$vsDir`""
  $baseArgs = "/rootSuffix:$hive /vsInstallDir:`"$vsDir`""

  Write-Host "Uninstalling old Roslyn VSIX"

  # Actual uninstall is failing at the moment using the uninstall options. Temporarily using
  # wildfire to uninstall our VSIX extensions
  $extDir = Join-Path ${env:USERPROFILE} "AppData\Local\Microsoft\VisualStudio\$vsMajorVersion.0_$vsid$hive"
  if (Test-Path $extDir) {
    foreach ($dir in Get-ChildItem -Directory $extDir) {
      $name = Split-Path -leaf $dir
      Write-Host "`tUninstalling $name"
    }
    Remove-Item -re -fo $extDir
  }

  Write-Host "Installing all Roslyn VSIX"

  # VSIX files need to be installed in this specific order:
  $orderedVsixFileNames = @(
    "Roslyn.Compilers.Extension.vsix",
    "Roslyn.VisualStudio.Setup.vsix",
    "Roslyn.VisualStudio.Setup.Dependencies.vsix",
    "Roslyn.VisualStudio.InteractiveComponents.vsix",
    "ExpressionEvaluatorPackage.vsix",
    "Roslyn.VisualStudio.DiagnosticsWindow.vsix",
    "Microsoft.VisualStudio.IntegrationTest.Setup.vsix")

  foreach ($vsixFileName in $orderedVsixFileNames) {
    $vsixFile = Join-Path $VSSetupDir $vsixFileName
    $fullArg = "$baseArgs $vsixFile"
    Write-Host "`tInstalling $vsixFileName"
    Exec-Console $vsixExe $fullArg
  }
}

# Ensure that procdump is available on the machine.  Returns the path to the directory that contains
# the procdump binaries (both 32 and 64 bit)
function Ensure-ProcDump() {

  # Jenkins images default to having procdump installed in the root.  Use that if available to avoid
  # an unnecessary download.
  if (Test-Path "C:\SysInternals\procdump.exe") {
    return "C:\SysInternals"
  }

  $outDir = Join-Path $ToolsDir "ProcDump"
  $filePath = Join-Path $outDir "procdump.exe"
  if (-not (Test-Path $filePath)) {
    Remove-Item -Re $filePath -ErrorAction SilentlyContinue
    Create-Directory $outDir
    $zipFilePath = Join-Path $toolsDir "procdump.zip"
    Invoke-WebRequest "https://download.sysinternals.com/files/Procdump.zip" -UseBasicParsing -outfile $zipFilePath | Out-Null
    Unzip $zipFilePath $outDir
  }

  return $outDir
}

# Setup the CI machine for running our integration tests.
function Setup-IntegrationTestRun() {
  $processesToStopOnExit += "devenv"
  $screenshotPath = (Join-Path $LogDir "StartingBuild.png")
  try {
    Capture-Screenshot $screenshotPath
  }
  catch {
    Write-Host "Screenshot failed; attempting to connect to the console"

    # Keep the session open so we have a UI to interact with
    $quserItems = ((quser $env:USERNAME | select -Skip 1) -split '\s+')
    $sessionid = $quserItems[2]
    if ($sessionid -eq 'Disc') {
      # When the session isn't connected, the third value is 'Disc' instead of the ID
      $sessionid = $quserItems[1]
    }

    if ($quserItems[1] -eq 'console') {
      Write-Host "Disconnecting from console before attempting reconnection"
      try {
        tsdiscon
      } catch {
        # ignore
      }

      # Disconnection is asynchronous, so wait a few seconds for it to complete
      Start-Sleep -Seconds 3
      query user
    }

    Write-Host "tscon $sessionid /dest:console"
    tscon $sessionid /dest:console

    # Connection is asynchronous, so wait a few seconds for it to complete
    Start-Sleep 3
    query user

    # Make sure we can capture a screenshot. An exception at this point will fail-fast the build.
    Capture-Screenshot $screenshotPath
  }
}

function Prepare-TempDir() {
  Copy-Item (Join-Path $RepoRoot "src\Workspaces\MSBuildTest\Resources\.editorconfig") $TempDir
  Copy-Item (Join-Path $RepoRoot "src\Workspaces\MSBuildTest\Resources\Directory.Build.props") $TempDir
  Copy-Item (Join-Path $RepoRoot "src\Workspaces\MSBuildTest\Resources\Directory.Build.targets") $TempDir
  Copy-Item (Join-Path $RepoRoot "src\Workspaces\MSBuildTest\Resources\Directory.Build.rsp") $TempDir
  Copy-Item (Join-Path $RepoRoot "src\Workspaces\MSBuildTest\Resources\NuGet.Config") $TempDir
}

function List-Processes() {
  Write-Host "Listing running build processes..."
  Get-Process -Name "msbuild" -ErrorAction SilentlyContinue | Out-Host
  Get-Process -Name "vbcscompiler" -ErrorAction SilentlyContinue | Out-Host
  Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | where { $_.Modules | select { $_.ModuleName -eq "VBCSCompiler.dll" } } | Out-Host
  Get-Process -Name "devenv" -ErrorAction SilentlyContinue | Out-Host
}

try {
  if ($PSVersionTable.PSVersion.Major -lt "5") {
    Write-Host "PowerShell version must be 5 or greater (version $($PSVersionTable.PSVersion) detected)"
    exit 1
  }

  $regKeyProperty = Get-ItemProperty -Path HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem -Name "LongPathsEnabled" -ErrorAction Ignore
  if (($null -eq $regKeyProperty) -or ($regKeyProperty.LongPathsEnabled -ne 1)) {
    Write-Host "LongPath is not enabled, you may experience build errors. You can avoid these by enabling LongPath with `"reg ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem /v LongPathsEnabled /t REG_DWORD /d 1`""
  }

  Process-Arguments

  . (Join-Path $PSScriptRoot "build-utils.ps1")

  Push-Location $RepoRoot

  if ($ci) {
    List-Processes
    Prepare-TempDir
    EnablePreviewSdks
    if ($testVsi) {
      Setup-IntegrationTestRun 
    }

    $global:_DotNetInstallDir = Join-Path $RepoRoot ".dotnet"
    InstallDotNetSdk $global:_DotNetInstallDir $GlobalJson.tools.dotnet

    # Make sure a 2.1 runtime is installed so we can run our tests. Most of them still 
    # target netcoreapp2.1.
    InstallDotNetSdk $global:_DotNetInstallDir "2.1.503"
  }

  try
  {
    if ($bootstrap) {
      $bootstrapDir = Make-BootstrapBuild -force32:$test32
    }
  }
  catch
  {
    echo "##vso[task.logissue type=error](NETCORE_ENGINEERING_TELEMETRY=Build) Build failed"
    throw $_
  }

  if ($restore -or $build -or $rebuild -or $pack -or $sign -or $publish -or $testCoreClr) {
    BuildSolution
  }

  if ($ci -and $build -and $msbuildEngine -eq "vs") {
    SetVisualStudioBootstrapperBuildArgs
  }

  try
  {
    if ($testDesktop -or $testVsi -or $testIOperation) {
      TestUsingOptimizedRunner
    }
  }
  catch
  {
    echo "##vso[task.logissue type=error](NETCORE_ENGINEERING_TELEMETRY=Test) Tests failed"
    throw $_
  }

  if ($launch) {
    $devenvExe = Join-Path $env:VSINSTALLDIR 'Common7\IDE\devenv.exe'
    &$devenvExe /rootSuffix RoslynDev
  }

  ExitWithExitCode 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  if ($ci) {
    Stop-Processes
  }
  Pop-Location
}
