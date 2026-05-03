<#
  This script tests that Roslyn artifacts are rebuildable--i.e. that the source code and resources can be identified
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Debug",
  [switch]$ci = $false,
  [switch]$prepareMachine = $false,
  [switch]$useGlobalNuGetCache = $true,
  [switch]$bootstrap = $false,
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-rebuild.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
  Write-Host "  -ci                       Set when running on CI server"
  Write-Host "  -useGlobalNuGetCache      Use global NuGet cache."
  Write-Host "  -bootstrap                Do a bootstrap build before running the build validatior"
  Write-Host "  -help                     Print help and exit"
}

try {
  if ($help) {
    Print-Usage
    exit 0
  }

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  if ($bootstrap) {
    Write-Host "Building Roslyn"
    & eng/build.ps1 -restore -build -bootstrap -prepareMachine:$prepareMachine -ci:$ci -useGlobalNuGetCache:$useGlobalNuGetCache -configuration:$configuration -pack -binaryLog /p:RoslynCompilerType=Framework
    Test-LastExitCode
  }

  Subst-TempDir

  $dotnetInstallDir = (InitializeDotNetCli -install:$true)
  $rebuildArgs = ("--verbose" +
  " --assembliesPath `"$ArtifactsDir/obj/`"" +

# Rebuilds with output differences
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.CSharp.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.Implementation.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.VisualBasic.dll" +
  " --exclude net472\Roslyn.Hosting.Diagnostics.dll" +
  " --exclude net472\Roslyn.VisualStudio.DiagnosticsWindow.dll" +

  # The merged Razor subtree currently rebuilds with known BuildValidator output differences.
  " --exclude dotnet-razorsyntaxgenerator.dll" +
  " --exclude Microsoft.AspNetCore.Mvc.Razor.Extensions.Tooling.Internal.dll" +
  " --exclude Microsoft.AspNetCore.Mvc.Razor.Extensions.UnitTests.dll" +
  " --exclude Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X.UnitTests.dll" +
  " --exclude Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X.UnitTests.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Language.Legacy.UnitTests.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Language.UnitTests.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Microbenchmarks.Compiler.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Microbenchmarks.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Microbenchmarks.exe" +
  " --exclude Microsoft.AspNetCore.Razor.Microbenchmarks.Generator.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Test.Common.Cohosting.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Test.Common.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Test.Common.Tooling.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Test.MvcShim.Version1_X.Compiler.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Test.MvcShim.Version2_X.Compiler.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Utilities.Shared.dll" +
  " --exclude Microsoft.AspNetCore.Razor.Utilities.Shared.UnitTests.dll" +
  " --exclude Microsoft.CodeAnalysis.Razor.Compiler.dll" +
  " --exclude Microsoft.CodeAnalysis.Razor.Tooling.Internal.dll" +
  " --exclude Microsoft.CodeAnalysis.Razor.UnitTests.dll" +
  " --exclude Microsoft.CodeAnalysis.Razor.Workspaces.dll" +
  " --exclude Microsoft.CodeAnalysis.Razor.Workspaces.UnitTests.dll" +
  " --exclude Microsoft.CodeAnalysis.Remote.Razor.CoreComponents.arm64.dll" +
  " --exclude Microsoft.CodeAnalysis.Remote.Razor.CoreComponents.x64.dll" +
  " --exclude Microsoft.CodeAnalysis.Remote.Razor.dll" +
  " --exclude Microsoft.CodeAnalysis.Remote.Razor.UnitTests.dll" +
  " --exclude Microsoft.Net.Compilers.Razor.Toolset.dll" +
  " --exclude Microsoft.NET.Sdk.Razor.SourceGenerators.UnitTests.dll" +
  " --exclude Microsoft.VisualStudio.LanguageServer.ContainedLanguage.dll" +
  " --exclude Microsoft.VisualStudio.LanguageServer.ContainedLanguage.UnitTests.dll" +
  " --exclude Microsoft.VisualStudio.LanguageServices.Razor.dll" +
  " --exclude Microsoft.VisualStudio.LanguageServices.Razor.UnitTests.dll" +
  " --exclude Microsoft.VisualStudio.Razor.IntegrationTests.dll" +
  " --exclude Microsoft.VisualStudio.RazorExtension.Dependencies.dll" +
  " --exclude Microsoft.VisualStudio.RazorExtension.dll" +
  " --exclude Microsoft.VisualStudioCode.Razor.IntegrationTests.dll" +
  " --exclude Microsoft.VisualStudioCode.RazorExtension.dll" +
  " --exclude Microsoft.VisualStudioCode.RazorExtension.UnitTests.dll" +
  " --exclude Razor.Diagnostics.Analyzers.dll" +
  " --exclude Razor.Diagnostics.Analyzers.UnitTests.dll" +
  " --exclude RazorDeployment.dll" +

# Rebuilds with compilation errors
# Rebuilds with missing references
# Rebuilds with other issues
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures2.UnitTests.dll" +
  " --exclude net10.0\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Contracts.Package.dll" +
  " --exclude net8.0\Microsoft.CodeAnalysis.Contracts.Package.dll" +
  " --exclude net10.0\Microsoft.CodeAnalysis.Contracts.Package.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Debugging.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.PooledObjects.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Threading.Package.dll" +
  " --exclude net8.0\Microsoft.CodeAnalysis.Threading.Package.dll" +
  " --exclude net10.0\Microsoft.CodeAnalysis.Threading.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Extensions.Package.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Workspaces.UnitTests.dll" +

  # Semantic Search reference assemblies can't be reconstructed from source.
  # The assemblies are not marked with ReferenceAssemblyAttribute attribute.
  " --exclude net10.0\GeneratedRefAssemblies\Microsoft.CodeAnalysis.dll" +
  " --exclude net10.0\GeneratedRefAssemblies\Microsoft.CodeAnalysis.CSharp.dll" +
  " --exclude net10.0\GeneratedRefAssemblies\Microsoft.CodeAnalysis.VisualBasic.dll" +
  " --exclude net10.0\GeneratedRefAssemblies\Microsoft.CodeAnalysis.SemanticSearch.Extensions.dll" +
  " --exclude net10.0\GeneratedRefAssemblies\System.Collections.Immutable.dll" +

  " --debugPath `"$ArtifactsDir/BuildValidator`"" +
  " --sourcePath `"$RepoRoot/`"" +
  " --referencesPath `"$ArtifactsDir/bin`"" +
  " --referencesPath `"$dotnetInstallDir/packs`"")
  Exec-Command "$ArtifactsDir/bin/BuildValidator/$configuration/net472/BuildValidator.exe" $rebuildArgs

  exit 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  Unsubst-TempDir
  Pop-Location
}
