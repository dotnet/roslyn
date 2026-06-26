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
# it's fine to call `build.ps1 -build` followed by `test-vsi.ps1` for integration testing.

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
  [string]$bootstrapDir = "",
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
  [string]$warnNotAsError = "",
  [switch][Alias('pb')]$productBuild = $false,
  [switch]$fromVMR = $false,
  [string]$solution = "Roslyn.slnx",

  # official build settings
  [string]$officialBuildId = "",
  [string]$officialSkipApplyOptimizationData = "",
  [string]$officialSourceBranchName = "",
  [string]$officialIbcDrop = "",
  [string]$officialVisualStudioDropAccessToken = "",

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
  Write-Host ""
  Write-Host "Advanced settings:"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -bootstrap                Build using a bootstrap compilers"
  Write-Host "  -bootstrapDir             Build using bootstrap compiler at specified location"
  Write-Host "  -msbuildEngine <value>    Msbuild engine to use to run build ('dotnet', 'vs', or unspecified)."
  Write-Host "  -collectDumps             Collect dumps from test runs"
  Write-Host "  -runAnalyzers             Run analyzers during build operations (short: -a)"
  Write-Host "  -skipDocumentation        Skip generation of XML documentation files"
  Write-Host "  -prepareMachine           Prepare machine for CI run, clean up processes after build"
  Write-Host "  -useGlobalNuGetCache      Use global NuGet cache."
  Write-Host "  -warnAsError              Treat all warnings as errors"
  Write-Host "  -warnNotAsError <codes>   Suppress specific warnings from being treated as errors (semi-colon delimited)"
  Write-Host "  -productBuild             Build the repository in product-build mode"
  Write-Host "  -fromVMR                  Set when building from within the VMR"
  Write-Host "  -solution                 Solution to build (default is Roslyn.slnx)"
  Write-Host ""
  Write-Host "Official build settings:"
  Write-Host "  -officialBuildId                                  An official build id, e.g. 20190102.3"
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
# example it's okay to infer $runAnalyzers based on other flags. It's not okay though to infer
# $build based on other test flags. It's possible the developer wanted only for testing
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

  OfficialBuildOnly "officialSkipApplyOptimizationData"
  OfficialBuildOnly "officialSourceBranchName"
  OfficialBuildOnly "officialVisualStudioDropAccessToken"

  if ($officialBuildId) {
    $script:useGlobalNuGetCache = $false
    $script:collectDumps = $true
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

  if ($bootstrapDir -ne "") {
    $script:bootstrap = $true
  }

  if ($build -and $launch -and -not $deployExtensions) {
    Write-Host -ForegroundColor Red "Cannot combine -build and -launch without -deployExtensions"
    exit 1
  }

  if ($bootstrap) {
    $script:restore = $true
  }

  if ($productBuild) {
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

function RestoreInternalTooling() {
  $internalToolingProject = Join-Path $RepoRoot 'eng/common/internal/Tools.csproj'
  # The restore config file might be set via env var. Ignore that for this operation,
  # as the internal nuget.config should be used.
  $restoreConfigFile = Join-Path $RepoRoot 'eng/common/internal/NuGet.config'
  MSBuild $internalToolingProject /t:Restore /p:RestoreConfigFile=$restoreConfigFile
}

function BuildSolution() {
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
  $msbuildWarnNotAsError = if ($warnAsError -and $warnNotAsError -ne "") { "/warnNotAsError:$warnNotAsError" } else { "" }

  # Workaround for some machines in the AzDO pool not allowing long paths
  $ibcDir = $RepoRoot

  $generateDocumentationFile = if ($skipDocumentation) { "/p:GenerateDocumentationFile=false" } else { "" }
  $roslynUseHardLinks = if ($ci) { "/p:ROSLYNUSEHARDLINKS=true" } else { "" }

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
      /p:VisualStudioIbcDrop=$ibcDropName `
      /p:VisualStudioDropAccessToken=$officialVisualStudioDropAccessToken `
      /p:DotNetBuild=$productBuild `
      /p:DotNetBuildFromVMR=$fromVMR `
      $suppressExtensionDeployment `
      $msbuildWarnAsError `
      $msbuildWarnNotAsError `
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
    $branchData = GetBranchPublishData
    return $branchData.vsBranch
  }

  return $global:_IbcSourceBranchName = calculate
}

function GetIbcDropName() {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute(
     'PSAvoidUsingConvertToSecureStringWithPlainText',
     '',
     Justification='$officialVisualStudioDropAccessToken is a script parameter so it needs to be plain text')]
    param()

    if ($officialIbcDrop -and $officialIbcDrop -ne "default"){
        return $officialIbcDrop
    }

    # Don't try and get the ibc drop if we're not in an official build as it won't be used anyway
    if (!$applyOptimizationData -or !$officialBuildId) {
        return ""
    }

    # Ensure that we have the internal tooling restored before attempting to load the powershell module.
    RestoreInternalTooling

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

  Process-Arguments

  . (Join-Path $PSScriptRoot "build-utils.ps1")

  Push-Location $RepoRoot

  Subst-TempDir

  if ($ci) {
    List-Processes
    EnablePreviewSdks

    $dotnet = (InitializeDotNetCli -install:$true)
  }

  if ($restore) {
    &(Ensure-DotNetSdk) tool restore
  }

  if ($bootstrap -and $bootstrapDir -eq "") {
    Write-Host "Building bootstrap Compiler"
    $bootstrapDir = Join-Path (Join-Path $ArtifactsDir "bootstrap") "build"
    & eng/make-bootstrap.ps1 -output $bootstrapDir -force -ci:$ci
    Test-LastExitCode
  }

  if ($restore -or $build -or $rebuild -or $pack -or $sign -or $publish) {
    BuildSolution
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
  if (Test-Path Function:\Unsubst-TempDir) {
    Unsubst-TempDir
  }
  Pop-Location
}
