param (
  [Parameter(Mandatory = $true)][string]$filePath
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "../eng/build-utils.ps1")

$fileInfo = Get-ItemProperty $filePath
$projectFileInfo = Get-ProjectFile $fileInfo
if ($projectFileInfo) {
  & dotnet build -p:RunAnalyzersDuringBuild=false -p:GenerateFullPaths=true $($projectFileInfo.FullName)
}
else {
  Write-Host "Failed to build project. $fileInfo is not part of a C# / VB project."
  exit 1
}
