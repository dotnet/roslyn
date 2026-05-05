param (
  [string]$Configuration = "Debug",
  [string]$Solution = "Compilers.slnf",
  [string]$CachePath = "",
  [int]$BuildId = 0,
  [string]$Branch = "",
  [int]$PipelineId = 0
)

Set-StrictMode -version 3.0
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsPath = Join-Path $repoRoot "artifacts"
$solutionPath = Join-Path $repoRoot $Solution
$enableCacheScript = Join-Path $repoRoot "eng/enable-compiler-cache.cs"

if ($CachePath -eq "") {
  $repoName = Split-Path $repoRoot -Leaf
  $CachePath = Join-Path ([System.IO.Path]::GetTempPath()) "roslyn-compiler-cache-benchmark/$repoName"
}

$CachePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($CachePath)

if (-not (Test-Path $solutionPath)) {
  throw "Solution file '$solutionPath' does not exist."
}

if (-not (Test-Path $enableCacheScript)) {
  throw "Cache enable script '$enableCacheScript' does not exist."
}

function Format-Duration([TimeSpan]$duration) {
  return "{0:hh\:mm\:ss\.fff}" -f $duration
}

function Invoke-TimedCommand([string]$Name, [string]$FilePath, [string[]]$Arguments) {
  Write-Host ""
  Write-Host "== $Name =="
  Write-Host "> $FilePath $($Arguments -join ' ')"

  $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
  & $FilePath @Arguments
  $exitCode = $LASTEXITCODE
  $stopwatch.Stop()

  if ($exitCode -ne 0) {
    throw "$Name failed with exit code $exitCode after $(Format-Duration $stopwatch.Elapsed)."
  }

  Write-Host "Elapsed: $(Format-Duration $stopwatch.Elapsed)"

  return [PSCustomObject]@{
    Name = $Name
    Elapsed = $stopwatch.Elapsed
  }
}

function Clear-Artifacts {
  Write-Host ""
  Write-Host "Clearing artifacts: $artifactsPath"
  if (Test-Path $artifactsPath) {
    Remove-Item $artifactsPath -Recurse -Force
  }
}

function Clear-CompilerCache {
  Write-Host ""
  Write-Host "Clearing compiler cache: $CachePath"
  if (Test-Path $CachePath) {
    Remove-Item $CachePath -Recurse -Force
  }
}

function Stop-BuildServers {
  Write-Host ""
  Write-Host "Stopping dotnet build servers"
  & dotnet build-server shutdown
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet build-server shutdown failed with exit code $LASTEXITCODE."
  }
}

function Invoke-Restore([string]$Name) {
  Invoke-TimedCommand $Name "dotnet" @(
    "restore",
    $solutionPath,
    "-p:Configuration=$Configuration",
    "-tl:off"
  )
}

function Invoke-Build([string]$Name) {
  Invoke-TimedCommand $Name "dotnet" @(
    "build",
    $solutionPath,
    "--no-restore",
    "-p:Configuration=$Configuration",
    "-p:RunAnalyzersDuringBuild=false",
    "-p:GenerateFullPaths=true",
    "-tl:off"
  )
}

function Invoke-DownloadCache {
  $arguments = @(
    $enableCacheScript,
    "--",
    "--configuration",
    $Configuration
  )

  if ($BuildId -ne 0) {
    $arguments += @("--build-id", $BuildId.ToString())
  }

  if ($Branch -ne "") {
    $arguments += @("--branch", $Branch)
  }

  if ($PipelineId -ne 0) {
    $arguments += @("--pipeline-id", $PipelineId.ToString())
  }

  Invoke-TimedCommand "Download compiler cache" "dotnet" $arguments
}

function Invoke-BuildPass([string]$Label) {
  Stop-BuildServers
  Clear-Artifacts
  $restore = Invoke-Restore "$Label restore"
  $build = Invoke-Build "$Label build"

  return @($restore, $build)
}

$oldCachePath = $env:ROSLYN_CACHE_PATH
$oldUseCachingCompiler = $env:ROSLYN_USE_CACHING_COMPILER

try {
  Set-Location $repoRoot

  $env:ROSLYN_CACHE_PATH = $CachePath
  $env:ROSLYN_USE_CACHING_COMPILER = "true"

  Write-Host "Compiler cache benchmark"
  Write-Host "Repo:          $repoRoot"
  Write-Host "Solution:      $solutionPath"
  Write-Host "Configuration: $Configuration"
  Write-Host "Cache path:    $CachePath"
  Write-Host "Artifacts:     $artifactsPath"
  if ($BuildId -ne 0) {
    Write-Host "Build ID:      $BuildId"
  }

  Stop-BuildServers
  Clear-CompilerCache

  $results = @()
  $results += Invoke-BuildPass "Empty local cache"
  Stop-BuildServers
  $results += Invoke-DownloadCache
  $results += Invoke-BuildPass "Downloaded local cache"

  Write-Host ""
  Write-Host "Summary"
  foreach ($result in $results) {
    Write-Host ("  {0,-32} {1}" -f $result.Name, (Format-Duration $result.Elapsed))
  }
}
finally {
  if ($null -eq $oldCachePath) {
    Remove-Item Env:ROSLYN_CACHE_PATH -ErrorAction Ignore
  }
  else {
    $env:ROSLYN_CACHE_PATH = $oldCachePath
  }

  if ($null -eq $oldUseCachingCompiler) {
    Remove-Item Env:ROSLYN_USE_CACHING_COMPILER -ErrorAction Ignore
  }
  else {
    $env:ROSLYN_USE_CACHING_COMPILER = $oldUseCachingCompiler
  }
}