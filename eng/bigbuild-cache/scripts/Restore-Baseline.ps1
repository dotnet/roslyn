<#
.SYNOPSIS
  Seed a fresh clone with a captured baseline's output overlay (warm-clone cache). Part of
  the BigBuild caching pilot; see bigbuild/capture-replay-cache-design.md.

.DESCRIPTION
  Mirrors the portable overlay produced by Capture-Baseline -Archive into a target repo,
  preserving the captured (older) timestamps so a subsequent Replay-Baseline -BackDate can
  make the tracked inputs older still and the up-to-date check skips the targets.

  This is the "extract into a fresh checkout at a different path" half of the warm-clone
  cache. Whether the seeded outputs are actually accepted (compile skipped) rather than
  rebuilt depends on the cache-hash gates being location-independent -- i.e. the PathMap
  normalization landing in the toolchain that builds the clone.

  Reads OutputRoots from the baseline's meta.json; each <BaselineDir>\overlay\<root> is
  mirrored to <RepoRoot>\<root>. /COPY:DAT preserves data+attributes+timestamps.
#>
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$BaselineDir,
    [switch]$Mirror,                                  # /MIR: also delete target files absent
                                                      # from the overlay (clean seed). Off by
                                                      # default so a fresh clone's tracked files
                                                      # are never touched.
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path

$metaPath = Join-Path $BaselineDir 'meta.json'
if (-not (Test-Path $metaPath)) { throw "no meta.json in $BaselineDir" }
$meta = Get-Content $metaPath -Raw | ConvertFrom-Json
if (-not $meta.Archived) { throw "baseline $BaselineDir was captured without -Archive (no overlay to restore)" }

$overlay = Join-Path $BaselineDir 'overlay'
if (-not (Test-Path $overlay)) { throw "overlay missing at $overlay" }

$mode = if ($Mirror) { '/MIR' } else { '/E' }
$restored = 0
foreach ($r in @($meta.OutputRoots)) {
    $src = Join-Path $overlay $r
    if (-not (Test-Path $src)) { continue }
    $dst = Join-Path $RepoRoot $r
    New-Item -ItemType Directory -Force $dst | Out-Null
    robocopy $src $dst $mode /COPY:DAT /MT:$Throttle /NFL /NDL /NJH /NJS /NP | Out-Null
    # robocopy exit codes 0-7 are success (8+ is failure).
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE) restoring $r" }
    $restored++
    Write-Host "[restore] $r -> $dst"
}
Write-Host "[restore] seeded $restored output root(s) from $BaselineDir (sha=$($meta.Sha))"
