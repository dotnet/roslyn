# Make a bootstrap compiler and install it into artifacts/bootstrap folder

[CmdletBinding(PositionalBinding=$true)]
param (
  [string]$output = "",
  [string]$toolset = "Default",
  [string]$configuration = "Release",
  [switch]$force = $false,
  [switch]$ci = $false
)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {

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

  # Because we override the C#/VB toolset to build against our LKG package, it is important
  # that we do not reuse MSBuild nodes from other jobs/builds on the machine. Otherwise,
  # we'll run into issues such as https://github.com/dotnet/roslyn/issues/6211.
  # MSBuildAdditionalCommandLineArgs=
  $args = "/p:TreatWarningsAsErrors=true /warnaserror /nologo /nodeReuse:false /p:Configuration=$configuration /v:m";
  $args += " /p:RunAnalyzersDuringBuild=false /bl:$binaryLogFilePath"
  $args += " /t:Pack /p:RoslynEnforceCodeStyle=false /p:DotNetUseShippingVersions=true /p:InitialDefineConstants=BOOTSTRAP"
  $args += " /p:PackageOutputPath=$output /p:NgenOptimization=false /p:PublishWindowsPdb=false"

  if ($ci) {
    $args += " /p:ContinuousIntegrationBuild=true"
  
    # Set NUGET_PACKAGES to fix issues with package Restore when building with `-ci`.
    # Workaround for https://github.com/dotnet/arcade/issues/15970
    $env:NUGET_PACKAGES = Join-Path $RepoRoot '.packages\'
    $env:RESTORENOCACHE = $true
  }

  Exec-DotNet "build $args $projectPath"

  $packageFilePath = Get-ChildItem -Path $output -Filter "$packageName.*.nupkg"
  Write-Host "Found package $packageFilePath"
  Unzip $packageFilePath.FullName $output

  Write-Host "Cleaning up artifacts"
  Exec-DotNet "build --no-restore /t:Clean $projectPath"
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
  Pop-Location
}