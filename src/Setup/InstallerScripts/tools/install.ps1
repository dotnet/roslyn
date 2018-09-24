Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "utils.ps1")

  if (Test-Process "devenv") {
      Write-Host "Please shut down all instances of Visual Studio before running" -ForegroundColor Red
      exit 1
  }

  # Welcome Message
  Write-Host "Installing Roslyn Insiders Build" -ForegroundColor Green

  # Find VS Instance
  $vsInstalls = Get-VisualStudioDirAndId
  $vsDir = $vsInstalls[0].Trim("\")
  $vsId = $vsInstalls[1]
  # We are given two strings per VS instance (vsdir and vsid)
  # Check to see if more than one instance meets our reqs
  if ($vsInstalls.Count -gt 2) {
    while ($true) {
      Write-Host "Multiple Visual Studio Installs Detected" -ForegroundColor White
      Write-Host "Please Select an Instance to Install Into:" -ForegroundColor White
      $number=1
      For($i=0; $i -lt $vsInstalls.Count; $i+=2){
        $tempVsDir = $vsInstalls[$i].Trim("\")
        $tempVsExe = Join-Path $tempVsDir "Common7\IDE\devenv.exe"
        Write-Host "[$number]:  $tempVsExe" -ForegroundColor White
        $number++
      }

      $input = Read-Host
      $vsInstallNumber = $input -as [int]
      if ($vsInstallNumber -is [int] -and $vsInstallNumber -le ($number-1)) {
        $index = ($vsInstallNumber -1) * 2
        $vsDir = $vsInstalls[$index].Trim("\")
        $vsId = $vsInstalls[$index+1]
        break
      }

      Write-Host ""
    }
  }

  # Install VSIX
  $vsExe = Join-Path $vsDir "Common7\IDE\devenv.exe"
  Write-Host "Installing Preview Into $vsExe" -ForegroundColor Gray
  Uninstall-VsixViaTool -vsDir $vsDir -vsId $vsId -hive ""
  Install-VsixViaTool -vsDir $vsDir -vsId $vsId -hive ""

  # Clear MEF Cache
  Write-Host "Refreshing MEF Cache" -ForegroundColor Gray
  $mefCacheFolder = Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio\15.0_$vsId\ComponentModelCache"
  Get-ChildItem -Path $mefCacheFolder -Include *.* -File -Recurse | foreach { Remove-Item $_}
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
