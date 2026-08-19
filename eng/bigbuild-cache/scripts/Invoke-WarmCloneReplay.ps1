<#
.SYNOPSIS
  End-to-end warm-clone cache replay: seed a fresh clone from a stored baseline, reconcile
  inputs, (build), and run the cross-path correctness oracle. Part of the BigBuild caching
  pilot; see bigbuild/capture-replay-cache-design.md.

.DESCRIPTION
  Composes the primitives into the v2 (cross-machine / cross-path) flow:

    1. lane      : derive <RepoName>-<SdkFingerprint>-<os> and its store directory. The
                   fingerprint pins the toolchain so a baseline is only reused by an
                   identical compiler/SDK; a different toolchain gets its own lane.
    2. capture   : (optional -Capture) archive a baseline from -SourceRepo into the lane,
                   keyed by that repo's HEAD sha, with a portable overlay.
    3. select    : find the nearest ancestor baseline of -CloneRepo's HEAD in the lane.
    4. restore   : mirror the baseline overlay into -CloneRepo (timestamps preserved).
    5. back-date : hash -CloneRepo's tracked inputs against the baseline manifest; unchanged
                   inputs are back-dated below the outputs (skip), changed/new stay newer
                   (rebuild).
    6. build     : (optional -BuildCommand) run the real build in -CloneRepo. Skipped
                   targets are the win; anything the delta touched rebuilds.
    7. oracle    : compare -CloneRepo's outputs to the baseline by repo-relative key
                   (cross-path). content-diff == 0 means the seeded outputs were accepted
                   at the new path -- the thing the PathMap normalization makes possible.

  Without -BuildCommand the orchestrator stops after back-dating and runs the oracle on the
  seeded state as-is (plumbing check: proves restore + relative oracle line up).
#>
param(
    [Parameter(Mandatory)][string]$CloneRepo,         # fresh checkout to seed + replay (+ build)
    [Parameter(Mandatory)][string]$Store,             # baseline store root
    [Parameter(Mandatory)][string]$SdkFingerprint,    # toolchain lane key, e.g. sdk-10.0.301-pathmap
    [string]$SourceRepo,                              # built clone to capture from (with -Capture)
    [string]$RepoName,                                # lane repo name; default = CloneRepo dir leaf
    [switch]$Capture,                                 # capture a baseline from SourceRepo first
    [string]$BuildCommand,                            # build to run in CloneRepo (omit for dry run)
    [string[]]$OutputRoots = @('artifacts'),
    [string]$RunDir,                                  # scratch for oracle artifacts (keeps store clean)
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$CloneRepo = (Resolve-Path $CloneRepo).Path

$os = if ($IsWindows) { 'win' } elseif ($IsMacOS) { 'osx' } else { 'linux' }
if (-not $RepoName) { $RepoName = Split-Path -Leaf (git -C $CloneRepo rev-parse --show-toplevel).Trim() }
$lane = "$RepoName-$SdkFingerprint-$os"
$laneDir = Join-Path $Store $lane
Write-Host "==> lane: $lane"

# 2. capture (optional)
if ($Capture) {
    if (-not $SourceRepo) { throw "-Capture requires -SourceRepo" }
    $SourceRepo = (Resolve-Path $SourceRepo).Path
    $srcSha = (git -C $SourceRepo rev-parse HEAD).Trim()
    $baselineDir = Join-Path $laneDir $srcSha
    Write-Host "==> capture: $SourceRepo @ $srcSha -> $baselineDir"
    & (Join-Path $here 'Capture-Baseline.ps1') -RepoRoot $SourceRepo -BaselineDir $baselineDir `
        -OutputRoots $OutputRoots -Archive -Throttle $Throttle
}

# 3. select
Write-Host "==> select: nearest baseline for $CloneRepo"
$sel = & (Join-Path $here 'Select-Baseline.ps1') -RepoRoot $CloneRepo -LaneDir $laneDir | Select-Object -Last 1
if (-not $sel) { throw "no baseline available in lane $lane" }
$meta = Get-Content (Join-Path $sel 'meta.json') -Raw | ConvertFrom-Json
$refRoot = $meta.RepoRoot

# 4. restore
Write-Host "==> restore: overlay -> $CloneRepo"
& (Join-Path $here 'Restore-Baseline.ps1') -RepoRoot $CloneRepo -BaselineDir $sel -Throttle $Throttle

# 5. back-date
Write-Host "==> back-date: reconcile inputs against baseline"
& (Join-Path $here 'Replay-Baseline.ps1') -RepoRoot $CloneRepo -BaselineDir $sel -Mode BackDate -Throttle $Throttle

# 6. build (optional)
if ($BuildCommand) {
    Write-Host "==> build: $BuildCommand"
    Push-Location $CloneRepo
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        Invoke-Expression $BuildCommand
        $code = $LASTEXITCODE
        $sw.Stop()
        Write-Host ("==> build finished in {0:n1}s (exit {1})" -f $sw.Elapsed.TotalSeconds, $code)
        if ($code -ne 0) { throw "build failed ($code)" }
    } finally { Pop-Location }
} else {
    Write-Host "==> build: skipped (dry run -- oracle measures seeded state as-is)"
}

# 7. oracle (cross-path)
if (-not $RunDir) { $RunDir = Join-Path ([System.IO.Path]::GetTempPath()) ("warmclone-" + (Get-Date -Format yyyyMMdd-HHmmss)) }
New-Item -ItemType Directory -Force $RunDir | Out-Null
Write-Host "==> oracle: cross-path compare (ref root=$refRoot)"
& (Join-Path $here 'Verify-Replay.ps1') -RepoRoot $CloneRepo -BaselineDir $RunDir `
    -Reference (Join-Path $sel 'outputs.clixml') -ReferenceRoot $refRoot `
    -OutputRoots $OutputRoots -Throttle $Throttle
exit $LASTEXITCODE
