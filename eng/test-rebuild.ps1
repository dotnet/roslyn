<#
  This script tests that Roslyn artifacts are rebuildable--i.e. that the source code and resources can be identified 
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Debug",
  [switch]$ci = $false,
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-rebuild.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -help                     Print help and exit"
}

try {
  if ($help) {
    Print-Usage
    exit 0
  }

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  Write-Host "Building Roslyn"
  Exec-Console (Join-Path $PSScriptRoot "build.ps1") "-restore -build -ci:$ci -configuration:$configuration -pack -binaryLog"
  Exec-Console "artifacts\bin\BuildValidator\$configuration\net472\BuildValidator.exe" "--assembliesPath '$ArtifactsDir/obj/Microsoft.CodeAnalysis'"

  exit 0
}
catch [exception] {
  Write-Host $_
  Write-Host $_.Exception
  exit 1
}
finally {
  Pop-Location
}
