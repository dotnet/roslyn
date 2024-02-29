# This script is used to restore tools into repo. It's useful in CI where we need to get
# a few base tools, like pwsh, established before we can run the main build.
#
# Generally this is not necessary for developer machines as they run Restore.cmd (or 
# an equivalent) which gets the necessary tools on their machine.

Set-StrictMode -version 3.0
$ErrorActionPreference="Stop"

try {

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  $dotnet = Ensure-DotNetSdk
  Exec-Command $dotnet "tool restore"
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  Unsubst-TempDir
  Pop-Location
}
