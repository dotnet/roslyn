[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug", [switch]$noBuild)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  $dotnet = Ensure-DotnetSdk
  # permissions issues make this a pain to do in PrepareTests itself.
  Remove-Item -Recurse -Force "$RepoRoot\artifacts\testPayload" -ErrorAction SilentlyContinue
  $noBuildArg = if ($noBuild) { "--no-build" } else { "" }
  Exec-Console $dotnet "run --project src\Tools\PrepareTests\PrepareTests.csproj --configuration $configuration $noBuildArg -- --source $RepoRoot --destination $RepoRoot\artifacts\testPayload"
  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
