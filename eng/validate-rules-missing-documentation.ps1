[CmdletBinding(PositionalBinding=$false)]
param (
  [switch]$ci = $false,
  # Consumed implicitly by the MSBuild helper.
  [switch]$warnAsError = $ci
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

Write-Host "Building Microsoft.CodeAnalysis.Features"
try {
  $configuration = "Release"
  $msbuildEngine = "dotnet"
  $disablePipelineSetResult = $true
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot
  $prepareMachine = $ci

  $projectFilePath = Join-Path $RepoRoot "src\Features\Core\Portable\Microsoft.CodeAnalysis.Features.csproj"
  # Use Arcade's MSBuild helper for correct warnAsError/warnNotAsError behavior.
  MSBuild $projectFilePath `
    /restore `
    /t:GenerateRulesMissingDocumentation `
    /p:RunAnalyzersDuringBuild=false `
    /p:Configuration=$configuration `
    "/bl:$(Join-Path $LogDir "RulesMissingDocumentation.binlog")"
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  Pop-Location
}
