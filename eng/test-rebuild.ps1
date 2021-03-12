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

  $dotnetInstallDir = (InitializeDotNetCli -install:$true)
  $rebuildArgs = ("--verbose" +
  " --assembliesPath `"$ArtifactsDir/obj/`"" +

  # Configuration Issues
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Collections.Package.dll" +
  " --exclude net45\Microsoft.CodeAnalysis.Debugging.Package.dll" +
  " --exclude netstandard1.3\Microsoft.CodeAnalysis.Debugging.Package.dll" +
  " --exclude net45\Microsoft.CodeAnalysis.PooledObjects.Package.dll" +
  " --exclude netstandard1.3\Microsoft.CodeAnalysis.PooledObjects.Package.dll" +
  " --exclude net472\Zip\tools\vsixexpinstaller\System.ValueTuple.dll" +
  " --exclude net472\Zip\tools\vsixexpinstaller\VSIXExpInstaller.exe" +

  # Rebuild differences
  " --exclude netcoreapp3.1\BoundTreeGenerator.dll" +
  " --exclude net472\BuildActionTelemetryTable.exe" +
  " --exclude netcoreapp3.1\CSharpErrorFactsGenerator.dll" +
  " --exclude net472\IdeBenchmarks.exe" +
  " --exclude net5.0\InteractiveHost.UnitTests.dll" +
  " --exclude net472\InteractiveHost32.exe" +
  " --exclude net5.0-windows7.0\win10-x64\InteractiveHost64.dll" +
  " --exclude net472\win10-x64\InteractiveHost64.exe" +
  " --exclude net472\Microsoft.Build.Tasks.CodeAnalysis.UnitTests.dll" +
  " --exclude net5.0\Microsoft.Build.Tasks.CodeAnalysis.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.EditorFeatures.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.EditorFeatures2.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.IOperation.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.CSharp.IOperation.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Syntax.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.CSharp.Syntax.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.WinRT.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures.Wpf.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures2.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.ExpressionEvaluator.FunctionResolver.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Features.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.Features.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.LanguageServer.Protocol.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.Rebuild.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.Rebuild.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.Scripting.Desktop.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.UnitTests.dll" +
  " --exclude net5.0\Microsoft.CodeAnalysis.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.Workspaces.Desktop.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.Workspaces.UnitTests.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Workspaces.UnitTests.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.CSharp.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.Implementation.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.IntegrationTests.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests.dll" +
  " --exclude net472\Microsoft.VisualStudio.LanguageServices.VisualBasic.dll" +
  " --exclude net472\Roslyn.Hosting.Diagnostics.dll" +
  " --exclude net472\Roslyn.VisualStudio.DiagnosticsWindow.dll" +
  " --exclude net472\VBCSCompiler.UnitTests.dll" +
  " --exclude net5.0\VBCSCompiler.UnitTests.dll" +
  " --exclude netcoreapp3.1\VBErrorFactsGenerator.dll" +
  " --exclude netcoreapp3.1\VBSyntaxGenerator.dll" +

  # Compilation Errors
  " --exclude net5.0\IOperationGenerator.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.CSharp.Scripting.Desktop.UnitTests.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.CSharp.Scripting.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.CSharp.Scripting.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures.DiagnosticsTests.Utilities.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.EditorFeatures.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.EditorFeatures.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures.Test.Utilities.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.EditorFeatures.UnitTests.dll" +
  " --exclude netstandard2.0\Microsoft.CodeAnalysis.InteractiveHost.dll" +
  " --exclude net472\Microsoft.CodeAnalysis.Scripting.UnitTests.dll" +
  " --exclude netcoreapp3.1\Microsoft.CodeAnalysis.Scripting.UnitTests.dll" +
  " --exclude net472\Roslyn.VisualStudio.Next.UnitTests.dll" +

  " --debugPath `"$ArtifactsDir/BuildValidator`"" +
  " --sourcePath `"$RepoRoot`"" +
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
  Pop-Location
}
