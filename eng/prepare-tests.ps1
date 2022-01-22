[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  $dotnet = Ensure-DotnetSdk
  # permissions issues make this a pain to do in PrepareTests itself.
  Remove-Item -Recurse -Force "$RepoRoot\artifacts\testPayload" -ErrorAction SilentlyContinue
  Exec-Console $dotnet "$RepoRoot\artifacts\bin\PrepareTests\$configuration\net6.0\PrepareTests.dll --source $RepoRoot --destination $RepoRoot\artifacts\testPayload"
  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
