<#
.SYNOPSIS
  Pick the nearest usable baseline for a clone from a store lane. Part of the BigBuild
  caching pilot; see bigbuild/capture-replay-cache-design.md.

.DESCRIPTION
  A store lane (<Store>\<lane>) holds baselines keyed by commit SHA -- one subdirectory per
  captured commit, each with meta.json (+ inputs/outputs/overlay). A clone rarely sits
  exactly on a captured commit, so this walks the clone's first-parent history and returns
  the FIRST ancestor that has a baseline in the lane. Replay-Baseline then reconciles the
  delta (commits between the baseline and HEAD touch some inputs, which get rebuilt; the
  rest are back-dated and skipped).

  Emits the selected baseline directory path on success (and its distance/SHA to stderr).
  Writes nothing and exits 1 if no ancestor within -MaxDepth has a baseline.
#>
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$LaneDir,
    [int]$MaxDepth = 500
)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path

if (-not (Test-Path $LaneDir)) { [Console]::Error.WriteLine("[select] lane not found: $LaneDir"); exit 1 }

$shas = @(git -C $RepoRoot rev-list --first-parent -n $MaxDepth HEAD) | Where-Object { $_ }
$distance = 0
foreach ($sha in $shas) {
    $dir = Join-Path $LaneDir $sha
    if (Test-Path (Join-Path $dir 'meta.json')) {
        Write-Host "[select] baseline $sha is $distance commit(s) behind HEAD -> $dir"
        Write-Output $dir
        exit 0
    }
    $distance++
}
[Console]::Error.WriteLine("[select] no baseline within $MaxDepth ancestors of HEAD in $LaneDir")
exit 1
