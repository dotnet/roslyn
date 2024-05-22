param (
    [string]$rootDirectory,
    [string[]]$includeDirectories,
    [switch]$ci = $false
)
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot
  $prepareMachine = $ci

  Exec-DotNet "tool run dotnet-format -v detailed whitespace $rootDirectory --folder --include-generated --include $includeDirectories --verify-no-changes"

  ExitWithExitCode 0
}
catch {
  Write-Host $_
  ExitWithExitCode 1
}
finally {
  Pop-Location
}