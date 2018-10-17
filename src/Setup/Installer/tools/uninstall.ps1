Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "utils.ps1")
  if (Test-Process "devenv") {
      Write-Host "Please shut down all instances of Visual Studio before running" -ForegroundColor Red
      exit 1
  }
  # Find VS Instance
  $vsInstalls = Get-VisualStudioDirAndId
  $vsDir = $vsInstalls[0].Trim("\")
  $vsId = $vsInstalls[1]
  # Uninstall VSIX
  Write-Host "Uninstalling Preview Everywhere..." -ForegroundColor Green
  for ($i = 0; $i -lt $vsInstalls.Count;  $i+=2) {
      $vsDir = $vsInstalls[$i].Trim("\")
      $vsId = $vsInstalls[$i+1]
      Uninstall-VsixViaTool -vsDir $vsDir -vsId $vsId -hive ""
      # Clear MEF Cache
      Write-Host "Refreshing MEF Cache" -ForegroundColor Gray
      $mefCacheFolder = Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio\15.0_$vsId\ComponentModelCache"
      Get-ChildItem -Path $mefCacheFolder -Include *.* -File -Recurse | foreach { Remove-Item $_}
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
