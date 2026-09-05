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
$compilerServerPath = [System.IO.Path]::Combine($repoRoot, "artifacts", "bin", "VBCSCompiler", $Configuration, "net10.0", "VBCSCompiler.dll")

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

function Get-CacheStatsTimestamp {
  return [System.DateTimeOffset]::UtcNow.ToString("O", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Invoke-NativeCommand([string]$FilePath, [string[]]$Arguments) {
  & $FilePath @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
  return $LASTEXITCODE
}

function Invoke-TimedCommand([string]$Name, [string]$FilePath, [string[]]$Arguments) {
  Write-Host ""
  Write-Host "== $Name =="
  Write-Host "> $FilePath $($Arguments -join ' ')"

  $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
  $exitCode = Invoke-NativeCommand $FilePath $Arguments
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
  $exitCode = Invoke-NativeCommand "dotnet" @("build-server", "shutdown")
  if ($exitCode -ne 0) {
    throw "dotnet build-server shutdown failed with exit code $exitCode."
  }
}

function Invoke-Restore([string]$Name) {
  Invoke-TimedCommand $Name "dotnet" @(
    "restore",
    $solutionPath,
    "-p:Configuration=$Configuration",
    "-v:quiet",
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
    "-v:quiet",
    "-tl:off"
  )
}

function Invoke-DownloadCache {
  $arguments = @(
    "run",
    "--no-cache",
    "--file",
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
  $cacheStatsStart = Get-CacheStatsTimestamp
  $restore = Invoke-Restore "$Label restore"
  $build = Invoke-Build "$Label build"
  Show-CacheStats $Label $cacheStatsStart

  return @($restore, $build)
}

function Show-CacheStats([string]$Label, [string]$Since) {
  Write-Host ""
  Write-Host "Compiler cache stats for $Label since pass start ($Since):"

  if (-not (Test-Path -LiteralPath $compilerServerPath)) {
    Write-Warning "VBCSCompiler was not found at '$compilerServerPath'. Skipping cache stats."
    return
  }

  if (-not (Test-Path -LiteralPath $CachePath)) {
    Write-Warning "Compiler cache path does not exist: $CachePath. Skipping cache stats."
    return
  }

  $exitCode = Invoke-NativeCommand "dotnet" @(
    "exec",
    $compilerServerPath,
    "-cachepath:$CachePath",
    "-cachestats:$Since",
    "-cachestatsverbosity:1"
  )

  if ($exitCode -ne 0) {
    Write-Warning "Failed to show compiler cache stats. Exit code: $exitCode"
  }
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
  $results += Invoke-BuildPass "Warm local cache"

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