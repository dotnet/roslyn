Param(
  [string] $locateVsApiVersion = "0.2.0-beta"
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

function Create-Directory([string[]] $path) {
  if (!(Test-Path -path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function Locate-LocateVsApi {
  $packagesPath = Locate-PackagesPath
  $locateVsApi = Join-Path -path $packagesPath -ChildPath "RoslynTools.Microsoft.LocateVS\$locateVsApiVersion\lib\net46\LocateVS.dll"

  if (!(Test-Path -path $locateVsApi)) {
    throw "The specified LocateVS API version ($locateVsApiVersion) could not be located."
  }

  return Resolve-Path -path $locateVsApi
}

function Locate-PackagesPath {
  if ($env:NUGET_PACKAGES -eq $null) {
    $env:NUGET_PACKAGES =  Join-Path -path $env:UserProfile -childPath ".nuget\packages\"
  }

  $packagesPath = $env:NUGET_PACKAGES

  Create-Directory -path $packagesPath
  return Resolve-Path -path $packagesPath
}

try
{
  $locateVsApi = Locate-LocateVsApi
  $requiredPackageIds = @()

  $requiredPackageIds += "Microsoft.Component.MSBuild"
  $requiredPackageIds += "Microsoft.Net.Component.4.6.TargetingPack"
  $requiredPackageIds += "Microsoft.VisualStudio.Component.PortableLibrary"
  $requiredPackageIds += "Microsoft.VisualStudio.Component.Roslyn.Compiler"
  $requiredPackageIds += "Microsoft.VisualStudio.Component.VSSDK"

  Add-Type -path $locateVsApi
  $visualStudioInstallationPath = [LocateVS.Instance]::GetInstallPath("15.0", $requiredPackageIds)

  return Join-Path -Path $visualStudioInstallationPath -ChildPath "Common7\Tools\"
}
catch
{
  # Return an empty string and let the caller fallback or handle this as appropriate
  return ""
}
