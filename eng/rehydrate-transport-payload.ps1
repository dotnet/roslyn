# This script will be called on the test machine when the transport payload is 
# still in the raw unzipped form. The job of this script is to undo the packing
# that prepare-transport-payload did
[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$transportDirectory = "",
  [string]$targetDirectory = "")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  Push-Location artifacts\bin
  & .\rehydrate.cmd
  Pop-Location

  Ensure-DotNetSdk
  & eng\build.ps1 -restore
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
