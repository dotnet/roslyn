# This is meant as a script for testing the rehydrate behavior on a local 
# dev machine. 
[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  Write-Host "Removing artifacts\bin"
  Remove-Item -Recurse -Force -Path artifacts\bin -ErrorAction SilentlyContinue
  
  Write-Host "Copying testPayload to artifacts\bin"
  Create-Directory -Recurse -Force -Path artifacts\bin
  Copy-Item -Recurse artifacts\testPayload\* artifacts\bin

  Write-Host "Calling rehydrate-tests"
  & eng\rehydrate-tests.ps1
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
