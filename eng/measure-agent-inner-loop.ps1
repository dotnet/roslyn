[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$slice = "",
  [ValidateRange(1, 20)]
  [int]$iterations = 3,
  [ValidateSet("Debug", "Release")]
  [string]$configuration = "Debug",
  [string]$testFilter = "",
  [string]$outputPath = "",
  [switch]$skipPreparation,
  [switch]$help)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "build-utils.ps1")

function Print-Usage([string[]]$supportedSlices) {
  Write-Host "Usage: measure-agent-inner-loop.ps1 -slice <name> [-iterations 3]"
  Write-Host "       [-configuration Debug] [-testFilter <filter>] [-outputPath <path>]"
  Write-Host "       [-skipPreparation]"
  Write-Host ""
  Write-Host "Measures product/test preparation, representative edit validation builds, and"
  Write-Host "filtered tests for a documented agent inner-loop slice."
  Write-Host ""
  Write-Host "Supported slices: $($supportedSlices -join ', ')"
  Write-Host ""
  Write-Host "Use -skipPreparation only when the product and test projects are already built."
}

$repoDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Join-RelativePath([string[]]$segments) {
  return [IO.Path]::Combine($segments)
}

$slices = @{
  CSharpCodeStyle = @{
    ProductProject = Join-RelativePath @(
      "src", "CodeStyle", "CSharp", "CodeFixes",
      "Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.csproj"
    )
    TestProject = Join-RelativePath @(
      "src", "CodeStyle", "CSharp", "Tests",
      "Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests.csproj"
    )
    RepresentativeSource = Join-RelativePath @(
      "src", "Analyzers", "CSharp", "Analyzers", "AddRequiredParentheses",
      "CSharpAddRequiredPatternParenthesesDiagnosticAnalyzer.cs"
    )
    TargetFrameworkProperty = "NetRoslyn"
    TestFilter = "FullyQualifiedName~Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses.AddRequiredPatternParenthesesTests"
  }
  CSharpFormatting = @{
    ProductProject = Join-RelativePath @(
      "src", "Workspaces", "CSharp", "Portable",
      "Microsoft.CodeAnalysis.CSharp.Workspaces.csproj"
    )
    TestProject = Join-RelativePath @(
      "src", "Workspaces", "CSharpTest",
      "Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.csproj"
    )
    RepresentativeSource = Join-RelativePath @(
      "src", "Workspaces", "SharedUtilitiesAndExtensions", "Workspace", "CSharp",
      "Formatting", "CSharpSyntaxFormattingService.cs"
    )
    TargetFrameworkProperty = "NetVSShared"
    TestFilter = "FullyQualifiedName=Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting.FormattingTests.Format1"
  }
}

$supportedSlices = @($slices.Keys | Sort-Object)
if ($help) {
  Print-Usage $supportedSlices
  exit 0
}

if ([string]::IsNullOrWhiteSpace($slice)) {
  Print-Usage $supportedSlices
  throw "Specify a slice with -slice."
}

if (-not $slices.ContainsKey($slice)) {
  Print-Usage $supportedSlices
  throw "Unknown slice '$slice'."
}

$sliceName = $slice
$sliceDefinition = $slices[$sliceName]

$dotnetExecutable = Ensure-DotnetSdk
$targetFrameworkProperty = $null
if ($sliceDefinition.ContainsKey("TargetFramework")) {
  $targetFramework = $sliceDefinition.TargetFramework
}
elseif ($sliceDefinition.ContainsKey("TargetFrameworkProperty")) {
  $targetFrameworkProperty = $sliceDefinition.TargetFrameworkProperty
  $testProjectPath = Join-Path $repoDir $sliceDefinition.TestProject
  $targetFrameworkOutput = @(
    & $dotnetExecutable msbuild $testProjectPath "-getProperty:$targetFrameworkProperty" -nologo |
      Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
  )
  if ($LASTEXITCODE -ne 0 -or $targetFrameworkOutput.Count -eq 0) {
    throw "Could not determine $targetFrameworkProperty for $($sliceDefinition.TestProject)."
  }

  $targetFramework = $targetFrameworkOutput[-1].Trim()
}
else {
  throw "Slice '$sliceName' does not define a target framework."
}

if ([string]::IsNullOrWhiteSpace($testFilter)) {
  $testFilter = $sliceDefinition.TestFilter
}

if ([string]::IsNullOrWhiteSpace($outputPath)) {
  $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
  $logDirectory = Join-Path (Join-Path $repoDir "artifacts") "log"
  $outputPath = Join-Path $logDirectory "agent-inner-loop-$($sliceName)-$timestamp.json"
}
elseif (-not [IO.Path]::IsPathRooted($outputPath)) {
  $outputPath = Join-Path $repoDir $outputPath
}

function Invoke-MeasuredDotNet(
  [string]$name,
  [string[]]$arguments,
  [string]$testResultPath = ""
) {
  Write-Host ""
  Write-Host "=== $name ===" -ForegroundColor Cyan
  Write-Host "dotnet $($arguments -join ' ')"

  $stopwatch = [Diagnostics.Stopwatch]::StartNew()
  & $dotnetExecutable @arguments | Out-Host
  $exitCode = $LASTEXITCODE
  $stopwatch.Stop()

  $measurement = [ordered]@{
    Name = $name
    DurationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
    ExitCode = $exitCode
    Command = "dotnet $($arguments -join ' ')"
  }

  if ($exitCode -ne 0) {
    throw "$name failed with exit code $exitCode."
  }

  if (-not [string]::IsNullOrWhiteSpace($testResultPath)) {
    if (-not (Test-Path $testResultPath)) {
      throw "$name did not produce the expected TRX result file: $testResultPath"
    }

    [xml]$testResult = Get-Content -Raw $testResultPath
    $testsExecuted = [int]$testResult.TestRun.ResultSummary.Counters.total
    $measurement.TestsExecuted = $testsExecuted
    if ($testsExecuted -eq 0) {
      throw "$name completed without executing any tests. Check the test filter."
    }
  }

  return $measurement
}

function Get-Median([double[]]$values) {
  $sorted = @($values | Sort-Object)
  $middle = [Math]::Floor($sorted.Count / 2)
  if (($sorted.Count % 2) -eq 1) {
    return $sorted[$middle]
  }

  return ($sorted[$middle - 1] + $sorted[$middle]) / 2
}

function Get-MeasurementDuration([string]$name) {
  $measurement = $measurements | Where-Object { $_.Name -eq $name }
  if ($null -eq $measurement) {
    return $null
  }

  return $measurement.DurationSeconds
}

$measurements = [Collections.Generic.List[object]]::new()
$testResultsDirectory = [IO.Path]::Combine($repoDir, "artifacts", "TestResults", "AgentInnerLoop")
$representativeSource = Get-Item (Join-Path $repoDir $sliceDefinition.RepresentativeSource)
$originalRepresentativeSourceLastWriteTimeUtc = $representativeSource.LastWriteTimeUtc
$previousLocation = Get-Location

try {
  Set-Location $repoDir

  if (-not $skipPreparation) {
    $measurements.Add((Invoke-MeasuredDotNet "Restore test graph" @(
      "restore", $sliceDefinition.TestProject,
      "--nologo",
      "--verbosity", "minimal"
    )))

    $measurements.Add((Invoke-MeasuredDotNet "Initial product build" @(
      "build", $sliceDefinition.ProductProject,
      "--configuration", $configuration,
      "--no-restore",
      "--nologo",
      "--verbosity", "minimal"
    )))

    $measurements.Add((Invoke-MeasuredDotNet "Prepare test project" @(
      "build", $sliceDefinition.TestProject,
      "--configuration", $configuration,
      "--no-restore",
      "--framework", $targetFramework,
      "--nologo",
      "--verbosity", "minimal"
    )))
  }

  for ($iteration = 1; $iteration -le $iterations; $iteration++) {
    $representativeSource.LastWriteTimeUtc = [DateTime]::UtcNow

    $measurements.Add((Invoke-MeasuredDotNet "Representative edit validation build $iteration" @(
      "build", $sliceDefinition.TestProject,
      "--configuration", $configuration,
      "--no-restore",
      "--framework", $targetFramework,
      "--nologo",
      "--verbosity", "minimal"
    )))

    New-Item -ItemType Directory -Force -Path $testResultsDirectory | Out-Null
    $trxFileName = "$sliceName-$iteration.trx"
    $trxPath = Join-Path $testResultsDirectory $trxFileName
    Remove-Item $trxPath -ErrorAction SilentlyContinue

    $measurements.Add((Invoke-MeasuredDotNet "Filtered test $iteration" @(
      "test", $sliceDefinition.TestProject,
      "--configuration", $configuration,
      "--framework", $targetFramework,
      "--no-build",
      "--no-restore",
      "--filter", $testFilter,
      "--logger", "trx;LogFileName=$trxFileName",
      "--results-directory", $testResultsDirectory,
      "--nologo",
      "--verbosity", "minimal"
    ) $trxPath))
  }
}
finally {
  $representativeSource.LastWriteTimeUtc = $originalRepresentativeSourceLastWriteTimeUtc
  Set-Location $previousLocation
}

$representativeEditValidationBuildDurations = @(
  $measurements |
    Where-Object { $_.Name -like "Representative edit validation build *" } |
    ForEach-Object { $_.DurationSeconds }
)
$filteredTestDurations = @(
  $measurements |
    Where-Object { $_.Name -like "Filtered test *" } |
    ForEach-Object { $_.DurationSeconds }
)

$result = [ordered]@{
  Slice = $sliceName
  Configuration = $configuration
  Iterations = $iterations
  ProductProject = $sliceDefinition.ProductProject
  TestProject = $sliceDefinition.TestProject
  RepresentativeSource = $sliceDefinition.RepresentativeSource
  TargetFrameworkProperty = $targetFrameworkProperty
  TargetFramework = $targetFramework
  RepresentativeTestFilter = $sliceDefinition.TestFilter
  TestFilter = $testFilter
  TimestampUtc = [DateTime]::UtcNow.ToString("o")
  Machine = [ordered]@{
    OS = [Environment]::OSVersion.VersionString
    ProcessorCount = [Environment]::ProcessorCount
    DotNetVersion = (& $dotnetExecutable --version)
  }
  Summary = [ordered]@{
    RestoreSeconds = Get-MeasurementDuration "Restore test graph"
    InitialProductBuildSeconds = Get-MeasurementDuration "Initial product build"
    TestProjectPreparationSeconds = Get-MeasurementDuration "Prepare test project"
    RepresentativeEditValidationBuildMedianSeconds = [Math]::Round((Get-Median $representativeEditValidationBuildDurations), 3)
    FilteredTestMedianSeconds = [Math]::Round((Get-Median $filteredTestDurations), 3)
  }
  Measurements = $measurements
}

$outputDirectory = Split-Path -Parent $outputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$result | ConvertTo-Json -Depth 6 | Set-Content -Path $outputPath -Encoding utf8

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Green
Write-Host "Representative edit validation build median: $($result.Summary.RepresentativeEditValidationBuildMedianSeconds) seconds"
Write-Host "Filtered test median:                       $($result.Summary.FilteredTestMedianSeconds) seconds"
Write-Host "Results: $outputPath"
