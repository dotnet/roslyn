[CmdletBinding(PositionalBinding=$false)]
param (
    [string][Alias("hive")]$rootSuffix = ""
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "utils.ps1")

  # Find VS Instance
  $vsInstances = Get-VisualStudioInstances

  # Uninstall VSIX from all instances
  Write-Host "Uninstalling Roslyn Preview ..." -ForegroundColor Green

  foreach ($vsInstance in $vsInstances) {
    $vsDir = $vsInstance.installationPath.Trim("\")
    $vsId = $vsInstance.instanceId
    $vsMajorVersion = $vsInstance.installationVersion.Split(".")[0]
    $vsLocalDir = Get-VisualStudioLocalDir -vsMajorVersion $vsMajorVersion -vsId $vsId -rootSuffix $rootSuffix
    
    Stop-Processes $vsDir $vsLocalDir

    Uninstall-VsixViaTool -vsDir $vsDir -vsId $vsId -rootSuffix $rootSuffix

    # Clear MEF Cache
    Write-Host "Refreshing MEF Cache" -ForegroundColor Gray
    $mefCacheFolder = Get-MefCacheDir $vsLocalDir
    if (Test-Path $mefCacheFolder) {
      Get-ChildItem -Path $mefCacheFolder -Include *.* -File -Recurse | foreach { Remove-Item $_ }
    }

    $vsExe = Join-Path $vsDir "Common7\IDE\devenv.exe"
    $args = "/updateconfiguration"
    Exec-Console $vsExe $args
  }

  Write-Host "Uninstall Succeeded" -ForegroundColor Green
  exit 0
}
catch {
  Write-Host $_ -ForegroundColor Red
  Write-Host $_.Exception -ForegroundColor Red
  Write-Host $_.ScriptStackTrace -ForegroundColor Red
  exit 1
}
