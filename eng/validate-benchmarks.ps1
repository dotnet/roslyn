<#
  This script validates that our benchmark projects remain runnable by executing them
  in BenchmarkDotNet's Dry mode. This catches issues where package updates or code
  changes break benchmark execution.
#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Release",
  [switch]$ci = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

$repoDir = Resolve-Path (Join-Path $PSScriptRoot "..")

# Each entry is a hashtable with the project path and optional framework.
# Comment out entries that are currently broken and file a tracking bug.
$benchmarkProjects = @(
  @{ Project = "src/Tools/Benchmarks/Benchmarks.csproj" }
  @{ Project = "src/Razor/src/Compiler/perf/Microbenchmarks/Microsoft.AspNetCore.Razor.Microbenchmarks.Compiler.csproj"; Framework = "net10.0" }
  @{ Project = "src/Razor/src/Razor/benchmarks/Microsoft.AspNetCore.Razor.Microbenchmarks/Microsoft.AspNetCore.Razor.Microbenchmarks.csproj"; Framework = "net10.0" }
  @{ Project = "src/Razor/src/Compiler/perf/Microsoft.AspNetCore.Razor.Microbenchmarks.Generator/Microsoft.AspNetCore.Razor.Microbenchmarks.Generator.csproj" }

  # Currently broken, tracking bugs to be filed
  # @{ Project = "src/Tools/IdeCoreBenchmarks/IdeCoreBenchmarks.csproj"; Framework = "net10.0" }
  # @{ Project = "src/Tools/IdeBenchmarks/IdeBenchmarks.csproj" }
)

$failed = @()

foreach ($entry in $benchmarkProjects) {
  $projectPath = Join-Path $repoDir $entry.Project
  $projectName = Split-Path $entry.Project -Leaf

  Write-Host ""
  Write-Host "=== Validating $projectName ===" -ForegroundColor Cyan

  $args = @(
    "run"
    "--project", $projectPath
    "-c", $configuration
    "--no-build"
  )

  if ($entry.ContainsKey("Framework")) {
    $args += "-f"
    $args += $entry["Framework"]
  }

  # Separator between dotnet args and BenchmarkDotNet args
  $args += "--"
  $args += "--job"
  $args += "Dry"

  Write-Host "dotnet $($args -join ' ')"

  & dotnet @args

  if ($LASTEXITCODE -ne 0) {
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
