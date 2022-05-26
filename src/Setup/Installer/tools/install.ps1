[CmdletBinding(PositionalBinding=$false)]
param (
    [string][Alias("hive")]$rootSuffix = ""
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "utils.ps1")

  # Welcome Message
  Write-Host "Installing Roslyn Preview Build" -ForegroundColor Green

  # Find VS Instance
  $vsInstances = Get-VisualStudioInstances

  # We are given two strings per VS instance (vsdir and vsid)
  # Check to see if more than one instance meets our reqs
  if ($vsInstances.Count -gt 1) {
    while ($true) {
      Write-Host "Multiple Visual Studio instances detected" -ForegroundColor White
      Write-Host "Please select an instance to install into:" -ForegroundColor White

      for ($i=0; $i -lt $vsInstances.Length; $i++) {
        $devenvPath = Join-Path $vsInstances[$i].installationPath "Common7\IDE\devenv.exe"
        Write-Host "[$($i + 1)]:  $devenvPath" -ForegroundColor White
      }

      $input = Read-Host
      $vsInstallNumber = $input -as [int]
      if (($vsInstallNumber -is [int]) -and ($vsInstallNumber -gt 0) -and ($vsInstallNumber -le $vsInstances.Length)) {
        $vsInstance = $vsInstances[$vsInstallNumber - 1]
        break
      }

      Write-Host ""
    }
  } else {
    $vsInstance = $vsInstances[0]
  }

  $vsDir = $vsInstance.installationPath.Trim("\")
  $vsId = $vsInstance.instanceId
  $vsMajorVersion = $vsInstance.installationVersion.Split(".")[0]
  $vsLocalDir = Get-VisualStudioLocalDir -vsMajorVersion $vsMajorVersion -vsId $vsId -rootSuffix $rootSuffix

  Stop-Processes $vsDir $vsLocalDir

  # Install VSIX
  $vsExe = Join-Path $vsDir "Common7\IDE\devenv.exe"

  Write-Host "Installing Preview into $vsExe" -ForegroundColor Gray
  Uninstall-VsixViaTool -vsDir $vsDir -vsId $vsId -rootSuffix $rootSuffix
  Install-VsixViaTool -vsDir $vsDir -vsId $vsId -rootSuffix $rootSuffix

  # Clear MEF Cache
  Write-Host "Refreshing MEF Cache" -ForegroundColor Gray
  $mefCacheFolder = Get-MefCacheDir $vsLocalDir
  if (Test-Path $mefCacheFolder) {
    Get-ChildItem -Path $mefCacheFolder -Include *.* -File -Recurse | foreach { Remove-Item $_ }
  }

  $args = "/updateconfiguration"
  Exec-Console $vsExe $args

  Write-Host "Install Succeeded" -ForegroundColor Green
  exit 0
}
catch {
  Write-Host $_ -ForegroundColor Red
  Write-Host $_.Exception -ForegroundColor Red
  Write-Host $_.ScriptStackTrace -ForegroundColor Red
  exit 1
}
