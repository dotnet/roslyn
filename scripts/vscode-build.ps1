param (
  [Parameter(Mandatory = $true)][string]$filePath,
  [string]$msbuildEngine = "vs"
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "../eng/build-utils.ps1")

$fileInfo = Get-ItemProperty $filePath
$projectFileInfo = Get-ProjectFile $fileInfo
if ($projectFileInfo) {
  $buildTool = InitializeBuildTool
  $buildArgs = "$($buildTool.Command) -v:m -m -p:UseRoslynAnalyzers=false -p:GenerateFullPaths=true $($projectFileInfo.FullName)"

  Write-Host "$($buildTool.Path) $buildArgs"
  Exec-Console $buildTool.Path $buildArgs
  exit 0
}
else {
  Write-Host "Failed to build project. $fileInfo is not part of a C# / VB project."
  exit 1
}
