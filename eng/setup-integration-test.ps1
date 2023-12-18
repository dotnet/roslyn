[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$configuration,
  [switch]$oop64bit,
  [switch]$oopCoreClr,
  [switch]$lspEditor,
  [switch]$ci
)

. (Join-Path $PSScriptRoot "build-utils.ps1")
. (Join-Path $PSScriptRoot "build-utils-win.ps1")

# Deploy our core VSIX libraries to Visual Studio via the Roslyn VSIX tool.  This is an alternative to
# deploying at build time.
function Deploy-VsixViaTool() {
  $vsixExe = Join-Path $ArtifactsDir "bin\RunTests\$configuration\net7.0\VSIXExpInstaller\VSIXExpInstaller.exe"
  Write-Host "VSIX EXE path: " $vsixExe
  if (-not (Test-Path $vsixExe)) {
    Write-Host "VSIX EXE not found: '$vsixExe'." -ForegroundColor Red
    ExitWithExitCode 1
  }

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
    "Roslyn.VisualStudio.ServiceHub.Setup.x64.vsix",
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

  # Disable text spell checker to avoid spurious warnings in the error list
  &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Editor\EnableSpellChecker" Value dword 0

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

  # Configure RemoteHostOptions.OOPCoreClr for testing
  $oopCoreClrValue = [int]$oopCoreClr.ToBool()
  &$vsRegEdit set "$vsDir" $hive HKCU "Roslyn\Internal\OnOff\Features" OOPCoreClr dword $oopCoreClrValue
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

if ($ci) {
  Setup-IntegrationTestRun
}

Deploy-VsixViaTool

if ($ci) {
  # Minimize all windows to avoid interference during integration test runs
  $shell = New-Object -ComObject "Shell.Application"
  $shell.MinimizeAll()
}