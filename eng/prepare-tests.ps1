[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  $dotnet = Ensure-DotnetSdk
  Exec-Console $dotnet "run --project src\Tools\PrepareTests\PrepareTests.csproj $RepoRoot\artifacts\bin $RepoRoot\artifacts\testPayload"
  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
