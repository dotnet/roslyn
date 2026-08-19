<#
.SYNOPSIS
  Capture a capture/replay-cache baseline: hash the input set (git-tracked files) and
  snapshot the output set (build artifacts). Part of the BigBuild caching pilot; see
  bigbuild/capture-replay-cache-design.md.

.DESCRIPTION
  Records, for a repo that has just been built successfully:
    - inputs.clixml  : per-tracked-file { Rel, Path, Sha, Mtime(ticks), Size }
    - outputs.clixml : Snap-Tree of the output roots (absolute Path, Sha, Mtime, Size)
    - meta.json      : commit SHA, output roots, repo root, timestamp
  The input manifest is what a replay hashes against to decide which files are unchanged
  (and may be back-dated) versus changed/new (which must stay newer so their targets
  rebuild). The output snapshot is the oracle's "cold" side.

  Without -Archive the baseline is single-machine / in-place: outputs stay where they are,
  so baseline and replay must share the same absolute paths. With -Archive the output roots
  are also mirrored (timestamps preserved) into <BaselineDir>\overlay, making the baseline
  portable -- Restore-Baseline can seed a fresh clone at a DIFFERENT path (warm-clone cache).
#>
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$BaselineDir,
    [string[]]$OutputRoots = @('artifacts'),
    [string]$SnapTree = (Join-Path $PSScriptRoot 'Snap-Tree.ps1'),
    [switch]$Archive,
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path
New-Item -ItemType Directory -Force $BaselineDir | Out-Null

$sha = (git -C $RepoRoot rev-parse HEAD).Trim()
Write-Host "[capture] repo=$RepoRoot sha=$sha"

# Input set = git-tracked files. Tracked source is the real build input; the global
# NuGet cache and generated obj/ live outside this set (obj is under an output root).
$tracked = @(git -C $RepoRoot ls-files) | Where-Object { $_ }
Write-Host "[capture] $($tracked.Count) tracked paths; hashing inputs with $Throttle threads..."

$inputs = $tracked | ForEach-Object -Parallel {
    $rel = $_
    $full = Join-Path $using:RepoRoot ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $full)) { return }   # tracked but not materialized
    try {
        $fi = Get-Item -LiteralPath $full -Force
        $h = [System.Security.Cryptography.SHA256]::Create()
        $fs = [System.IO.File]::Open($full, 'Open', 'Read', 'ReadWrite')
        try { $hash = $h.ComputeHash($fs) } finally { $fs.Dispose(); $h.Dispose() }
        [pscustomobject]@{
            Rel   = $rel
            Path  = $full
            Sha   = [BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()
            Mtime = $fi.LastWriteTimeUtc.Ticks
            Size  = $fi.Length
        }
    } catch { }
} -ThrottleLimit $Throttle

$inputs | Export-Clixml -Path (Join-Path $BaselineDir 'inputs.clixml') -Depth 2
Write-Host "[capture] wrote $($inputs.Count) input entries"

# Output set snapshot (the oracle cold side). Reuse the shared Snap-Tree primitive.
$roots = $OutputRoots | ForEach-Object { Join-Path $RepoRoot $_ } | Where-Object { Test-Path $_ }
& $SnapTree -Roots $roots -Out (Join-Path $BaselineDir 'outputs.clixml') -Throttle $Throttle

# Overlay archive (the portable output tree). Without this the baseline can only be replayed
# in place (v1 single-machine); with it, a fresh clone at a DIFFERENT path can be seeded via
# Restore-Baseline. Mirror each output root preserving timestamps (/COPY:DAT) so the captured
# (older) mtimes ride along -- Replay-Baseline back-dates inputs relative to them.
if ($Archive) {
    $overlay = Join-Path $BaselineDir 'overlay'
    foreach ($r in $OutputRoots) {
        $src = Join-Path $RepoRoot $r
        if (-not (Test-Path $src)) { continue }
        $dst = Join-Path $overlay $r
        New-Item -ItemType Directory -Force $dst | Out-Null
        # /E all subdirs incl empty, /COPY:DAT preserve data+attrs+timestamps, /MT multithread, quiet.
        robocopy $src $dst /E /COPY:DAT /MT:$Throttle /NFL /NDL /NJH /NJS /NP | Out-Null
    }
    Write-Host "[capture] archived overlay of $($OutputRoots -join ', ') to $overlay"
}

$meta = [pscustomobject]@{
    Sha         = $sha
    RepoRoot    = $RepoRoot
    OutputRoots = $OutputRoots
    Archived    = [bool]$Archive
    CapturedUtc = (Get-Date).ToUniversalTime().ToString('o')
}
$meta | ConvertTo-Json | Set-Content -Path (Join-Path $BaselineDir 'meta.json') -Encoding ascii
Write-Host "[capture] baseline written to $BaselineDir"
