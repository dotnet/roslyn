<#
  This script tests that Roslyn artifacts are rebuildable--i.e. that the source code and resources can be identified 
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Debug",
  [switch]$ci = $false,
  [switch]$noBuild = $false,
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-rebuild.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
  Write-Host "  -ci                       Set when running on CI server"
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
    Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -build -bootstrap -ci:$ci -configuration:$configuration -pack -binaryLog }
  }

  $dotnetInstallDir = (InitializeDotNetCli -install:$true)
  $rebuildArgs = ("--verbose" +
  " --assembliesPath `"$ArtifactsDir/obj/AnalyzerRunner`"" +
  " --assembliesPath `"$ArtifactsDir/obj/BuildBoss`"" +
  " --assembliesPath `"$ArtifactsDir/obj/CodeStyleConfigFileGenerator`"" +
  " --assembliesPath `"$ArtifactsDir/obj/csc`"" +
  " --assembliesPath `"$ArtifactsDir/obj/CSharpResultProvider.NetFX20`"" +
  " --assembliesPath `"$ArtifactsDir/obj/CSharpSyntaxGenerator`"" +
  " --assembliesPath `"$ArtifactsDir/obj/csi`"" +
  " --assembliesPath `"$ArtifactsDir/obj/IdeCoreBenchmarks`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.Build.Tasks.CodeAnalysis`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CodeStyle.Fixes`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CodeStyle.UnitTestUtilities`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CodeStyle`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Compiler.Test.Resources`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp.CodeStyle`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp.ExpressionCompiler`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp.Features`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp.Test.Utilities`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp.Workspaces`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.CSharp`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.EditorFeatures.Text`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.ExpressionCompiler`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.ExternalAccess.Debugger`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.ExternalAccess.Razor`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.FunctionResolver`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Remote.Razor.ServiceHub`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Remote.Workspaces`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.ResultProvider.Utilities`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.ResultProvider`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Scripting.TestUtilities`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Scripting`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Test.Utilities`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.TestSourceGenerator`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.VisualBasic`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Workspaces.Desktop`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Workspaces.MSBuild`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.Workspaces`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis.XunitHook`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.CodeAnalysis`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Microsoft.VisualStudio.IntegrationTest.IntegrationService`"" +
  " --assembliesPath `"$ArtifactsDir/obj/PrepareTests`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Roslyn.PerformanceTests`"" +
  " --assembliesPath `"$ArtifactsDir/obj/Roslyn.Test.Performance.Utilities`"" +
  " --assembliesPath `"$ArtifactsDir/obj/RoslynDeployment`"" +
  " --assembliesPath `"$ArtifactsDir/obj/RoslynPublish`"" +
  " --assembliesPath `"$ArtifactsDir/obj/RunTests`"" +
  " --assembliesPath `"$ArtifactsDir/obj/StackDepthTest`"" +
  " --assembliesPath `"$ArtifactsDir/obj/vbc`"" +
  " --assembliesPath `"$ArtifactsDir/obj/VBCSCompiler`"" +

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
