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
  [string]$branchName = "",
  [string]$releaseName = "",
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
    "devdiv.pkgs.visualstudio.com" { return "DevDivAzureArtifacts" }
    "pkgs.dev.azure.com" { return "AzureArtifacts"}
    default { throw "Cannot determine publish key for $uploadUrl" }
  }
}

function Publish-Nuget($publishData, [string]$packageDir) {
  Push-Location $packageDir
  try {
    # Retrieve the feed name to source mapping.
    $feedData = GetFeedPublishData
    
    # The default feed we publish to for our official build is the VS feed.
    $feedName = "vs"

    if ($publishData.PSobject.Properties.Name -contains "feed") {
      $feedName = $publishData.feed
    }

    $packageOverrideData = GetPackageFeedOverrideData

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

      # Check if the specific package has a feed override.
      if (Get-Member -InputObject $packageOverrideData -Name $nupkgWithoutVersion) {
        $feedName = $packageOverrideData.$nupkgWithoutVersion
        Write-Host "Using package feed override $feedName for $nupkg"
      }

      # If we're doing PR validation, we always publish to the VS feed as it runs in the DevDiv AzDo instance
      # and does not have permissions to publish anywhere else.
      if ($prValidation) {
        $feedName = "vs"
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

# Publish a given entry: branch or release.
function Publish-Entry($publishData, [switch]$isBranch) {
  # First publish the NuGet packages to the specified feeds
  $nugetKinds = @('Shipping', 'NonShipping')
  foreach ($nugetKind in $nugetKinds) {
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

  if ($branchName -ne "" -and $releaseName -ne "") {
    Write-Host "Can only specify -branchName or -releaseName, not both"
    exit 1
  }

  if ($branchName -ne "") {
    $data = GetBranchPublishData $branchName
    if ($data -eq $null) {
      Write-Host "Branch $branchName not listed for publishing."
      exit 0
    }

    Publish-Entry $data -isBranch:$true
  }
  elseif ($releaseName -ne "") {
    $data = GetReleasePublishData $releaseName
    if ($data -eq $null) {
      Write-Host "Release $releaseName not listed for publishing."
      exit 1
    }

    Publish-Entry $data -isBranch:$false
  }
  else {
    Write-Host "Need to specify -branchName or -releaseName"
    exit 1
  }
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
