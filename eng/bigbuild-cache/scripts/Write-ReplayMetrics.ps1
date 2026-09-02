<#
.SYNOPSIS
  Write machine-readable metadata for one historical warm-cache replay.

.DESCRIPTION
  Records the requested and actual commit, cache lineage, timings, input
  classification, expanded cache size, and output hash manifest. The small
  metrics artifact is sufficient for clean-control comparisons without
  downloading the full warm-cache payload.
#>
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$CacheStore,
    [Parameter(Mandatory)][string]$MetricsDir,
    [Parameter(Mandatory)][string]$ReplayCommit,
    [Parameter(Mandatory)][int]$ReplaySequence,
    [Parameter(Mandatory)][string]$ReplayExperimentId,
    [Parameter(Mandatory)][string]$SdkFingerprint,
    [Parameter(Mandatory)][string]$Solution,
    [string]$Seeded = 'false',
    [string]$PreviousBaselineSha = '',
    [string]$ForceCold = 'false',
    [double]$RestoreSeconds = 0,
    [double]$BuildSeconds = 0,
    [double]$CaptureSeconds = 0
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path
New-Item -ItemType Directory -Force $MetricsDir | Out-Null

$actualCommit = (git -C $RepoRoot rev-parse HEAD).Trim()
$lane = "roslyn-$ReplayExperimentId-$SdkFingerprint-win"
$baselineDir = Join-Path (Join-Path $CacheStore $lane) $actualCommit
$classificationPath = Join-Path $MetricsDir 'input-classification.json'
$classification = if (Test-Path $classificationPath) {
    Get-Content $classificationPath -Raw | ConvertFrom-Json
}
else {
    $null
}

$cacheFiles = if (Test-Path $CacheStore) {
    @(Get-ChildItem $CacheStore -Recurse -File -ErrorAction SilentlyContinue)
}
else {
    @()
}
$cacheBytes = ($cacheFiles | Measure-Object -Property Length -Sum).Sum
if ($null -eq $cacheBytes) {
    $cacheBytes = 0
}

$inputCount = 0
$outputCount = 0
$baselineMetaPath = Join-Path $baselineDir 'meta.json'
$inputManifestPath = Join-Path $baselineDir 'inputs.clixml'
$outputManifestPath = Join-Path $baselineDir 'outputs.clixml'
if (Test-Path $inputManifestPath) {
    $inputCount = @(Import-Clixml $inputManifestPath).Count
}
if (Test-Path $outputManifestPath) {
    $outputCount = @(Import-Clixml $outputManifestPath).Count
    Copy-Item $outputManifestPath (Join-Path $MetricsDir 'outputs.clixml') -Force
}
if (Test-Path $baselineMetaPath) {
    Copy-Item $baselineMetaPath (Join-Path $MetricsDir 'baseline-meta.json') -Force
}

$globalJson = Get-Content (Join-Path $RepoRoot 'global.json') -Raw | ConvertFrom-Json
$commit = git -C $RepoRoot show -s --format='%cI%x00%s' HEAD
$commitParts = "$commit" -split "`0", 2
$buildUrl = if ($env:SYSTEM_COLLECTIONURI -and $env:SYSTEM_TEAMPROJECT -and $env:BUILD_BUILDID) {
    "$($env:SYSTEM_COLLECTIONURI)$($env:SYSTEM_TEAMPROJECT)/_build/results?buildId=$($env:BUILD_BUILDID)"
}
else {
    ''
}

$metrics = [ordered]@{
    replaySequence = $ReplaySequence
    replayExperimentId = $ReplayExperimentId
    requestedCommit = $ReplayCommit
    actualCommit = $actualCommit
    previousBaselineSha = $PreviousBaselineSha
    seeded = ($Seeded -eq 'true')
    forceCold = ($ForceCold -eq 'true')
    solution = $Solution
    sdkFingerprint = $SdkFingerprint
    dotnetSdkVersion = $globalJson.tools.dotnet
    arcadeSdkVersion = $globalJson.'msbuild-sdks'.'Microsoft.DotNet.Arcade.Sdk'
    commitTime = $commitParts[0]
    commitSubject = if ($commitParts.Count -gt 1) { $commitParts[1] } else { '' }
    pipelineBuildId = $env:BUILD_BUILDID
    pipelineBuildNumber = $env:BUILD_BUILDNUMBER
    pipelineSourceBranch = $env:BUILD_SOURCEBRANCH
    pipelineSourceVersion = $env:BUILD_SOURCEVERSION
    pipelineUrl = $buildUrl
    agentName = $env:AGENT_NAME
    agentMachineName = $env:AGENT_MACHINENAME
    restoreSeconds = $RestoreSeconds
    buildSeconds = $BuildSeconds
    captureSeconds = $CaptureSeconds
    cacheExpandedBytes = [long]$cacheBytes
    cacheFileCount = $cacheFiles.Count
    inputManifestCount = $inputCount
    outputManifestCount = $outputCount
    inputClassification = $classification
    recordedUtc = (Get-Date).ToUniversalTime().ToString('o')
}

$metrics | ConvertTo-Json -Depth 8 |
    Set-Content -Path (Join-Path $MetricsDir 'replay-metrics.json') -Encoding ascii
Write-Host "[metrics] wrote $(Join-Path $MetricsDir 'replay-metrics.json')"
