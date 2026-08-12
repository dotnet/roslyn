<#
.SYNOPSIS
  Publishes prerelease roslyn-language-server packages to NuGet.org.

.DESCRIPTION
  Validates that every roslyn-language-server package in the given directory is a prerelease package and pushes it
  to the configured NuGet source. Supports -WhatIf so a run can validate the packages without publishing them.

.EXAMPLE
  ./publish-prerelease-roslyn-lsp.ps1 -PackageDirectory ./artifacts/packages/Release -WhatIf
#>
[CmdletBinding(SupportsShouldProcess, PositionalBinding = $false)]
param (
  [Parameter(Mandatory = $true)][string]$PackageDirectory,
  [string]$NuGetApiKey = $env:NUGET_API_KEY,
  [string]$NuGetSource = "https://www.nuget.org/api/v2/package"
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"

if (-not (Test-Path -Path $PackageDirectory -PathType Container)) {
  Write-Error "Package directory '$PackageDirectory' does not exist."
  exit 1
}

Write-Host "Looking for prerelease roslyn-language-server packages in '$PackageDirectory'"

# Only look at the root of the package directory. Symbol packages are published separately and are excluded here.
$packages = @(Get-ChildItem -Path $PackageDirectory -Filter "roslyn-language-server.*.nupkg" -File |
  Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
  Sort-Object -Property Name)

if ($packages.Count -eq 0) {
  Write-Host "Files found in '$PackageDirectory':"
  Get-ChildItem -Path $PackageDirectory -File | ForEach-Object { Write-Host "  $($_.Name)" }
  Write-Error "No roslyn-language-server packages were found in '$PackageDirectory'."
  exit 1
}

foreach ($package in $packages) {
  # Package file names have the form roslyn-language-server.<version>.nupkg
  if ($package.Name -notmatch '^(?<id>roslyn-language-server)\.(?<version>.+)\.nupkg$') {
    Write-Error "Unable to parse the version from package '$($package.Name)'."
    exit 1
  }

  $packageId = $Matches['id']
  $packageVersion = $Matches['version']

  # A prerelease version always contains a hyphen, e.g. 5.11.0-1.26412.5
  if (-not $packageVersion.Contains('-')) {
    Write-Error "Package '$($package.Name)' has version '$packageVersion' which is not a prerelease version. Only prerelease packages may be published by this script."
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
