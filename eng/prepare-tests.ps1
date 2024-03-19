[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  # permissions issues make this a pain to do in PrepareTests itself.
  Remove-Item -Recurse -Force "$RepoRoot\artifacts\testPayload" -ErrorAction SilentlyContinue
  $dotnet = Ensure-DotNetSdk
  Exec-Command $dotnet "exec $RepoRoot\artifacts\bin\PrepareTests\$configuration\net8.0\PrepareTests.dll --source $RepoRoot --destination $RepoRoot\artifacts\testPayload --dotnetPath `"$dotnet`""
  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
