<#
.SYNOPSIS
  Correctness oracle + skip metric for a capture/replay-cache run. Part of the BigBuild
  caching pilot; see bigbuild/capture-replay-cache-design.md.

.DESCRIPTION
  Snapshots the current output roots and compares them to a reference output snapshot
  (Snap-Tree Clixml). Reports, per output file, one of:
    - untouched   : absent from both change sets -> the target was SKIPPED (the win)
    - mtime-only  : same content, newer mtime    -> re-emitted identical bytes (cheap
                    over-build, not a correctness problem)
    - content     : different content            -> a real rebuild-difference. For a
                    zero-delta replay this MUST be empty (modulo the allowlist) or it is
                    an under/over-build or nondeterminism finding.
    - added/removed : output-set drift.

  Reference is normally the baseline outputs.clixml (baseline == replay for zero-delta).
  For a changed-input replay, point -Reference at a clean-build snapshot instead.

  Pass -ReferenceRoot when the reference outputs were captured under a different repo root
  (warm-clone cache: baseline built in clone-A, replay in clone-B at another path). Both
  sides are then compared by repo-relative key instead of absolute path.

  PASS = content-change count is zero after applying the allowlist.
#>
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$BaselineDir,
    [string]$Reference = $null,                       # defaults to <BaselineDir>\outputs.clixml
    [string]$ReferenceRoot = $null,                   # set when the reference was captured under a
                                                      # DIFFERENT repo root (warm-clone / cross-path
                                                      # oracle). Both sides are then keyed by their
                                                      # path relative to their own root.
    [string[]]$OutputRoots = @('artifacts'),
    [string]$Allowlist = (Join-Path $PSScriptRoot 'output-diff-allowlist.json'),
    [string]$SnapTree = (Join-Path $PSScriptRoot 'Snap-Tree.ps1'),
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path
if (-not $Reference) { $Reference = Join-Path $BaselineDir 'outputs.clixml' }

# Snapshot the replay outputs.
$replaySnap = Join-Path $BaselineDir 'replay-outputs.clixml'
$roots = $OutputRoots | ForEach-Object { Join-Path $RepoRoot $_ } | Where-Object { Test-Path $_ }
& $SnapTree -Roots $roots -Out $replaySnap -Throttle $Throttle

# Key selection. In-place (default): compare by absolute path. Cross-path (-ReferenceRoot):
# strip each side's own root and normalize to a lowercase forward-slash relative key so the
# baseline (clone-A) and replay (clone-B) line up despite living at different absolute paths.
$crossPath = [bool]$ReferenceRoot
if ($crossPath) { $ReferenceRoot = (Resolve-Path $ReferenceRoot).Path }
function Get-Key([string]$path, [string]$root) {
    if (-not $crossPath) { return $path }
    $p = $path
    if ($root -and $p.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        $p = $p.Substring($root.Length)
    }
    return ($p -replace '\\', '/').TrimStart('/').ToLowerInvariant()
}

$ref = @{}; Import-Clixml $Reference   | ForEach-Object { $ref[(Get-Key $_.Path $ReferenceRoot)] = $_ }
$now = @{}; Import-Clixml $replaySnap  | ForEach-Object { $now[(Get-Key $_.Path $RepoRoot)] = $_ }

# Allowlist of known-churny outputs (schema: { entries: [ { pattern: "**/glob" } ] }).
# Glob '**' is collapsed to '*' and paths are compared with forward slashes so PowerShell
# -like handles them.
$allow = @()
if ($Allowlist -and (Test-Path $Allowlist)) {
    try {
        $json = Get-Content $Allowlist -Raw | ConvertFrom-Json
        $allow = @($json.entries | ForEach-Object { ($_.pattern -replace '\*\*', '*') })
    } catch { $allow = @() }
}
function Test-Allowed([string]$path) {
    $p = ($path -replace '\\', '/')
    foreach ($g in $allow) { if ($g -and ($p -like $g)) { return $true } }
    return $false
}

$added = 0; $removed = 0; $mtime = 0
$content = New-Object System.Collections.Generic.List[object]
foreach ($k in $now.Keys) {
    if (-not $ref.ContainsKey($k)) { $added++; continue }
    $a = $ref[$k]; $b = $now[$k]
    if ($a.Sha -ne $b.Sha) { if (-not (Test-Allowed $k)) { $content.Add($b) }; continue }
    if ($a.Mtime -ne $b.Mtime) { $mtime++ }
}
foreach ($k in $ref.Keys) { if (-not $now.ContainsKey($k)) { $removed++ } }

$touched = $added + $removed + $mtime + $content.Count
$untouched = $ref.Count - $removed - $mtime - $content.Count
$skipPct = if ($ref.Count) { [math]::Round(100.0 * $untouched / $ref.Count, 2) } else { 0 }

Write-Host "=========================================================="
Write-Host "REPLAY ORACLE  (ref=$Reference)"
Write-Host "=========================================================="
Write-Host ("reference outputs : {0}" -f $ref.Count)
Write-Host ("replay outputs    : {0}" -f $now.Count)
Write-Host ("untouched (SKIPPED): {0}   ({1}% of reference)" -f $untouched, $skipPct)
Write-Host ("mtime-only        : {0}   (re-emitted identical)" -f $mtime)
Write-Host ("added / removed   : {0} / {1}" -f $added, $removed)
Write-Host ("content changes   : {0}   (allowlisted excluded)" -f $content.Count)
if ($content.Count -gt 0) {
    Write-Host "--- content offenders (top 40) ---"
    $content | Select-Object -First 40 | ForEach-Object { Write-Host ("  {0}" -f $_.Path) }
    $content | Export-Csv -NoTypeInformation (Join-Path $BaselineDir 'oracle-content-offenders.csv')
}

if ($content.Count -eq 0) {
    Write-Host "RESULT: PASS  (no content differences -> replay is correct)"
    exit 0
} else {
    Write-Host "RESULT: FAIL  ($($content.Count) content differences -> under/over-build or nondeterminism)"
    exit 1
}
