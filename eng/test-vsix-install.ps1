# Test script for VSIX installation with hardcoded values
# Hardcoded values as requested
$VSSetupDir = "C:\Users\dabarbet\source\repos\roslyn\artifacts\VSSetup\Debug"
$hive = "RoslynDev"


. (Join-Path $PSScriptRoot "build-utils.ps1")


$vsInfo = LocateVisualStudio
$vsDir = $vsInfo.installationPath.TrimEnd("\")

Write-Host "Using Visual Studio installation instanceId $($vsInfo.instanceId)"

$baseArgs = "/rootSuffix:$hive /shutdownprocesses /instanceIds:$($vsInfo.instanceId)"


# Single VSIX file as requested (compiler extension)
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
  $vsixInstallerExe = Join-Path $vsDir "Common7\IDE\VSIXInstaller.exe"
  $fullArg = "$baseArgs $vsixFile"

  #Exec-Command $vsixInstallerExe "$baseArgs /uninstall:Roslyn.Compilers.Extension"

  Write-Host "`tInstalling $vsixFileName"
  # Using direct command instead of Exec-Command to keep it simple
  Exec-Command $vsixInstallerExe $fullArg
}