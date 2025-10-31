[CmdletBinding(PositionalBinding=$false)]
param(
  [switch]$enableDumps = $false,
  [string]$logDir = "")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  if ($logDir -eq "") {
    Write-Host "Usage: toggle-dumps.ps1 -logDir <path>"
    exit 1
  }

  if ($enableDumps) {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    New-Item -Path $key -ErrorAction SilentlyContinue
    New-ItemProperty -Path $key -Name 'DumpType' -PropertyType 'DWord' -Value 2 -Force
    New-ItemProperty -Path $key -Name 'DumpCount' -PropertyType 'DWord' -Value 10 -Force
    New-ItemProperty -Path $key -Name 'DumpFolder' -PropertyType 'String' -Value $LogDir -Force
  }
  else {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    Remove-ItemProperty -Path $key -Name 'DumpType'
    Remove-ItemProperty -Path $key -Name 'DumpCount'
    Remove-ItemProperty -Path $key -Name 'DumpFolder'
  }

  exit 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
