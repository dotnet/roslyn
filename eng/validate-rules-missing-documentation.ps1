[CmdletBinding(PositionalBinding=$false)]
param (
  [switch]$ci = $false,
  [switch]$warnAsError = $ci
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

Write-Host "Building Microsoft.CodeAnalysis.Features"
try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot
  $prepareMachine = $ci

  $projectFilePath = Join-Path $RepoRoot "src\Features\Core\Portable\Microsoft.CodeAnalysis.Features.csproj"
  $msbuildWarnAsError = if ($warnAsError) { "/warnAsError" } else { "" }
  Exec-DotNet "build $projectFilePath -t:GenerateRulesMissingDocumentation -p:RunAnalyzersDuringBuild=false -p:ContinuousIntegrationBuild=$ci -p:TreatWarningsAsErrors=$warnAsError $msbuildWarnAsError -c Release"
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
