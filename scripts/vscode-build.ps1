param (
  [Parameter(Mandatory = $true)][string]$filePath
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "../eng/build-utils.ps1")

$fileInfo = Get-ItemProperty $filePath
$projectFileInfo = Get-ProjectFile $fileInfo
if ($projectFileInfo) {
  $dotnetPath = Resolve-Path (Ensure-DotNetSdk) -Relative
  $projectDir = Resolve-Path $projectFileInfo.Directory -Relative

  $invocation = "$dotnetPath msbuild $projectDir -p:UseRoslynAnalyzers=false -p:GenerateFullPaths=true"
  Write-Output "> $invocation"
  Invoke-Expression $invocation

  exit 0
}
else {
  Write-Host "Failed to build project. $fileInfo is not part of a C# project."

  exit 1
}
