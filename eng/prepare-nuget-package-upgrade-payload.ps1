[CmdletBinding(PositionalBinding = $false)]
param(
  [Parameter(Mandatory = $true)]
  [string]$PackageDirectory,

  [Parameter(Mandatory = $true)]
  [string]$OutputDirectory
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Get-NuspecDocument([string]$packagePath) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem

  $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
  try {
    $nuspecEntry = $archive.Entries | Where-Object { $_.FullName.EndsWith(".nuspec", [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
    if ($null -eq $nuspecEntry) {
      throw "Package '$packagePath' does not contain a NuSpec file."
    }

    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try {
      [xml]$nuspec = $reader.ReadToEnd()
      return $nuspec
    }
    finally {
      $reader.Dispose()
    }
  }
  finally {
    $archive.Dispose()
  }
}

function Get-DependencyVersion([System.Xml.XmlElement]$dependency) {
  $version = $dependency.GetAttribute("version")
  if ([string]::IsNullOrEmpty($version)) {
    return $null
  }

  return $version
}

function Test-VersionRangeContains([string]$versionRange, [string]$version) {
  if ([string]::IsNullOrEmpty($versionRange) -or $versionRange -eq "*") {
    return $true
  }

  if ($versionRange[0] -ne '[' -and $versionRange[0] -ne '(') {
    return $versionRange -eq $version
  }

  if ($versionRange[-1] -ne ']' -and $versionRange[-1] -ne ')') {
    return $false
  }

  $bounds = $versionRange.Substring(1, $versionRange.Length - 2).Split(',', 2)
  if ($bounds.Count -eq 1) {
    return $versionRange[0] -eq '[' -and $versionRange[-1] -eq ']' -and $bounds[0] -eq $version
  }

  $parseVersion = {
    param([string]$value)

    $match = [regex]::Match($value, '^\d+(?:\.\d+){0,3}')
    if (-not $match.Success) {
      throw "Unsupported NuGet version '$value'."
    }

    return [version]$match.Value
  }

  $candidateVersion = & $parseVersion $version
  if (-not [string]::IsNullOrEmpty($bounds[0])) {
    $minimumVersion = & $parseVersion $bounds[0]
    if ($candidateVersion -lt $minimumVersion -or ($candidateVersion -eq $minimumVersion -and $versionRange[0] -eq '(')) {
      return $false
    }
  }

  if (-not [string]::IsNullOrEmpty($bounds[1])) {
    $maximumVersion = & $parseVersion $bounds[1]
    if ($candidateVersion -gt $maximumVersion -or ($candidateVersion -eq $maximumVersion -and $versionRange[-1] -eq ')')) {
      return $false
    }
  }

  return $true
}

if (-not (Test-Path -Path $PackageDirectory -PathType Container)) {
  throw "Package directory '$PackageDirectory' does not exist."
}

$packages = @(Get-ChildItem -Path $PackageDirectory -Filter "*.nupkg" -File | Where-Object { $_.Name -notlike "*.symbols.nupkg" } | Sort-Object Name)
if ($packages.Count -eq 0) {
  throw "Package directory '$PackageDirectory' does not contain any NuGet packages."
}

$packageMetadata = foreach ($package in $packages) {
  $nuspec = Get-NuspecDocument $package.FullName
  $metadata = $nuspec.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
  if ($null -eq $metadata) {
    throw "NuSpec in '$($package.FullName)' does not contain package metadata."
  }

  $id = $metadata.SelectSingleNode('./*[local-name()="id"]').InnerText
  $version = $metadata.SelectSingleNode('./*[local-name()="version"]').InnerText
  if ([string]::IsNullOrEmpty($id) -or [string]::IsNullOrEmpty($version)) {
    throw "NuSpec in '$($package.FullName)' must contain an id and version."
  }

  [pscustomobject]@{
    Id = $id
    Version = $version
    Package = $package
    NuSpec = $nuspec
  }
}

$roslynPackages = @{}
foreach ($package in $packageMetadata) {
  if ($package.Id.StartsWith("Microsoft.CodeAnalysis.", [System.StringComparison]::OrdinalIgnoreCase)) {
    $roslynPackages[$package.Id] = $package.Version
  }
}

$payloadPackageDirectory = Join-Path $OutputDirectory "packages"
New-Item -Path $payloadPackageDirectory -ItemType Directory -Force | Out-Null

$manifestPackages = foreach ($package in $packageMetadata) {
  $dependencies = @()
  foreach ($dependency in @($package.NuSpec.SelectNodes('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="dependencies"]/*[local-name()="dependency"]'))) {
    $dependencies += [pscustomobject]@{
      Id = $dependency.GetAttribute("id")
      Version = Get-DependencyVersion $dependency
      TargetFramework = $null
    }
  }

  foreach ($group in @($package.NuSpec.SelectNodes('/*[local-name()="package"]/*[local-name()="metadata"]/*[local-name()="dependencies"]/*[local-name()="group"]'))) {
    foreach ($dependency in @($group.SelectNodes('./*[local-name()="dependency"]'))) {
      $dependencies += [pscustomobject]@{
        Id = $dependency.GetAttribute("id")
        Version = Get-DependencyVersion $dependency
        TargetFramework = $group.GetAttribute("targetFramework")
      }
    }
  }

  foreach ($dependency in $dependencies | Where-Object { $_.Id.StartsWith("Microsoft.CodeAnalysis.", [System.StringComparison]::OrdinalIgnoreCase) }) {
    if (-not $roslynPackages.ContainsKey($dependency.Id)) {
      throw "Package '$($package.Id)' depends on Roslyn package '$($dependency.Id)', which is missing from the payload."
    }

    if (-not (Test-VersionRangeContains $dependency.Version $roslynPackages[$dependency.Id])) {
      throw "Package '$($package.Id)' depends on '$($dependency.Id) $($dependency.Version)', which does not accept payload version '$($roslynPackages[$dependency.Id])'."
    }
  }

  Copy-Item -Path $package.Package.FullName -Destination $payloadPackageDirectory -Force
  [pscustomobject]@{
    Id = $package.Id
    Version = $package.Version
    FileName = $package.Package.Name
    Sha256 = (Get-FileHash -Path $package.Package.FullName -Algorithm SHA256).Hash
    ExternalDependencies = @($dependencies | Where-Object { -not $_.Id.StartsWith("Microsoft.CodeAnalysis.", [System.StringComparison]::OrdinalIgnoreCase) } | Sort-Object Id, TargetFramework)
  }
}

[pscustomobject]@{
  Packages = @($manifestPackages | Sort-Object Id)
} | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $OutputDirectory "package-manifest.json") -Encoding UTF8
