<#
  This script validates that our benchmark projects remain runnable by executing them
  in BenchmarkDotNet's Dry mode. This catches issues where package updates or code
  changes break benchmark execution.
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Release",
  [switch]$ci = $false,
  [switch]$warnAsError = $ci)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

$msbuildEngine = "dotnet"
$disablePipelineSetResult = $true
. (Join-Path $PSScriptRoot "build-utils.ps1")

# Each entry is a hashtable with the project path and optional framework.
$benchmarkProjects = @(
  @{ Project = "src/Tools/Benchmarks/Benchmarks.csproj" }
  @{ Project = "src/Razor/src/Compiler/perf/Microbenchmarks/Microsoft.AspNetCore.Razor.Microbenchmarks.Compiler.csproj"; Framework = "net10.0" }
  @{ Project = "src/Razor/src/Razor/benchmarks/Microsoft.AspNetCore.Razor.Microbenchmarks/Microsoft.AspNetCore.Razor.Microbenchmarks.csproj"; Framework = "net10.0"; HasValidationMode = $true }
  @{ Project = "src/Razor/src/Compiler/perf/Microsoft.AspNetCore.Razor.Microbenchmarks.Generator/Microsoft.AspNetCore.Razor.Microbenchmarks.Generator.csproj"; HasValidationMode = $true }
  # Use a representative type to exercise the generated runner build without running the full benchmark suite.
  # This project uses BenchmarkDotNet 0.15 for the VS DiagnosticsHub adapter, so keep both its host and generated runner on .NET 10.
  @{ Project = "src/Tools/IdeCoreBenchmarks/IdeCoreBenchmarks.csproj"; Framework = "net10.0"; Filter = "*SegmentedArrayBenchmarks_Indexer*"; RollForward = "LatestPatch"; RollForwardToPrerelease = "0" }

  # These projects are excluded because their current benchmark harnesses do not
  # complete a Dry validation run in this script's execution model.
  # @{ Project = "src/Tools/IdeBenchmarks/IdeBenchmarks.csproj" }
)

$failed = @()

foreach ($entry in $benchmarkProjects) {
  $projectPath = Join-Path $RepoRoot $entry.Project
  $projectName = Split-Path $entry.Project -Leaf

  Write-Host ""
  Write-Host "=== Validating $projectName ===" -ForegroundColor Cyan

  $buildArgs = @(
    $projectPath
    "/restore"
    "/t:Build"
    "/p:Configuration=$configuration"
  )

  $runArgs = @(
    "run"
    "--no-build"
    "--project", $projectPath
    "-c", $configuration
  )

  if ($ci) {
    $buildArgs += "/p:UseRazorBuildServer=false"
    $buildArgs += "/p:UseSharedCompilation=false"
    $buildArgs += "-bl:$(Join-Path $LogDir "$projectName.Build.binlog")"
    $runArgs += "--disable-build-servers"
  }

  if ($entry.ContainsKey("Framework")) {
    $buildArgs += "/p:TargetFramework=$($entry["Framework"])"
    $runArgs += "-f"
    $runArgs += $entry["Framework"]
  }

  # Separator between dotnet args and BenchmarkDotNet args
  $runArgs += "--"
  if ($entry.ContainsKey("HasValidationMode")) {
    # These harnesses define multiple jobs for normal benchmark runs. Their validation
    # mode replaces those jobs with one Dry job instead of unioning a CLI job with them.
    $runArgs += "--validate"
  }
  else {
    $runArgs += "--job"
    $runArgs += "Dry"
  }

  if ($ci) {
    # Keep the filter as one argument so PowerShell does not expand '*' into file names.
    $filter = if ($entry.ContainsKey("Filter")) { $entry["Filter"] } else { "*" }
    $runArgs += "--filter=$filter"
  }

  $previousRollForward = $env:DOTNET_ROLL_FORWARD
  $previousRollForwardToPrerelease = $env:DOTNET_ROLL_FORWARD_TO_PRERELEASE
  try {
    # CI sets these variables to LatestMajor globally; scope project-specific overrides to this process and its benchmark children.
    if ($entry.ContainsKey("RollForward")) {
      $env:DOTNET_ROLL_FORWARD = $entry["RollForward"]
    }
    if ($entry.ContainsKey("RollForwardToPrerelease")) {
      $env:DOTNET_ROLL_FORWARD_TO_PRERELEASE = $entry["RollForwardToPrerelease"]
    }

    # Use Arcade's MSBuild helper for correct warnAsError/warnNotAsError behavior.
    & (Join-Path $PSScriptRoot "common\msbuild.ps1") -msbuildEngine dotnet -ci:$ci -warnAsError:$warnAsError @buildArgs
    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
      Write-Host "dotnet $($runArgs -join ' ')"
      $null = DotNet -ignoreFailure @runArgs
      $exitCode = $LASTEXITCODE
    }
  }
  finally {
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
    $env:DOTNET_ROLL_FORWARD_TO_PRERELEASE = $previousRollForwardToPrerelease
  }

  if ($exitCode -ne 0) {
    Write-Host "FAILED: $projectName" -ForegroundColor Red
    $failed += $projectName
  }
  else {
    Write-Host "PASSED: $projectName" -ForegroundColor Green
  }
}

Write-Host ""
if ($failed.Count -gt 0) {
  Write-Host "The following benchmark projects failed dry run validation:" -ForegroundColor Red
  foreach ($f in $failed) {
    Write-Host "  - $f" -ForegroundColor Red
  }
  exit 1
}
else {
  Write-Host "All benchmark projects passed dry run validation." -ForegroundColor Green
}
