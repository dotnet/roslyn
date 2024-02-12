param (
  [Parameter(Mandatory = $true)][string]$filePath,
  [string]$msbuildEngine = "vs",
  [string]$framework = ""
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "../eng/build-utils.ps1")

$fileInfo = Get-ItemProperty $filePath
$projectFileInfo = Get-ProjectFile $fileInfo
if ($projectFileInfo) {
  $buildTool = InitializeBuildTool
  $frameworkArg = if ($framework -ne "") { " -p:TargetFramework=$framework" } else { "" }
  $buildArgs = "$($buildTool.Command) -v:m -m -p:RunAnalyzersDuringBuild=false -p:GenerateFullPaths=true$frameworkArg $($projectFileInfo.FullName)"

  Exec-Command $buildTool.Path $buildArgs
  exit 0
}
else {
  Write-Host "Failed to build project. $fileInfo is not part of a C# / VB project."
  exit 1
}
