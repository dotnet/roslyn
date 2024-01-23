param (
    [string]$rootDirectory,
    [string[]]$includeDirectories
)

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  $dotnet = Ensure-DotnetSdk
  Exec-Console $dotnet "tool run dotnet-format -v diag whitespace $rootDirectory --folder --include-generated --include $includeDirectories --verify-no-changes"

  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}