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
  [string]$bootstrapToolset = "",
  [switch][Alias('bl')]$binaryLog,
  [string]$binaryLogName = "",
  [switch]$ci,
  [switch]$collectDumps,
  [switch][Alias('a')]$runAnalyzers,
  [switch]$skipDocumentation = $false,
  [switch][Alias('d')]$deployExtensions,
  [switch]$prepareMachine,
  [switch]$useGlobalNuGetCache = $true,
  [switch]$warnAsError = $false,
  [switch]$sourceBuild = $false,
  [switch]$oop64bit = $true,
  [switch]$oopCoreClr = $false,
  [switch]$lspEditor = $false,

  # official build settings
  [string]$officialBuildId = "",
  [string]$officialSkipApplyOptimizationData = "",
  [string]$officialSkipTests = "",
  [string]$officialSourceBranchName = "",
  [string]$officialIbcDrop = "",
  [string]$officialVisualStudioDropAccessToken = "",

  # Test actions
  [string]$testArch = "x64",
  [switch]$testVsi,
  [switch][Alias('test')]$testDesktop,
  [switch]$testCoreClr,
  [switch]$testCompilerOnly = $false,
  [switch]$testIOperation,
  [switch]$testUsedAssemblies,
  [switch]$sequential,
  [switch]$helix,
  [string]$helixQueueName = "",

  [parameter(ValueFromRemainingArguments=$true)][string[]]$properties)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
  Write-Host "Common settings:"
  Write-Host "  -configuration <value>    Build configuration: 'Debug' or 'Release' (short: -c)"
  Write-Host "  -verbosity <value>        Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]"
  Write-Host "  -deployExtensions         Deploy built vsixes (short: -d)"
  Write-Host "  -binaryLog                Create MSBuild binary log (short: -bl)"
  Write-Host "  -binaryLogName            Name of the binary log (default Build.binlog)"
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
  Write-Host "  -testArch                 Maps to --arch parameter of dotnet test"
  Write-Host "  -testDesktop              Run Desktop unit tests (short: -test)"
  Write-Host "  -testCoreClr              Run CoreClr unit tests"
  Write-Host "  -testCompilerOnly         Run only the compiler unit tests"
  Write-Host "  -testVsi                  Run all integration tests"
  Write-Host "  -testIOperation           Run extra checks to validate IOperations"
  Write-Host "  -testUsedAssemblies       Run extra checks to validate used assemblies feature (see ROSLYN_TEST_USEDASSEMBLIES in codebase)"
  Write-Host ""
  Write-Host "Advanced settings:"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -bootstrap                Build using a bootstrap compilers"
  Write-Host "  -bootstrapConfiguration   Build configuration for bootstrap compiler: 'Debug' or 'Release'"
  Write-Host "  -msbuildEngine <value>    Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
  Write-Host "  -collectDumps             Collect dumps from test runs"
  Write-Host "  -runAnalyzers             Run analyzers during build operations (short: -a)"
  Write-Host "  -skipDocumentation        Skip generation of XML documentation files"
  Write-Host "  -prepareMachine           Prepare machine for CI run, clean up processes after build"
  Write-Host "  -useGlobalNuGetCache      Use global NuGet cache."
  Write-Host "  -warnAsError              Treat all warnings as errors"
  Write-Host "  -sourceBuild              Simulate building source-build"
  Write-Host ""
  Write-Host "Official build settings:"
  Write-Host "  -officialBuildId                                  An official build id, e.g. 20190102.3"
  Write-Host "  -officialSkipTests <bool>                         Pass 'true' to not run tests"
  Write-Host "  -officialSkipApplyOptimizationData <bool>         Pass 'true' to not apply optimization data"
  Write-Host "  -officialSourceBranchName <string>                The source branch name"
  Write-Host "  -officialIbcDrop <string>                         IBC data drop to use (e.g. 'ProfilingOutputs/DevDiv/VS/..')."
  Write-Host "                                                    'default' for the most recent available for the branch."
  Write-Host "  -officialVisualStudioDropAccessToken <string>     The access token to access OptProf data drop"
  Write-Host ""
  Write-Host "Command line arguments starting with '/p:' are passed through to MSBuild."
}

# Process the command line arguments and establish defaults for the values which are not
# specified.
#
# In this function it's okay to use two arguments to extend the effect of another. For
# example it's okay to look at $testVsi and infer $runAnalyzers. It's not okay though to infer
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
  OfficialBuildOnly "officialVisualStudioDropAccessToken"

  if ($officialBuildId) {
    $script:useGlobalNuGetCache = $false
    $script:collectDumps = $true
    $script:testDesktop = ![System.Boolean]::Parse($officialSkipTests)
    $script:applyOptimizationData = ![System.Boolean]::Parse($officialSkipApplyOptimizationData)
  } else {
    $script:applyOptimizationData = $false
  }

  if ($binaryLogName -ne "") {
    $script:binaryLog = $true
  }

  if ($ci) {
    $script:binaryLog = $true
  }

  if ($binaryLog -and ($binaryLogName -eq "")) {
    $script:binaryLogName = "Build.binlog"
  }

  $anyUnit = $testDesktop -or $testCoreClr
  if ($anyUnit -and $testVsi) {
    Write-Host "Cannot combine unit and VSI testing"
    exit 1
  }

  if ($testVsi -and $helix) {
    Write-Host "Cannot run integration tests on Helix"
    exit 1
  }

  if ($testVsi) {
    # Avoid spending time in analyzers when requested, and also in the slowest integration test builds
    $script:runAnalyzers = $false
    $script:bootstrap = $false
  }

  if ($build -and $launch -and -not $deployExtensions) {
    Write-Host -ForegroundColor Red "Cannot combine -build and -launch without -deployExtensions"
    exit 1
  }

  if ($bootstrap) {
    $script:restore = $true
  }

  if ($sourceBuild) {
    $script:msbuildEngine = "dotnet"
  }

  foreach ($property in $properties) {
    if (!$property.StartsWith("/p:", "InvariantCultureIgnoreCase")) {
      Write-Host "Invalid argument: $property"
      Print-Usage
      exit 1
    }
  }
}

function BuildSolution() {
  $solution = "Roslyn.sln"

  Write-Host "$($solution):"

  $bl = ""
  if ($binaryLog) {
    $binaryLogPath = Join-Path $LogDir $binaryLogName
    $bl = "/bl:" + $binaryLogPath
    if ($ci -and (Test-Path $binaryLogPath)) {
      Write-LogIssue -Type "error" -Message "Overwriting binary log file $($binaryLogPath)"
      throw "Overwriting binary log files"
    }

  }

  $projects = Join-Path $RepoRoot $solution
  $toolsetBuildProj = InitializeToolset

  $ibcDropName = GetIbcDropName

  # Do not set this property to true explicitly, since that would override values set in projects.
  $suppressExtensionDeployment = if (!$deployExtensions) { "/p:DeployExtension=false" } else { "" }

  # The warnAsError flag for MSBuild will promote all warnings to errors. This is true for warnings
  # that MSBuild output as well as ones that custom tasks output.
  $msbuildWarnAsError = if ($warnAsError) { "/warnAsError" } else { "" }

  # Workaround for some machines in the AzDO pool not allowing long paths (%5c is msbuild escaped backslash)
  $ibcDir = Join-Path $RepoRoot ".o%5c"

  # Set DotNetBuildFromSource to 'true' if we're simulating building for source-build.
  $buildFromSource = if ($sourceBuild) { "/p:DotNetBuildFromSource=true" } else { "" }

  $generateDocumentationFile = if ($skipDocumentation) { "/p:GenerateDocumentationFile=false" } else { "" }
  $roslynUseHardLinks = if ($ci) { "/p:ROSLYNUSEHARDLINKS=true" } else { "" }

 # Temporarily disable RestoreUseStaticGraphEvaluation to work around this NuGet issue 
  # in our CI builds
  # https://github.com/NuGet/Home/issues/12373
  $restoreUseStaticGraphEvaluation = if ($ci) { $false } else { $true }
  
  try {
    MSBuild $toolsetBuildProj `
      $bl `
      /p:Configuration=$configuration `
      /p:Projects=$projects `
      /p:RepoRoot=$RepoRoot `
      /p:Restore=$restore `
      /p:Build=$build `
      /p:Rebuild=$rebuild `
      /p:Pack=$pack `
      /p:Sign=$sign `
      /p:Publish=$publish `
      /p:ContinuousIntegrationBuild=$ci `
      /p:OfficialBuildId=$officialBuildId `
      /p:RunAnalyzersDuringBuild=$runAnalyzers `
      /p:BootstrapBuildPath=$bootstrapDir `
      /p:TreatWarningsAsErrors=$warnAsError `
      /p:EnableNgenOptimization=$applyOptimizationData `
      /p:IbcOptimizationDataDir=$ibcDir `
      /p:RestoreUseStaticGraphEvaluation=$restoreUseStaticGraphEvaluation `
      /p:VisualStudioIbcDrop=$ibcDropName `
      /p:VisualStudioDropAccessToken=$officialVisualStudioDropAccessToken `
      $suppressExtensionDeployment `
      $msbuildWarnAsError `
      $buildFromSource `
      $generateDocumentationFile `
      $roslynUseHardLinks `
      @properties
  }
  finally {
    ${env:ROSLYNCOMMANDLINELOGFILE} = $null
  }
}

# Get the branch that produced the IBC data this build is going to consume.
# IBC data are only merged in official built, but we want to test some of the logic in CI builds as well.
function GetIbcSourceBranchName() {
  if (Test-Path variable:global:_IbcSourceBranchName) {
      return $global:_IbcSourceBranchName
  }

  function calculate {
    $fallback = "main"

    $branchData = GetBranchPublishData $officialSourceBranchName
    if ($branchData -eq $null) {
      Write-LogIssue -Type "warning" -Message "Branch $officialSourceBranchName is not listed in PublishData.json. Using IBC data from '$fallback'."
      Write-Host "Override by setting IbcDrop build variable." -ForegroundColor Yellow
      return $fallback
    }

    return $branchData.vsBranch
  }

  return $global:_IbcSourceBranchName = calculate
}

function GetIbcDropName() {

    if ($officialIbcDrop -and $officialIbcDrop -ne "default"){
        return $officialIbcDrop
    }

    # Don't try and get the ibc drop if we're not in an official build as it won't be used anyway
    if (!$applyOptimizationData -or !$officialBuildId) {
        return ""
    }

    # Bring in the ibc tools
    $packagePath = Join-Path (Get-PackageDir "Microsoft.DevDiv.Optimization.Data.PowerShell") "lib\net472"
    Import-Module (Join-Path $packagePath "Optimization.Data.PowerShell.dll")

    # Find the matching drop
    $branch = GetIbcSourceBranchName
    Write-Host "Optimization data branch name is '$branch'."

    $pat = ConvertTo-SecureString $officialVisualStudioDropAccessToken -AsPlainText -Force
    $drop = Find-OptimizationInputsStoreForBranch -ProjectName "DevDiv" -RepositoryName "VS" -BranchName $branch -PAT $pat
    return $drop.Name
}

function GetCompilerTestAssembliesIncludePaths() {
  $assemblies = " --include '^Microsoft\.CodeAnalysis\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CompilerServer\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.Syntax\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.Symbol\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.Semantic\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.Emit\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.Emit2\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.IOperation\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.CSharp\.CommandLine\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Syntax\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Symbol\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Semantic\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.VisualBasic\.Emit\.UnitTests$'"
  $assemblies += " --include '^Roslyn\.Compilers\.VisualBasic\.IOperation\.UnitTests$'"
  $assemblies += " --include '^Microsoft\.CodeAnalysis\.VisualBasic\.CommandLine\.UnitTests$'"
  return $assemblies
}

# Core function for running our unit / integration tests tests
function TestUsingRunTests() {

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

  if ($ci) {
    $env:ROSLYN_TEST_CI = "true"
  }

  if ($testIOperation) {
    $env:ROSLYN_TEST_IOPERATION = "true"
  }

  if ($testUsedAssemblies) {
    $env:ROSLYN_TEST_USEDASSEMBLIES = "true"
  }

  $runTests = GetProjectOutputBinary "RunTests.dll" -tfm "net7.0"

  if (!(Test-Path $runTests)) {
    Write-Host "Test runner not found: '$runTests'. Run Build.cmd first." -ForegroundColor Red
    ExitWithExitCode 1
  }

  $dotnetExe = Join-Path $dotnet "dotnet.exe"
  $args += " --dotnet `"$dotnetExe`""
  $args += " --logs `"$LogDir`""
  $args += " --configuration $configuration"

  if ($testCoreClr) {
    $args += " --tfm net6.0 --tfm net7.0"
    $args += " --timeout 90"
    if ($testCompilerOnly) {
      $args += GetCompilerTestAssembliesIncludePaths
    } else {
      $args += " --tfm net6.0-windows"
      $args += " --include '\.UnitTests'"
    }
  }
  elseif ($testDesktop -or ($testIOperation -and -not $testCoreClr)) {
    $args += " --tfm net472"
    $args += " --timeout 90"

    if ($testCompilerOnly) {
      $args += GetCompilerTestAssembliesIncludePaths
    } else {
      $args += " --include '\.UnitTests'"
    }

    if ($testArch -ne "x86") {
      $args += " --exclude '\.InteractiveHost'"
    }

  } elseif ($testVsi) {
    $args += " --timeout 110"
    $args += " --tfm net472"
    $args += " --retry"
    $args += " --sequential"
    $args += " --include '\.IntegrationTests'"
    $args += " --include 'Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests'"

    if ($lspEditor) {
      $args += " --testfilter Editor=LanguageServerProtocol"
    }
  }

  if (-not $ci -and -not $testVsi) {
    $args += " --html"
  }

  if ($collectDumps) {
    $procdumpFilePath = Ensure-ProcDump
    $args += " --procdumppath $procDumpFilePath"
    $args += " --collectdumps";
  }

  $args += " --arch $testArch"

  if ($sequential) {
    $args += " --sequential"
  }

  if ($helix) {
    $args += " --helix"
  }

  if ($helixQueueName) {
    $args += " --helixQueueName $helixQueueName"
  }

  try {
    Write-Host "$runTests $args"
    Exec-Console $dotnetExe "$runTests $args"
  } finally {
    Get-Process "xunit*" -ErrorAction SilentlyContinue | Stop-Process
    if ($ci) {
      Remove-Item env:\ROSLYN_TEST_CI
    }

    # Note: remember to update TestRunner when using new environment variables
    # (they need to be transferred over to the Helix machines that run the tests)
    if ($testIOperation) {
      Remove-Item env:\ROSLYN_TEST_IOPERATION
    }

    if ($testUsedAssemblies) {
      Remove-Item env:\ROSLYN_TEST_USEDASSEMBLIES
    }

    if ($testVsi) {
      $serviceHubLogs = Join-Path $TempDir "servicehub\logs"
      if (Test-Path $serviceHubLogs) {
        Write-Host "Copying ServiceHub logs to $LogDir"
        Copy-Item -Path $serviceHubLogs -Destination (Join-Path $LogDir "servicehub") -Recurse
      } else {
        Write-Host "No ServiceHub logs found to copy"
      }

      $projectFaultLogs = Join-Path $TempDir "VsProjectFault_*.failure.txt"
      if (Test-Path $projectFaultLogs) {
        Write-Host "Copying VsProjectFault logs to $LogDir"
        Copy-Item -Path $projectFaultLogs -Destination $LogDir
      } else {
        Write-Host "No VsProjectFault logs found to copy"
      }

      if ($lspEditor) {
        $lspLogs = Join-Path $TempDir "VSLogs"
        $telemetryLog = Join-Path $TempDir "VSTelemetryLog"
        if (Test-Path $lspLogs) {
          Write-Host "Copying LSP logs to $LogDir"
          Copy-Item -Path $lspLogs -Destination (Join-Path $LogDir "LSP") -Recurse
        } else {
          Write-Host "No LSP logs found to copy"
        }

        if (Test-Path $telemetryLog) {
          Write-Host "Copying telemetry logs to $LogDir"
          Copy-Item -Path $telemetryLog -Destination (Join-Path $LogDir "Telemetry") -Recurse
        } else {
          Write-Host "No telemetry logs found to copy"
        }
      }
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
  $displayVersion = $vsInfo.catalog.productDisplayVersion

  $hive = "RoslynDev"
  Write-Host "Using VS Instance $vsId ($displayVersion) at `"$vsDir`""
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
    "ExpressionEvaluatorPackage.vsix",
    "Roslyn.VisualStudio.DiagnosticsWindow.vsix",
    "Microsoft.VisualStudio.IntegrationTest.Setup.vsix")

  foreach ($vsixFileName in $orderedVsixFileNames) {
    $vsixFile = Join-Path $VSSetupDir $vsixFileName
    $fullArg = "$baseArgs $vsixFile"
    Write-Host "`tInstalling $vsixFileName"
    Exec-Console $vsixExe $fullArg
  }

  # Set up registry
  $vsRegEdit = Join-Path (Join-Path (Join-Path $vsDir 'Common7') 'IDE') 'VsRegEdit.exe'

  # Disable roaming settings to avoid interference from the online user profile
  &$vsRegEdit set "$vsDir" $hive HKCU "ApplicationPrivateSettings\Microsoft\VisualStudio" RoamingEnabled string "1*System.Boolean*False"

  # Disable IntelliCode line completions to avoid interference with argument completion testing
  &$vsRegEdit set "$vsDir" $hive HKCU "ApplicationPrivateSettings\Microsoft\VisualStudio\IntelliCode" wholeLineCompletions string "0*System.Int32*2"

  # Disable IntelliCode RepositoryAttachedModels since it requires authentication which can fail in CI
  &$vsRegEdit set "$vsDir" $hive HKCU "ApplicationPrivateSettings\Microsoft\VisualStudio\IntelliCode" repositoryAttachedModels string "0*System.Int32*2"

  # Disable background download UI to avoid toasts
  &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Setup\BackgroundDownload" Value dword 0

  # Configure LSP
  $lspRegistryValue = [int]$lspEditor.ToBool()
  &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Roslyn\LSP\Editor" Value dword $lspRegistryValue
  &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Lsp\PullDiagnostics" Value dword $lspRegistryValue

  # Disable text editor error reporting because it pops up a dialog. We want to either fail fast in our
  # custom handler or fail silently and continue testing.
  &$vsRegEdit set "$vsDir" $hive HKCU "Text Editor" "Report Exceptions" dword 0

  # Configure RemoteHostOptions.OOP64Bit for testing
  $oop64bitValue = [int]$oop64bit.ToBool()
  &$vsRegEdit set "$vsDir" $hive HKCU "Roslyn\Internal\OnOff\Features" OOP64Bit dword $oop64bitValue

  # Configure RemoteHostOptions.OOPCoreClrFeatureFlag for testing
  $oopCoreClrFeatureFlagValue = [int]$oopCoreClr.ToBool()
  &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Roslyn\ServiceHubCore" Value dword $oopCoreClrFeatureFlagValue
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

  return $filePath
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

  $env:ROSLYN_OOP64BIT = "$oop64bit"
  $env:ROSLYN_OOPCORECLR = "$oopCoreClr"
  $env:ROSLYN_LSPEDITOR = "$lspEditor"
}

function Prepare-TempDir() {
  Copy-Item (Join-Path $RepoRoot "src\Workspaces\MSBuildTest\Resources\global.json") $TempDir
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

  if ($testVsi) {
    . (Join-Path $PSScriptRoot "build-utils-win.ps1")
  }

  Push-Location $RepoRoot

  Subst-TempDir

  if ($ci) {
    List-Processes
    Prepare-TempDir
    EnablePreviewSdks
    if ($testVsi) {
      Setup-IntegrationTestRun
    }

    $global:_DotNetInstallDir = Join-Path $RepoRoot ".dotnet"
    InstallDotNetSdk $global:_DotNetInstallDir $GlobalJson.tools.dotnet
  }

  if ($restore) {
    &(Ensure-DotNetSdk) tool restore
  }

  try
  {
    if ($bootstrap) {
      $bootstrapDir = Make-BootstrapBuild $bootstrapToolset
    }
  }
  catch
  {
    if ($ci) {
      Write-LogIssue -Type "error" -Message "(NETCORE_ENGINEERING_TELEMETRY=Build) Build failed"
    }
    throw $_
  }

  if ($restore -or $build -or $rebuild -or $pack -or $sign -or $publish) {
    BuildSolution
  }

  try
  {
    if ($testDesktop -or $testVsi -or $testIOperation -or $testCoreClr) {
      TestUsingRunTests
    }
  }
  catch
  {
    if ($ci) {
      Write-LogIssue -Type "error" -Message "(NETCORE_ENGINEERING_TELEMETRY=Test) Tests failed"
    }
    throw $_
  }

  if ($launch) {
    if (-not $build) {
      InitializeBuildTool
    }

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

  Unsubst-TempDir
  Pop-Location
}
