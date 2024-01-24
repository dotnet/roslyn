param (
    [string]$rootDirectory,
    [string[]]$includeDirectories
)
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  $dotnet = Ensure-DotnetSdk
  Exec-Console $dotnet "tool run dotnet-format -v detailed whitespace $rootDirectory --folder --include-generated --include $includeDirectories --verify-no-changes"

  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}