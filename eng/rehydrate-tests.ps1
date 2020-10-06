[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  Push-Location artifacts\bin
  & .\rehydrate.cmd
  Pop-Location
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
