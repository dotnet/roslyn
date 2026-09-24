# Make a bootstrap compiler and install it into artifacts/bootstrap folder

[CmdletBinding(PositionalBinding=$true)]
param (
  [string]$output = "",
  [string]$toolset = "Default",
  [string]$configuration = "Release",
  [switch]$force = $false,
  [switch]$ci = $false,
  # Consumed implicitly by the MSBuild helper.
  [switch]$warnAsError = $false
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Bootstrap uses dotnet MSBuild without changing the caller's cached build engine.
$previousBuildTool = if (Test-Path variable:global:_BuildTool) { $global:_BuildTool } else { $null }
if ($null -ne $previousBuildTool) {
  Remove-Item variable:global:_BuildTool
}

try {
  $msbuildEngine = "dotnet"
  # Do not reuse nodes that may have loaded a different C#/VB toolset.
  # https://github.com/dotnet/roslyn/issues/6211
  $nodeReuse = $false
  $binaryLog = $true
  $disablePipelineSetResult = $true
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  $prepareMachine = $ci

  if ($output -eq "") {
    $output = Join-Path $ArtifactsDir "bootstrap" "local"
  }

  Write-Host "Building bootstrap compiler into $output"

  if (Test-Path $output) {
    if ($force) {
      Write-Host "Removing existing bootstrap compiler"
      Remove-Item -Recurse -Force $output
    }
    else {
      Write-Host "Bootstrap compiler already exists. Use -force to rebuild"
      exit 1
    }
  }

  if ($toolset -ieq "Default") {
    $projectPath = "src\NuGet\Microsoft.Net.Compilers.Toolset\AnyCpu\Microsoft.Net.Compilers.Toolset.Package.csproj"
    $packageName = "Microsoft.Net.Compilers.Toolset"
  }
  elseif ($toolset -ieq "Framework") {
    $projectPath = "src\NuGet\Microsoft.Net.Compilers.Toolset\Framework\Microsoft.Net.Compilers.Toolset.Framework.Package.csproj"
    $packageName = "Microsoft.Net.Compilers.Toolset.Framework"
  }
  else {
    throw "Unsupported bootstrap toolset $toolset"
  }

  $projectPath = Join-Path $RepoRoot $projectPath

  $name = Split-Path -Leaf $output
  $binaryLogFilePath = Join-Path $LogDir "bootstrap-$($name).binlog"

  if ($ci) {
    # Set NUGET_PACKAGES to fix issues with package Restore when building with `-ci`.
    # Workaround for https://github.com/dotnet/arcade/issues/15970
    $env:NUGET_PACKAGES = Join-Path $RepoRoot '.packages\'
    $env:RESTORENOCACHE = $true
  }

  # Use Arcade's MSBuild helper for correct warnAsError/warnNotAsError behavior.
  MSBuild $projectPath `
    /restore `
    /t:Pack `
    /p:Configuration=$configuration `
    /p:RunAnalyzersDuringBuild=false `
    /p:DotNetUseShippingVersions=true `
    /p:InitialDefineConstants=BOOTSTRAP `
    /p:PackageOutputPath=$output `
    /p:NgenOptimization=false `
    /p:PublishWindowsPdb=false `
    /bl:$binaryLogFilePath

  $packageFilePath = Get-ChildItem -Path $output -Filter "$packageName.*.nupkg"
  Write-Host "Found package $packageFilePath"
  Unzip $packageFilePath.FullName $output

  Write-Host "Cleaning up artifacts"
  MSBuild $projectPath /t:Clean "/bl:$(Join-Path $LogDir "bootstrap-$name-clean.binlog")"
  Exec-DotNet "build-server shutdown"

  ExitWithExitCode 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  ExitWithExitCode 1
}
finally {
  if ($null -ne $previousBuildTool) {
    $global:_BuildTool = $previousBuildTool
  }
  elseif (Test-Path variable:global:_BuildTool) {
    Remove-Item variable:global:_BuildTool
  }
  Pop-Location
}
