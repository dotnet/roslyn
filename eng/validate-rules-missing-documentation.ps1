[CmdletBinding(PositionalBinding=$false)]
param (
  [switch]$ci
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"


Write-Host "Building Microsoft.CodeAnalysis.Features"
Start-Process -FilePath "./.dotnet/dotnet.exe" -ArgumentList "build src/Features/Core/Portable/Microsoft.CodeAnalysis.Features.csproj -t:GenerateRulesMissingDocumentation -p:RoslynEnforceCodeStyle=false -p:RunAnalyzersDuringBuild=false -p:ContinuousIntegrationBuild=$ci -c Release"

exit 0