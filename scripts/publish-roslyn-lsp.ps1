<#
.SYNOPSIS
  Publishes roslyn-language-server packages to NuGet.org.

.DESCRIPTION
  Publishes release packages from the Release subdirectory of the given directory. When -Prerelease is specified,
  publishes prerelease packages from the given directory. Supports -WhatIf to validate packages without publishing
  them.

.EXAMPLE
  ./publish-roslyn-lsp.ps1 -PackageDirectory ./PackageArtifacts -WhatIf

.EXAMPLE
  ./publish-roslyn-lsp.ps1 -PackageDirectory ./PackageArtifacts -Prerelease -WhatIf
#>
[CmdletBinding(SupportsShouldProcess, PositionalBinding = $false)]
param (
  [Parameter(Mandatory = $true)][string]$PackageDirectory,
  [switch]$Prerelease,
  [string]$NuGetApiKey = $env:NUGET_API_KEY,
  [string]$NuGetSource = "https://www.nuget.org/api/v2/package"
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"

$resolvedPackageDirectory = if ($Prerelease) { $PackageDirectory } else { Join-Path $PackageDirectory "Release" }
$packageKind = if ($Prerelease) { "prerelease" } else { "release" }

if (-not (Test-Path -Path $resolvedPackageDirectory -PathType Container)) {
  Write-Error "Package directory '$resolvedPackageDirectory' does not exist."
  exit 1
}

Write-Host "Looking for $packageKind roslyn-language-server packages in '$resolvedPackageDirectory'"

# Only look at the root of the package directory. Symbol packages are published separately and are excluded here.
$packages = @(Get-ChildItem -Path $resolvedPackageDirectory -Filter "roslyn-language-server.*.nupkg" -File |
  Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
  Sort-Object -Property Name)

if ($packages.Count -eq 0) {
  Write-Host "Files found in '$resolvedPackageDirectory':"
  Get-ChildItem -Path $resolvedPackageDirectory -File | ForEach-Object { Write-Host "  $($_.Name)" }
  Write-Error "No roslyn-language-server packages were found in '$resolvedPackageDirectory'."
  exit 1
}

foreach ($package in $packages) {
  # Package file names have the form roslyn-language-server.[<rid>.]<version>.nupkg
  if ($package.Name -notmatch '^(?<id>roslyn-language-server)\.(?:(?<rid>[a-z0-9]+(?:-[a-z0-9]+)+)\.)?(?<version>.+)\.nupkg$') {
    Write-Error "Unable to parse the version from package '$($package.Name)'."
    exit 1
  }

  $packageId = $Matches['id'] + $Matches['rid']
  $packageVersion = $Matches['version']

  if ($Prerelease -and -not $packageVersion.Contains('-')) {
    Write-Error "Package '$($package.Name)' has version '$packageVersion' which is not a prerelease version. Only prerelease packages may be published by this script."
    exit 1
  }

  if (-not $Prerelease -and $packageVersion.Contains('-')) {
    Write-Error "Package '$($package.Name)' has version '$packageVersion' which is not a release version. Only release packages may be published by this script."
    exit 1
  }

  Write-Host "Validated package '$($package.Name)'"
  Write-Host "  Id: $packageId"
  Write-Host "  Version: $packageVersion"
  Write-Host "  Size: $($package.Length) bytes"
}

if ($WhatIfPreference) {
  Write-Host "Validation completed for $($packages.Count) package(s). No packages were published because -WhatIf was specified."
  exit 0
}

if (-not $NuGetApiKey) {
  Write-Error "A NuGet API key is required. Provide -NuGetApiKey or set the NUGET_API_KEY environment variable."
  exit 1
}

foreach ($package in $packages) {
  if (-not $PSCmdlet.ShouldProcess($package.Name, "Publish to $NuGetSource")) {
    continue
  }

  Write-Host "Publishing $($package.Name) to $NuGetSource"
  & dotnet nuget push $package.FullName --source $NuGetSource --api-key $NuGetApiKey --skip-duplicate
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish $($package.Name)."
    exit 1
  }
}

Write-Host "Successfully published $($packages.Count) package(s)."