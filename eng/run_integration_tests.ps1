[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$vsixExpInstallerExe,
  [switch]$oop64bit = $true,
  [switch]$oopCoreClr = $false,
  [switch]$lspEditor = $false
)

function LocateVisualStudio([string] $vsWhereExe, [string] $vsVersion)
{
  $args = @('-latest', '-format', 'json', '-requires', 'Microsoft.Component.MSBuild', '-products', '*', '-prerelease')
  $args += '-version'
  $args += $vsVersion

  $vsInfo =& $vsWhereExe $args | ConvertFrom-Json
  
  if ($lastExitCode -ne 0) {
    return $null
  }

  # use first matching instance
  return $vsInfo[0]
}

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to
# deploying at build time.
function Deploy-VsixViaTool([string] $vsixExe, [string] $vsSetupDir, [object] $vsInfo, [switch]$oop64bit, [switch]$oopCoreClr, [switch]$lspEditor) {
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
      $vsixFile = Join-Path $vsSetupDir $vsixFileName
      $fullArg = "$baseArgs $vsixFile"
      Write-Host "`tInstalling $vsixFileName"
      & cmd.exe /c "$vsixExe $fullArg"
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

function SetIntegrationTestEnvironmentVariables([switch]$oop64bit, [switch]$oopCoreClr, [switch]$lspEditor)
{
  $env:ROSLYN_OOP64BIT = "$oop64bit"
  $env:ROSLYN_OOPCORECLR = "$oopCoreClr"
  $env:ROSLYN_LSPEDITOR = "$lspEditor"
}

SetIntegrationTestEnvironmentVariables $oop64bit $oopCoreClr $lspEditor

$vsInfo = LocateVisualStudio "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" "17.0"

Deploy-VsixViaTool  $vsixExpInstallerExe (Join-Path $PSScriptRoot "VSSetup\Debug") $vsInfo $oop64bit $oopCoreClr $lspEditor