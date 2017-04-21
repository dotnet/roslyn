
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    $p = Get-VisualStudioDir
    Write-Host (Join-Path $p "Common7\Tools\")
}
catch {
  Write-Error $_.Exception.Message
  # Return an empty string and let the caller fallback or handle this as appropriate
  return ""
}
