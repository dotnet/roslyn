<#
  This script tests that Roslyn artifacts are rebuildable--i.e. that the source code and resources can be identified
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Debug",
  [switch]$ci = $false,
  [switch]$useGlobalNuGetCache = $true,
  [switch]$noBuild = $false,
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-rebuild.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -useGlobalNuGetCache      Use global NuGet cache."
  Write-Host "  -noBuild                  If set, skips running a bootstrap build before running the rebuild"
  Write-Host "  -help                     Print help and exit"
}

try {
  if ($help) {
    Print-Usage
    exit 0
  }

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  if (-not $noBuild) {
    Write-Host "Building Roslyn"
    Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -build -bootstrap -ci:$ci -useGlobalNuGetCache:$useGlobalNuGetCache -configuration:$configuration -pack -binaryLog }
  }

  Subst-TempDir

  $dotnetInstallDir = (InitializeDotNetCli -install:$true)
  $rebuildArgs = ("--verbose" +
  " --assembliesPath `"$ArtifactsDir/obj/`"" +

# Rebuilds with output differences
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures.Wpf.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.CSharp.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.Implementation.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.VisualBasic.dll" +
  " --exclude net472\Roslyn.Hosting.Diagnostics.dll" +
  " --exclude net472\Roslyn.VisualStudio.DiagnosticsWindow.dll" +
# Rebuilds with compilation errors
# Rebuilds with missing references
# Rebuilds with other issues
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures2.UnitTests.dll" +
  " --exclude net6.0\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude net45\Microsoft.CodeAnalysis.Debugging.Package.dll" +
  " --exclude netstandard1.3\Microsoft.CodeAnalysis.Debugging.Package.dll" +
  " --exclude net45\Microsoft.CodeAnalysis.PooledObjects.Package.dll" +
  " --exclude netstandard1.3\Microsoft.CodeAnalysis.PooledObjects.Package.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Workspaces.UnitTests.dll" +
  " --exclude net472\Zip\tools\vsixexpinstaller\System.ValueTuple.dll" +
  " --exclude net472\Zip\tools\vsixexpinstaller\VSIXExpInstaller.exe" +

  " --debugPath `"$ArtifactsDir/BuildValidator`"" +
  " --sourcePath `"$RepoRoot/`"" +
  " --referencesPath `"$ArtifactsDir/bin`"" +
  " --referencesPath `"$dotnetInstallDir/packs`"")
  Exec-Console "$ArtifactsDir/bin/BuildValidator/$configuration/net472/BuildValidator.exe" $rebuildArgs

  exit 0
}
catch [exception] {
  Write-Host $_
  Write-Host $_.Exception
  exit 1
}
finally {
  Unsubst-TempDir
  Pop-Location
}
