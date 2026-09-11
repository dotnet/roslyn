[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptPath = Join-Path $repoRoot "eng\prepare-nuget-package-upgrade-payload.ps1"
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("prepare-nuget-package-upgrade-payload-" + [guid]::NewGuid())

function New-TestPackage([string]$id, [string]$version, [string]$dependencies, [bool]$namespaced = $true) {
  $packagePath = Join-Path $packageDirectory "$id.$version.nupkg"
  $packageContentDirectory = Join-Path $testRoot $id
  New-Item -Path $packageContentDirectory -ItemType Directory -Force | Out-Null
  $nuspecPath = Join-Path $packageContentDirectory "$id.nuspec"
  $namespace = if ($namespaced) { ' xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"' } else { '' }
  @"
<?xml version="1.0" encoding="utf-8"?>
<package$namespace>
  <metadata>
    <id>$id</id>
    <version>$version</version>
    <authors>Test</authors>
    <description>Test package</description>
    $dependencies
  </metadata>
</package>
"@ | Set-Content -Path $nuspecPath -Encoding UTF8

  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::CreateFromDirectory($packageContentDirectory, $packagePath)
}

try {
  $packageDirectory = Join-Path $testRoot "packages"
  $outputDirectory = Join-Path $testRoot "payload"
  New-Item -Path $packageDirectory -ItemType Directory -Force | Out-Null

  New-TestPackage -id "Microsoft.CodeAnalysis.Common" -version "5.0.0" -dependencies "" -namespaced $false
  New-TestPackage -id "Microsoft.CodeAnalysis.CSharp" -version "5.0.0" -dependencies @"
<dependencies>
  <dependency id="External.Ungrouped" version="[1.0.0]" />
  <group targetFramework=".NETStandard2.0">
    <dependency id="Microsoft.CodeAnalysis.Common" version="[5.0.0]" />
    <dependency id="External.Grouped" version="[2.0.0]" />
  </group>
</dependencies>
"@

  & $scriptPath -PackageDirectory $packageDirectory -OutputDirectory $outputDirectory

  $manifest = Get-Content -Path (Join-Path $outputDirectory "package-manifest.json") -Raw | ConvertFrom-Json
  $csharpPackage = $manifest.Packages | Where-Object Id -eq "Microsoft.CodeAnalysis.CSharp"
  if ($null -eq $csharpPackage) {
    throw "Expected the namespaced NuSpec package to be in the manifest."
  }

  if ($csharpPackage.ExternalDependencies.Id -notcontains "External.Ungrouped" -or $csharpPackage.ExternalDependencies.Id -notcontains "External.Grouped") {
    throw "Expected grouped and ungrouped external dependencies in the manifest."
  }

  if (($csharpPackage.ExternalDependencies | Where-Object Id -eq "External.Grouped").TargetFramework -ne ".NETStandard2.0") {
    throw "Expected the grouped dependency target framework in the manifest."
  }

  if (-not (Test-Path (Join-Path $outputDirectory "packages\Microsoft.CodeAnalysis.CSharp.5.0.0.nupkg"))) {
    throw "Expected the package source to contain the copied NuGet package."
  }
}
finally {
  Remove-Item -Path $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
