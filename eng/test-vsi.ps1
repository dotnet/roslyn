#
# This script runs the Visual Studio integration tests. It handles VSIX deployment,
# VS configuration, test execution, and log collection.
#
# Typically invoked after build.ps1 has completed the build phase:
#   build.ps1 -build -ci -configuration Debug
#   test-vsi.ps1 -ci -configuration Debug
#

[CmdletBinding(PositionalBinding=$false)]
param (
  [string][Alias('c')]$configuration = "Debug",
  [switch]$ci,
  [switch]$collectDumps,
  [switch]$prepareMachine,
  [switch]$skipCustomRoslynDeploy = $false,
  [switch]$oop64bit = $true,
  [switch]$lspEditor = $false,
  [string]$testFilter = "",
  [switch]$help,

  [parameter(ValueFromRemainingArguments=$true)][string[]]$properties)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function Print-Usage() {
  Write-Host "Usage: test-vsi.ps1 [options]"
  Write-Host ""
  Write-Host "Options:"
  Write-Host "  -configuration <value>    Build configuration: Debug or Release (default: Debug)"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -collectDumps             Collect dumps on test crashes and timeouts"
  Write-Host "  -prepareMachine           Prepare machine for CI run (clean up processes)"
  Write-Host "  -skipCustomRoslynDeploy   Skip custom Roslyn deployment (uses Roslyn from the VS)"
  Write-Host "  -oop64bit                 Run OOP in 64-bit mode (default: true)"
  Write-Host "  -lspEditor                Use LSP editor (default: false)"
  Write-Host "  -testFilter               Filter tests to run (maps to --filter parameter of xunit)"
  Write-Host "  -help                     Print help and exit"
}

if ($help) {
  Print-Usage
  exit 0
}

$RepoRoot = Join-Path $PSScriptRoot ".." -Resolve

. (Join-Path $PSScriptRoot "build-utils.ps1")
. (Join-Path $PSScriptRoot "build-utils-win.ps1")

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
  $env:ROSLYN_LSPEDITOR = "$lspEditor"
}

function Deploy-VsixViaTool() {

  # Create a log file name for vsix installation.  The vsix installer will append to this log (not overwrite)
  # so we can re-use the same log file for all our install operations.
  # VSIX installer will always write the log file to %temp% and ignores full paths.
  $logFileName = "VSIXInstaller-" + [guid]::NewGuid().ToString() + ".log"

  $vsInfo = LocateVisualStudio
  if ($vsInfo -eq $null) {
    throw "Unable to locate required Visual Studio installation"
  }

  try {
    $vsDir = $vsInfo.installationPath.TrimEnd("\")
    $script:vsId = $vsInfo.instanceId
    $script:vsMajorVersion = $vsInfo.installationVersion.Split('.')[0]
    $displayVersion = $vsInfo.catalog.productDisplayVersion

    $script:hive = "RoslynDev"

    Write-Host "Using VS Instance $vsId ($displayVersion) at `"$vsDir`""

    if (-not $skipCustomRoslynDeploy) {
      # InstanceIds is required here to ensure it installs the vsixes only into the specified VS instance.
      # The default installer behavior without it is to install into every installed VS instance.
      $baseArgs = "/rootSuffix:$hive /quiet /shutdownprocesses /instanceIds:$vsId /logFile:$logFileName"

      $vsixInstallerExe = Join-Path $vsDir "Common7\IDE\VSIXInstaller.exe"

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

      Write-Host "Installing all Roslyn and Razor VSIXs"

      # VSIX files need to be installed in this specific order:
      $orderedVsixFileNames = @(
        "Roslyn.Compilers.Extension.vsix",
        "Roslyn.VisualStudio.Setup.vsix",
        "Roslyn.VisualStudio.ServiceHub.Setup.x64.vsix",
        "Roslyn.VisualStudio.Setup.Dependencies.vsix",
        "Microsoft.VisualStudio.RazorExtension.Dependencies.vsix",
        "Microsoft.VisualStudio.RazorExtension.vsix",
        "ExpressionEvaluatorPackage.vsix",
        "Roslyn.VisualStudio.DiagnosticsWindow.vsix",
        "Microsoft.VisualStudio.IntegrationTest.Setup.vsix")

      foreach ($vsixFileName in $orderedVsixFileNames) {
        $vsixFile = Join-Path $VSSetupDir $vsixFileName
        $fullArg = "$baseArgs $vsixFile"
        Write-Host "`tInstalling $vsixFileName"
        Exec-Command $vsixInstallerExe $fullArg
      }
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

    # Run source generators automatically during integration tests.
    &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Roslyn\SourceGeneratorExecutionBalanced" Value dword 0

    # Configure LSP
    $lspRegistryValue = [int]$lspEditor.ToBool()
    &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Roslyn\LSP\Editor" Value dword $lspRegistryValue
    &$vsRegEdit set "$vsDir" $hive HKCU "FeatureFlags\Lsp\PullDiagnostics" Value dword 1

    # Disable text editor error reporting because it pops up a dialog. We want to either fail fast in our
    # custom handler or fail silently and continue testing.
    &$vsRegEdit set "$vsDir" $hive HKCU "Text Editor" "Report Exceptions" dword 0

    # Configure RemoteHostOptions.OOP64Bit for testing
    $oop64bitValue = [int]$oop64bit.ToBool()
    &$vsRegEdit set "$vsDir" $hive HKCU "Roslyn\Internal\OnOff\Features" OOP64Bit dword $oop64bitValue

    # Disable targeted notifications
    if ($ci) {
      # Currently does not work via vsregedit, so only apply this setting in CI
      #&$vsRegEdit set "$vsDir" $hive HKCU "RemoteSettings" TurnOffSwitch dword 1
      reg add hkcu\Software\Microsoft\VisualStudio\RemoteSettings /f /t REG_DWORD /v TurnOffSwitch /d 1
    }
  } finally {
    $vsixInstallerLogs = Join-Path $TempDir $logFileName
    CopyToArtifactLogs $vsixInstallerLogs
  }
}

function CopyToArtifactLogs($inputPath) {
  if (Test-Path $inputPath) {
    Write-Host "Copying $inputPath to $LogDir"
    Copy-Item -Path $inputPath -Destination $LogDir
  } else {
    Write-Host "No log found to copy at $inputPath"
  }
}

function TestUsingRunTests() {

  # Tests need to locate .NET Core SDK
  $dotnet = InitializeDotNetCli

  Deploy-VsixViaTool

  if ($ci) {
    # Minimize all windows to avoid interference during integration test runs
    $shell = New-Object -ComObject "Shell.Application"
    $shell.MinimizeAll()
  }

  if ($ci) {
    $env:ROSLYN_TEST_CI = "true"
  }

  $runTests = GetProjectOutputBinary "RunTests.dll" -tfm "net10.0"

  if (!(Test-Path $runTests)) {
    Write-Host "Test runner not found: '$runTests'. Run Build.cmd first." -ForegroundColor Red
    ExitWithExitCode 1
  }

  $dotnetExe = Join-Path $dotnet "dotnet.exe"
  $args += " --dotnet `"$dotnetExe`""
  $args += " --logs `"$LogDir`""
  $args += " --testConfiguration $configuration"
  $testFilters = @()

  $args += " --testFramework:core --testFramework:desktop"
  $args += " --sequential"
  $args += " --include '\.IntegrationTests'"
  $args += " --include 'Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests'"

  if ($lspEditor) {
    $testFilters += "Editor=LanguageServerProtocol"
  }

  if ($testFilter -ne "") {
    $testFilters += $testFilter
  }

  if ($testFilters.Count -eq 1) {
    $args += " --testfilter $($testFilters[0])"
  }
  elseif ($testFilters.Count -gt 1) {
    $combinedTestFilter = ($testFilters | ForEach-Object { "($_)" }) -join "&"
    $args += " --testfilter $combinedTestFilter"
  }

  if ($collectDumps) {
    $args += " --collectdumps";
  }

  if ($ci) {
    $args += " --ci"
    $args += " --timeout 220"
  }

  try {
    Write-Host "$runTests $args"
    Exec-Command $dotnetExe "$runTests $args"
  } finally {
    Get-Process "xunit*" -ErrorAction SilentlyContinue | Stop-Process
    if ($ci) {
      Remove-Item env:\ROSLYN_TEST_CI
    }

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

    if ($vsId) {
      $activityLogPath = Join-Path ${env:USERPROFILE} "AppData\Roaming\Microsoft\VisualStudio\$vsMajorVersion.0_$($vsId)$hive\ActivityLog.xml"
      $devenvExeConfig = Join-Path ${env:USERPROFILE} "AppData\Local\Microsoft\VisualStudio\$vsMajorVersion.0_$($vsId)$hive\devenv.exe.config"
      $mefErrors = Join-Path ${env:USERPROFILE} "AppData\Local\Microsoft\VisualStudio\$vsMajorVersion.0_$($vsId)$hive\ComponentModelCache\Microsoft.VisualStudio.Default.err"
      CopyToArtifactLogs $activityLogPath
      CopyToArtifactLogs $devenvExeConfig
      CopyToArtifactLogs $mefErrors
    } else {
      Write-Host "No Visual Studio instance found to copy logs from"
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

try {
  Push-Location $RepoRoot

  if ($ci) {
    Setup-IntegrationTestRun
  }

  try
  {
    TestUsingRunTests
  }
  catch
  {
    if ($ci) {
      Write-LogIssue -Type "error" -Message "(NETCORE_ENGINEERING_TELEMETRY=Test) Tests failed"
    }
    throw $_
  }

  ExitWithExitCode 0
}
catch [exception] {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  Pop-Location
}
