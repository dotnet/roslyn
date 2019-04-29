# Publishes our build assets to nuget, myget, dotnet/versions, etc ..
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

  # Credentials 
  [string]$gitHubUserName = "",
  [string]$gitHubToken = "",
  [string]$gitHubEmail = "",
  [string]$nugetApiKey = "",
  [string]$myGetApiKey = ""
)
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-PublishKey([string]$uploadUrl) {
  $url = New-Object Uri $uploadUrl
  switch ($url.Host) {
    "dotnet.myget.org" { return $myGetApiKey }
    "api.nuget.org" { return $nugetApiKey }
    default { throw "Cannot determine publish key for $uploadUrl" }
  }
}

# Publish the NuGet packages to the specified URL
function Publish-NuGet([string]$packageDir, [string]$uploadUrl) {
  Push-Location $packageDir
  try {
    Write-Host "Publishing $(Split-Path -leaf $packageDir) to $uploadUrl"
    $apiKey = Get-PublishKey $uploadUrl
    foreach ($package in Get-ChildItem *.nupkg) {
      $nupkg = Split-Path -Leaf $package
      Write-Host "  Publishing $nupkg"
      if (-not (Test-Path $nupkg)) {
        throw "$nupkg does not exist"
      }

      if (-not $test) {
        Exec-Console $dotnet "nuget push $nupkg --source $uploadUrl --api-key $apiKey"
      }
    }
  } 
  finally {
    Pop-Location
  }
}

function Publish-Vsix([string]$uploadUrl) {
  Write-Host "Publishing VSIX to $uploadUrl"
  $apiKey = Get-PublishKey $uploadUrl
  $extensions = [xml](Get-Content (Join-Path $EngRoot "config\PublishVsix.MyGet.config"))
  foreach ($extension in $extensions.extensions.extension) {
    $vsix = Join-Path $VSSetupDir ($extension.id + ".vsix")
    if (-not (Test-Path $vsix)) {
      throw "VSIX $vsix does not exist"
    }
    
    Write-Host "  Publishing '$vsix'"
    if (-not $test) { 
      $response = Invoke-WebRequest -Uri $uploadUrl -Headers @{"X-NuGet-ApiKey"=$apiKey} -ContentType 'multipart/form-data' -InFile $vsix -Method Post -UseBasicParsing
      if ($response.StatusCode -ne 201) {
        throw "Failed to upload VSIX extension: $vsix. Upload failed with Status code: $response.StatusCode"
      }
    }
  }
}

function Publish-Channel([string]$packageDir, [string]$name) {
  $publish = GetProjectOutputBinary "RoslynPublish.exe"
  $args = "-nugetDir `"$packageDir`" -channel $name -gu $gitHubUserName -gt $gitHubToken -ge $githubEmail"
  Write-Host "Publishing $packageDir to channel $name"
  if (-not $test) { 
    Exec-Console $publish $args
  }
}

# Do basic verification on the values provided in the publish configuration
function Test-Entry($publishData, [switch]$isBranch) { 
  if ($isBranch) { 
    if ($publishData.nuget -ne $null) { 
      foreach ($nugetKind in $publishData.nugetKind) {
        if ($nugetKind -ne "PerBuildPreRelease" -and $nugetKind -ne "Shipping" -and $nugetKind -ne "NonShipping") {
                     throw "Branches are only allowed to publish Shipping, NonShipping, or PerBuildPreRelease"
        }
      }
    }
  }
}

# Publish a given entry: branch or release. 
function Publish-Entry($publishData, [switch]$isBranch) { 
  Test-Entry $publishData -isBranch:$isBranch

  # First publish the NuGet packages to the specified feeds
  foreach ($url in $publishData.nuget) {
    foreach ($nugetKind in $publishData.nugetKind) {
      Publish-NuGet (Join-Path $PackagesDir $nugetKind) $url
    }
  }

  # Next publish the VSIX to the specified feeds
  $vsixData = $publishData.vsix
  if ($vsixData -ne $null) { 
    Publish-Vsix $vsixData
  }

  # Finally get our channels uploaded to versions
  foreach ($channel in $publishData.channels) {
    foreach ($nugetKind in $publishData.nugetKind) {
      Publish-Channel (Join-Path $PackagesDir $nugetKind) $channel
    }
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
