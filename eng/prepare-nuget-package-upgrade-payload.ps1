[CmdletBinding(PositionalBinding = $false)]
param(
  [string]$Configuration = 'Release',
  [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
  [string]$ManifestPath,
  [string]$PackagesDirectory,
  [string]$OutputDirectory
)

# Validates and stages the local Roslyn NuGet package upgrade payload used by
# non-DartLab Release integration pipelines. Keep package IDs synchronized with
# eng/config/NuGetPackageUpgradePayload.json (which mirrors roslyn-tools publish IDs).
# Local validator test command:
#   pwsh eng/tests/prepare-nuget-package-upgrade-payload.tests.ps1

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
  $ManifestPath = Join-Path $RepositoryRoot 'eng/config/NuGetPackageUpgradePayload.json'
}

if ([string]::IsNullOrWhiteSpace($PackagesDirectory)) {
  $PackagesDirectory = Join-Path $RepositoryRoot "artifacts/packages/$Configuration/Shipping"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
  $OutputDirectory = Join-Path $RepositoryRoot "artifacts/packageValidation/$Configuration"
}

function New-ActionableError([string]$Message) {
  throw "[prepare-nuget-package-upgrade-payload] $Message"
}

function Get-SemanticVersion([Parameter(Mandatory = $true)][string]$VersionText) {
  try {
    return [System.Management.Automation.SemanticVersion]::Parse($VersionText)
  }
  catch {
    New-ActionableError "Invalid semantic version '$VersionText'."
  }
}

function Test-VersionSatisfiesRange(
  [Parameter(Mandatory = $true)][string]$Version,
  [string]$Range
) {
  if ([string]::IsNullOrWhiteSpace($Range)) {
    return $true
  }

  $candidate = Get-SemanticVersion -VersionText $Version
  $rangeText = $Range.Trim()

  if ($rangeText.StartsWith('[') -or $rangeText.StartsWith('(')) {
    if (-not ($rangeText.EndsWith(']') -or $rangeText.EndsWith(')'))) {
      New-ActionableError "Malformed version range '$Range'."
    }

    $lowerInclusive = $rangeText.StartsWith('[')
    $upperInclusive = $rangeText.EndsWith(']')
    $inner = $rangeText.Substring(1, $rangeText.Length - 2)

    if ($inner.IndexOf(',') -lt 0) {
      $exact = Get-SemanticVersion -VersionText $inner.Trim()
      return $candidate -eq $exact
    }

    $parts = $inner.Split(',', 2)
    $lowerText = $parts[0].Trim()
    $upperText = $parts[1].Trim()

    if (-not [string]::IsNullOrWhiteSpace($lowerText)) {
      $lower = Get-SemanticVersion -VersionText $lowerText
      if ($candidate -lt $lower -or ($candidate -eq $lower -and -not $lowerInclusive)) {
        return $false
      }
    }

    if (-not [string]::IsNullOrWhiteSpace($upperText)) {
      $upper = Get-SemanticVersion -VersionText $upperText
      if ($candidate -gt $upper -or ($candidate -eq $upper -and -not $upperInclusive)) {
        return $false
      }
    }

    return $true
  }

  # NuGet floating/version shorthand: treat as minimum inclusive.
  $minimum = Get-SemanticVersion -VersionText $rangeText
  return $candidate -ge $minimum
}

function Test-IsRoslynFamilyDependency([Parameter(Mandatory = $true)][string]$PackageId) {
  return $PackageId -eq 'Microsoft.CodeAnalysis' -or
    $PackageId.StartsWith('Microsoft.CodeAnalysis.', [System.StringComparison]::Ordinal) -or
    $PackageId.StartsWith('Microsoft.Net.Compilers.', [System.StringComparison]::Ordinal)
}

function ConvertTo-DependencyRecord($DependencyNode, [string]$TargetFramework) {
  $id = $DependencyNode.id
  if ([string]::IsNullOrWhiteSpace($id)) {
    New-ActionableError 'Encountered a dependency node without an id attribute.'
  }

  return [PSCustomObject]@{
    Id = $id.Trim()
    VersionRange = ($DependencyNode.version ?? '').Trim()
    TargetFramework = ($TargetFramework ?? '').Trim()
  }
}

function Get-PackageMetadata([Parameter(Mandatory = $true)][string]$PackagePath) {
  $fileName = [System.IO.Path]::GetFileName($PackagePath)
  if ($fileName.EndsWith('.symbols.nupkg', [System.StringComparison]::OrdinalIgnoreCase)) {
    return $null
  }

  $zip = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
  try {
    $nuspecEntries = @($zip.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [System.StringComparison]::OrdinalIgnoreCase) })
    if ($nuspecEntries.Count -ne 1) {
      New-ActionableError "Package '$PackagePath' is malformed: expected exactly one .nuspec entry but found $($nuspecEntries.Count)."
    }

    $entry = $nuspecEntries[0]
    $stream = $entry.Open()
    try {
      $reader = New-Object System.IO.StreamReader($stream)
      try {
        $nuspecText = $reader.ReadToEnd()
      }
      finally {
        $reader.Dispose()
      }
    }
    finally {
      $stream.Dispose()
    }

    [xml]$nuspec = $nuspecText
    $metadata = $nuspec.package.metadata
    if ($null -eq $metadata) {
      New-ActionableError "Package '$PackagePath' is malformed: nuspec is missing package metadata."
    }

    $packageId = ($metadata.id ?? '').Trim()
    $version = ($metadata.version ?? '').Trim()

    if ([string]::IsNullOrWhiteSpace($packageId) -or [string]::IsNullOrWhiteSpace($version)) {
      New-ActionableError "Package '$PackagePath' is malformed: nuspec id/version is missing."
    }

    $dependencies = [System.Collections.Generic.List[object]]::new()
    foreach ($dependency in @($nuspec.SelectNodes('/package/metadata/dependencies/dependency'))) {
      $dependencies.Add((ConvertTo-DependencyRecord -DependencyNode $dependency -TargetFramework ''))
    }

    foreach ($group in @($nuspec.SelectNodes('/package/metadata/dependencies/group'))) {
      $targetFramework = ($group.targetFramework ?? '').Trim()
      foreach ($dependency in @($group.SelectNodes('./dependency'))) {
        $dependencies.Add((ConvertTo-DependencyRecord -DependencyNode $dependency -TargetFramework $targetFramework))
      }
    }

    return [PSCustomObject]@{
      PackagePath = $PackagePath
      FileName = $fileName
      PackageId = $packageId
      Version = $version
      Dependencies = @($dependencies)
    }
  }
  finally {
    $zip.Dispose()
  }
}

function ConvertTo-OrderedDependency([Parameter(Mandatory = $true)]$Dependency) {
  return [ordered]@{
    id = $Dependency.Id
    versionRange = $Dependency.VersionRange
    targetFramework = $Dependency.TargetFramework
  }
}

function Get-ManifestPackageIds([Parameter(Mandatory = $true)][string]$Path) {
  if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    New-ActionableError "Manifest file '$Path' does not exist."
  }

  $manifest = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
  $ids = @($manifest.packageIds)
  if ($ids.Count -eq 0) {
    New-ActionableError "Manifest file '$Path' did not contain any packageIds."
  }

  $duplicates = $ids | Group-Object | Where-Object Count -gt 1
  if ($duplicates) {
    $duplicateIds = ($duplicates | ForEach-Object Name) -join ', '
    New-ActionableError "Manifest file '$Path' contains duplicate package IDs: $duplicateIds"
  }

  return $ids
}

function Assert-PackageFileNameMatchesIdentity([Parameter(Mandatory = $true)]$Package) {
  $expectedPrefix = "$($Package.PackageId).$($Package.Version)"
  if (-not $Package.FileName.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    New-ActionableError "Package '$($Package.PackagePath)' has nuspec identity '$($Package.PackageId) $($Package.Version)' but an ambiguous filename '$($Package.FileName)'."
  }
}

try {
  $expectedPackageIds = Get-ManifestPackageIds -Path $ManifestPath
  $expectedPackageIdSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
  foreach ($id in $expectedPackageIds) {
    [void]$expectedPackageIdSet.Add($id)
  }

  if (-not (Test-Path -LiteralPath $PackagesDirectory -PathType Container)) {
    New-ActionableError "Shipping package directory '$PackagesDirectory' does not exist. Run the Release pack build first."
  }

  $shippingPackages = Get-ChildItem -LiteralPath $PackagesDirectory -Filter '*.nupkg' -File | Sort-Object Name
  if ($shippingPackages.Count -eq 0) {
    New-ActionableError "No .nupkg files were found under '$PackagesDirectory'."
  }

  $allPackages = [System.Collections.Generic.List[object]]::new()
  foreach ($nupkg in $shippingPackages) {
    $metadata = Get-PackageMetadata -PackagePath $nupkg.FullName
    if ($null -ne $metadata) {
      Assert-PackageFileNameMatchesIdentity -Package $metadata
      $allPackages.Add($metadata)
    }
  }

  $duplicateIdentities = $allPackages |
    Group-Object PackageId, Version |
    Where-Object Count -gt 1

  if ($duplicateIdentities) {
    $details = $duplicateIdentities | ForEach-Object {
      $identity = $_.Group[0]
      "$($identity.PackageId) $($identity.Version): $((($_.Group | ForEach-Object FileName) -join ', '))"
    }

    New-ActionableError ("Duplicate package identity/version detected in shipping output: {0}" -f ($details -join '; '))
  }

  $toolsetCandidates = @($allPackages | Where-Object PackageId -eq 'Microsoft.Net.Compilers.Toolset')
  if ($toolsetCandidates.Count -ne 1) {
    $versions = if ($toolsetCandidates.Count -gt 0) { ($toolsetCandidates | ForEach-Object Version) -join ', ' } else { '<none>' }
    New-ActionableError "Expected exactly one Microsoft.Net.Compilers.Toolset package to establish candidate version, but found $($toolsetCandidates.Count) ($versions)."
  }

  $candidateVersion = $toolsetCandidates[0].Version

  $missingPackages = [System.Collections.Generic.List[string]]::new()
  $versionSkewPackages = [System.Collections.Generic.List[string]]::new()
  $selectedPackages = [System.Collections.Generic.List[object]]::new()

  foreach ($expectedId in $expectedPackageIds) {
    $idMatches = @($allPackages | Where-Object PackageId -eq $expectedId)

    if ($idMatches.Count -eq 0) {
      $missingPackages.Add($expectedId)
      continue
    }

    $versionMatches = @($idMatches | Where-Object Version -eq $candidateVersion)
    if ($versionMatches.Count -ne 1) {
      $versions = ($idMatches | ForEach-Object Version | Sort-Object -Unique) -join ', '
      $versionSkewPackages.Add("$expectedId => versions: $versions")
      continue
    }

    $selectedPackages.Add($versionMatches[0])
  }

  if ($missingPackages.Count -gt 0) {
    New-ActionableError "Missing expected packages at candidate version '$candidateVersion': $($missingPackages -join ', ')"
  }

  if ($versionSkewPackages.Count -gt 0) {
    New-ActionableError "Expected exactly one package at candidate version '$candidateVersion' for each expected package ID. Version skew detected: $($versionSkewPackages -join '; ')"
  }

  $externalDependenciesByKey = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)

  foreach ($package in $selectedPackages) {
    foreach ($dependency in $package.Dependencies) {
      if (Test-IsRoslynFamilyDependency -PackageId $dependency.Id) {
        if (-not $expectedPackageIdSet.Contains($dependency.Id)) {
          New-ActionableError "Package '$($package.PackageId)' declares Roslyn-family dependency '$($dependency.Id)' that is not listed in '$ManifestPath'."
        }

        if (-not (Test-VersionSatisfiesRange -Version $candidateVersion -Range $dependency.VersionRange)) {
          New-ActionableError "Package '$($package.PackageId)' dependency '$($dependency.Id) $($dependency.VersionRange)' is not satisfied by candidate version '$candidateVersion'."
        }
      }
      else {
        $key = "$($dependency.Id)|$($dependency.VersionRange)|$($dependency.TargetFramework)"
        if (-not $externalDependenciesByKey.ContainsKey($key)) {
          $externalDependenciesByKey[$key] = $dependency
        }
      }
    }
  }

  if (Test-Path -LiteralPath $OutputDirectory) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
  }

  $stagingPackagesDirectory = Join-Path $OutputDirectory 'packages'
  [void](New-Item -ItemType Directory -Path $stagingPackagesDirectory -Force)

  $orderedPackages = @($selectedPackages | Sort-Object PackageId)
  foreach ($package in $orderedPackages) {
    Copy-Item -LiteralPath $package.PackagePath -Destination (Join-Path $stagingPackagesDirectory $package.FileName) -Force
  }

  $manifestPackages = foreach ($package in $orderedPackages) {
    $stagedPath = Join-Path $stagingPackagesDirectory $package.FileName
    $hash = (Get-FileHash -LiteralPath $stagedPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $relativeStagedPath = Join-Path 'packages' $package.FileName

    $orderedDependencies = @($package.Dependencies |
      Sort-Object Id, TargetFramework, VersionRange |
      ForEach-Object { ConvertTo-OrderedDependency -Dependency $_ })

    [ordered]@{
      id = $package.PackageId
      version = $package.Version
      path = $relativeStagedPath -replace '\\', '/'
      size = (Get-Item -LiteralPath $stagedPath).Length
      sha256 = $hash
      dependencies = $orderedDependencies
    }
  }

  $externalDependencies = @($externalDependenciesByKey.Values |
    Sort-Object Id, TargetFramework, VersionRange |
    ForEach-Object { ConvertTo-OrderedDependency -Dependency $_ })

  $outputManifest = [ordered]@{
    candidateVersion = $candidateVersion
    packageCount = $manifestPackages.Count
    packages = $manifestPackages
    externalDependencies = $externalDependencies
  }

  [void](New-Item -ItemType Directory -Path $OutputDirectory -Force)

  $packageManifestPath = Join-Path $OutputDirectory 'package-manifest.json'
  $outputManifest | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $packageManifestPath -Encoding utf8

  Write-Host "Prepared NuGet upgrade payload: $($manifestPackages.Count) package(s) at version '$candidateVersion'."
  Write-Host "Staged packages: $stagingPackagesDirectory"
  Write-Host "Manifest: $packageManifestPath"
}
catch {
  Write-Error $_
  exit 1
}
