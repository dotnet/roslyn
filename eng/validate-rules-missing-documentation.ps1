[CmdletBinding(PositionalBinding=$false)]
param (
  [switch]$ci = $false
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

Write-Host "Building Microsoft.CodeAnalysis.Features"
try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot
  $prepareMachine = $ci

  $projectFilePath = Join-Path $RepoRoot "src\Features\Core\Portable\Microsoft.CodeAnalysis.Features.csproj"
  Exec-DotNet "build $projectFilePath -t:GenerateRulesMissingDocumentation -p:RoslynEnforceCodeStyle=false -p:RunAnalyzersDuringBuild=false -p:ContinuousIntegrationBuild=$ci -c Release"
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
