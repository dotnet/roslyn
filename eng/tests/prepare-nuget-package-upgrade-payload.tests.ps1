[CmdletBinding(PositionalBinding = $false)]
param()

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$scriptPath = Join-Path $repoRoot 'eng/prepare-nuget-package-upgrade-payload.ps1'

function New-TestPackage {
  param(
    [Parameter(Mandatory = $true)][string]$Directory,
    [Parameter(Mandatory = $true)][string]$Id,
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$FileName,
    [object[]]$Dependencies = @()
  )

  if ([string]::IsNullOrWhiteSpace($FileName)) {
    $FileName = "$Id.$Version.nupkg"
  }

  $contentDirectory = Join-Path $Directory ([System.Guid]::NewGuid().ToString('N'))
  [void](New-Item -ItemType Directory -Path $contentDirectory -Force)

  $nuspecPath = Join-Path $contentDirectory "$Id.nuspec"
  $dependencyXml = ''
  if ($Dependencies.Count -gt 0) {
    $dependencyLines = foreach ($dependency in $Dependencies) {
      "      <dependency id=`"$($dependency.Id)`" version=`"$($dependency.VersionRange)`" />"
    }

    $dependencyXml = "`n    <dependencies>`n$($dependencyLines -join "`n")`n    </dependencies>"
  }

  $nuspec = @"
<?xml version="1.0"?>
<package>
  <metadata>
    <id>$Id</id>
    <version>$Version</version>$dependencyXml
  </metadata>
</package>
"@

  Set-Content -LiteralPath $nuspecPath -Value $nuspec -Encoding utf8

  $packagePath = Join-Path $Directory $FileName
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::CreateFromDirectory($contentDirectory, $packagePath)
  Remove-Item -LiteralPath $contentDirectory -Recurse -Force
  return $packagePath
}

function New-TestLayout {
  param(
    [Parameter(Mandatory = $true)][string[]]$ManifestPackageIds,
    [Parameter(Mandatory = $true)][ScriptBlock]$PopulatePackages
  )

  $root = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString('N'))
  $shippingDirectory = Join-Path $root 'artifacts/packages/Release/Shipping'
  $outputDirectory = Join-Path $root 'artifacts/packageValidation/Release'
  [void](New-Item -ItemType Directory -Path $shippingDirectory -Force)

  $manifestPath = Join-Path $root 'eng/config/NuGetPackageUpgradePayload.json'
  [void](New-Item -ItemType Directory -Path (Split-Path -Parent $manifestPath) -Force)

  $manifest = [ordered]@{
    sourceOfTruth = 'test'
    packageIds = $ManifestPackageIds
  }

  $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath -Encoding utf8

  $null = & $PopulatePackages $shippingDirectory

  return [PSCustomObject]@{
    Root = $root
    ManifestPath = $manifestPath
    ShippingDirectory = $shippingDirectory
    OutputDirectory = $outputDirectory
  }
}

function Invoke-ExpectFailure {
  param(
    [Parameter(Mandatory = $true)][string]$Name,
    [Parameter(Mandatory = $true)][ScriptBlock]$Action,
    [Parameter(Mandatory = $true)][string]$ExpectedMessageContains
  )

  try {
    & $Action
    throw "[$Name] Expected failure but script succeeded."
  }
  catch {
    if (-not $_.ToString().Contains($ExpectedMessageContains)) {
      throw "[$Name] Expected message containing '$ExpectedMessageContains' but got: $($_.ToString())"
    }

    Write-Host "PASS: $Name"
  }
}

function Invoke-PrepareScript {
  param(
    [Parameter(Mandatory = $true)]$Layout
  )

  & $scriptPath `
    -Configuration Release `
    -RepositoryRoot $Layout.Root `
    -ManifestPath $Layout.ManifestPath `
    -PackagesDirectory $Layout.ShippingDirectory `
    -OutputDirectory $Layout.OutputDirectory
}

# Missing expected package
$missingLayout = New-TestLayout -ManifestPackageIds @(
  'Microsoft.Net.Compilers.Toolset',
  'Microsoft.CodeAnalysis',
  'Microsoft.CodeAnalysis.CSharp'
) -PopulatePackages {
  param($shipping)
  New-TestPackage -Directory $shipping -Id 'Microsoft.Net.Compilers.Toolset' -Version '1.0.0'
  New-TestPackage -Directory $shipping -Id 'Microsoft.CodeAnalysis' -Version '1.0.0'
}

Invoke-ExpectFailure -Name 'missing-expected-package' -ExpectedMessageContains 'Missing expected packages' -Action {
  Invoke-PrepareScript -Layout $missingLayout
}

# Duplicate identity
$duplicateLayout = New-TestLayout -ManifestPackageIds @(
  'Microsoft.Net.Compilers.Toolset',
  'Microsoft.CodeAnalysis'
) -PopulatePackages {
  param($shipping)
  New-TestPackage -Directory $shipping -Id 'Microsoft.Net.Compilers.Toolset' -Version '1.0.0'
  New-TestPackage -Directory $shipping -Id 'Microsoft.CodeAnalysis' -Version '1.0.0'
  New-TestPackage -Directory $shipping -Id 'Microsoft.CodeAnalysis' -Version '1.0.0' -FileName 'Microsoft.CodeAnalysis.1.0.0.copy.nupkg'
}

Invoke-ExpectFailure -Name 'duplicate-package-identity' -ExpectedMessageContains 'Duplicate package identity/version detected' -Action {
  Invoke-PrepareScript -Layout $duplicateLayout
}

# Version skew
$versionSkewLayout = New-TestLayout -ManifestPackageIds @(
  'Microsoft.Net.Compilers.Toolset',
  'Microsoft.CodeAnalysis'
) -PopulatePackages {
  param($shipping)
  New-TestPackage -Directory $shipping -Id 'Microsoft.Net.Compilers.Toolset' -Version '1.0.0'
  New-TestPackage -Directory $shipping -Id 'Microsoft.CodeAnalysis' -Version '1.0.1'
}

Invoke-ExpectFailure -Name 'version-skew' -ExpectedMessageContains 'Version skew detected' -Action {
  Invoke-PrepareScript -Layout $versionSkewLayout
}

# Undeclared Roslyn-family dependency
$undeclaredDependencyLayout = New-TestLayout -ManifestPackageIds @(
  'Microsoft.Net.Compilers.Toolset',
  'Microsoft.CodeAnalysis'
) -PopulatePackages {
  param($shipping)
  New-TestPackage -Directory $shipping -Id 'Microsoft.Net.Compilers.Toolset' -Version '1.0.0'
  New-TestPackage -Directory $shipping -Id 'Microsoft.CodeAnalysis' -Version '1.0.0' -Dependencies @(
    [PSCustomObject]@{ Id = 'Microsoft.CodeAnalysis.Workspaces.Common'; VersionRange = '[1.0.0]' }
  )
}

Invoke-ExpectFailure -Name 'undeclared-roslyn-family-dependency' -ExpectedMessageContains 'is not listed in' -Action {
  Invoke-PrepareScript -Layout $undeclaredDependencyLayout
}

Write-Host 'All prepare-nuget-package-upgrade-payload tests passed.'
