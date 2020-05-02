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
  $buildArgs = "$($buildTool.Command) -v:m -m -p:UseRoslynAnalyzers=false -p:GenerateFullPaths=true$frameworkArg $($projectFileInfo.FullName)"

  $buildToolPath = $buildTool.Path

  if ($msbuildEngine -ne "vs") {
    # InitializeBuildTool is hardcoded to dotnet.exe. If we're not building using vs (meaning msbuild), use
    # Ensure-DotnetSdk instead to get the xplat dotnet.exe. If we're using vs, that's not supported xplat anyway
    $buildToolPath = Ensure-DotnetSdk
  }

  Write-Host "$($buildToolPath) $buildArgs"
  Exec-Console $buildToolPath $buildArgs
  exit 0
}
else {
  Write-Host "Failed to build project. $fileInfo is not part of a C# / VB project."
  exit 1
}
