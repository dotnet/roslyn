# Publishes our build assets to nuget, Azure DevOps, dotnet/versions, etc ..
#
# The publish operation is best visioned as an optional yet repeatable post build operation. It can be
# run anytime after build or automatically as a post build step. But it is an operation that focuses on
# build outputs and hence can't rely on source code from the build being available
#
# Repeatable is important here because we have to assume that publishes can and will fail with some
# degree of regularity.
[CmdletBinding(PositionalBinding=$false)]
Param(
  # Standard options
  [string]$configuration = "",
  [switch]$test,
  [switch]$prValidation,

  # Credentials
  [string]$nugetApiKey = ""
)
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-PublishKey([string]$uploadUrl) {
  $url = New-Object Uri $uploadUrl
  switch ($url.Host) {
    "api.nuget.org" { return $nugetApiKey }
    # For publishing to azure, the API key can be any non-empty string as authentication is done in the pipeline.
    "pkgs.dev.azure.com" { return "AzureArtifacts"}
    default { throw "Cannot determine publish key for $uploadUrl" }
  }
}

function Publish-Nuget($publishData, [string]$packageDir) {
  Push-Location $packageDir
  try {
    # Retrieve the feed name to source mapping.
    $feedData = GetFeedPublishData
    
    # Each branch stores the name of the package to feed map it should use.
    # Retrieve the correct map for this particular branch.
    $packagesData = GetPackagesPublishData

    foreach ($package in Get-ChildItem *.nupkg) {
      Write-Host ""

      $nupkg = Split-Path -Leaf $package
      Write-Host "Publishing $nupkg"
      if (-not (Test-Path $nupkg)) {
        throw "$nupkg does not exist"
      }

      if ($nupkg.EndsWith(".symbols.nupkg")) {
        Write-Host "Skipping symbol package $nupkg"
        continue
      }

      $nupkgWithoutVersion = $nupkg -replace '(\.\d+){3}-.*.nupkg', ''
      if ($nupkgWithoutVersion.EndsWith(".Symbols")) {
        Write-Host "Skipping symbol package $nupkg"
        continue
      }

      # Lookup the feed name from the packages map using the package name without the version or extension.
      if (-not (Get-Member -InputObject $packagesData -Name $nupkgWithoutVersion)) {
        throw "$nupkg has no configured feed (looked for $nupkgWithoutVersion)"
      }

      $feedName = $packagesData.$nupkgWithoutVersion

      if ($prValidation) {
        $feedName = "vs"
      }

      # If the configured feed is arcade, then skip publishing here.  Arcade will handle publishing to their feeds.
      if ($feedName.equals("arcade")) {
        Write-Host "Skipping publishing for $nupkg as it is published by arcade"
        continue
      }

      # Use the feed name to get the source to upload the package to.
      if (-not (Get-Member -InputObject $feedData -Name $feedName)) {
        throw "$feedName has no configured source feed"
      }

      $uploadUrl = $feedData.$feedName
      $apiKey = Get-PublishKey $uploadUrl

      if (-not $test) {
        Write-Host "Publishing $nupkg"
        Exec-DotNet "nuget push $nupkg --source $uploadUrl --api-key $apiKey"
      }
    }
  }
  finally {
    Pop-Location
  }
}

# Do basic verification on the values provided in the publish configuration
function Test-Entry($publishData) {
  foreach ($nugetKind in $publishData.nugetKind) {
    if ($nugetKind -ne "PerBuildPreRelease" -and $nugetKind -ne "Shipping" -and $nugetKind -ne "NonShipping") {
                  throw "Branches are only allowed to publish Shipping, NonShipping, or PerBuildPreRelease"
    }
  }
}

# Publish a given entry: branch or release.
function Publish-Entry($publishData) {
  Test-Entry $publishData

  # First publish the NuGet packages to the specified feeds
  foreach ($nugetKind in $publishData.nugetKind) {
    Publish-NuGet $publishData (Join-Path $PackagesDir $nugetKind)
  }

  exit 0
}

try {
  if ($configuration -eq "") {
    Write-Host "Must provide the build configuration with -configuration"
    exit 1
  }

  . (Join-Path $PSScriptRoot "build-utils.ps1")

  $dotnet = Ensure-DotnetSdk

  $data = GetBranchPublishData

  Publish-Entry $data
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
