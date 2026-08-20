<#
.SYNOPSIS
  Apply the capture/replay-cache mechanism to a repo's tracked inputs. Part of the
  BigBuild caching pilot; see bigbuild/capture-replay-cache-design.md.

.DESCRIPTION
  Two modes:

    -Mode Simulate : set every tracked input file's mtime to now. Reproduces the
                     fresh-clone condition (inputs newer than the overlaid/existing
                     outputs), under which MSBuild's timestamp up-to-date check would
                     rebuild everything. This is the control that BackDate reverses.

    -Mode BackDate : the actual mechanism. Hash each tracked input and compare to the
                     baseline input manifest:
                       - match    -> back-date mtime to the captured (old) value, older
                                     than the outputs it feeds, so the target is skipped.
                       - changed  -> set mtime to now, so the target and its dependents
                                     rebuild.
                       - new      -> set mtime to now (rebuild).
                       - deleted  -> counted (orphan-cleanup is a v1 non-goal).

  Content decides equality; the mtime touch translates that decision into the timestamp
  domain stock MSBuild actually checks. No engine change.
#>
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$BaselineDir,
    [Parameter(Mandatory)][ValidateSet('Simulate', 'BackDate')][string]$Mode,
    # Force the content-hash classification (SHA256 every input vs the baseline manifest) instead
    # of the default git-diff fast path. Only needed for a baseline captured with -HashInputs, or
    # to operate without git.
    [switch]$HashInputs,
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path $RepoRoot).Path

$tracked = @(git -C $RepoRoot ls-files) | Where-Object { $_ }
$now = Get-Date

if ($Mode -eq 'Simulate') {
    $n = 0
    foreach ($rel in $tracked) {
        $full = Join-Path $RepoRoot ($rel -replace '/', '\')
        if (Test-Path -LiteralPath $full) { (Get-Item -LiteralPath $full -Force).LastWriteTime = $now; $n++ }
    }
    Write-Host "[replay:Simulate] set $n tracked inputs to now (fresh-clone condition)"
    return
}

# The pipeline-artifact round-trip (zip on publish, unzip on download) does NOT preserve mtimes,
# so the overlaid outputs -- and generated obj "inputs" a restore step refreshes (*.nuget.g.props,
# XlfSources.txt, ...) -- can end up NEWER than their output dlls, forcing a full rebuild even
# though nothing changed. So do not trust preserved mtimes: explicitly stamp the entire overlaid
# output tree to a uniform old time, and back-date unchanged inputs strictly below it. A captured
# baseline is internally consistent (everything was up-to-date when captured), so a single uniform
# output timestamp keeps every project up-to-date; changed inputs go to 'now' so their targets rebuild.
$now = Get-Date
$meta = Get-Content (Join-Path $BaselineDir 'meta.json') -Raw | ConvertFrom-Json
$outputRoots = @($meta.OutputRoots)
$tOut = $now.AddDays(-2)
$tIn = $now.AddDays(-3)

# 1) Stamp the overlaid output tree uniformly (robust to the artifact round-trip resetting mtimes).
$stamped = 0
foreach ($r in $outputRoots) {
    $rootPath = Join-Path $RepoRoot $r
    if (-not (Test-Path -LiteralPath $rootPath)) { continue }
    $files = @(Get-ChildItem -LiteralPath $rootPath -Recurse -File -Force -ErrorAction SilentlyContinue)
    $files | ForEach-Object -Parallel { try { $_.LastWriteTime = $using:tOut } catch { } } -ThrottleLimit $Throttle
    $stamped += $files.Count
}
Write-Host "[replay:BackDate] stamped $stamped output files to $tOut"

# 2) Classify inputs and back-date. Fast path: let git decide changed-vs-unchanged.
$baseSha = $meta.Sha
$haveBase = $false
if ($baseSha) { git -C $RepoRoot cat-file -e "$baseSha^{commit}" 2>$null; $haveBase = ($LASTEXITCODE -eq 0) }

if (-not $HashInputs -and $haveBase) {
    # `git diff --name-only <baseSha>` lists every TRACKED file whose current WORKING-TREE content
    # differs from the baseline commit (committed and uncommitted alike). Files not listed are
    # byte-identical (git blob identity), so they back-date; listed files stay newer and rebuild.
    $changed = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($rel in (git -c core.quotepath=false -C $RepoRoot diff --name-only $baseSha)) {
        if ($rel) { [void]$changed.Add($rel) }
    }
    $changedList = [System.Collections.Generic.List[string]]::new()
    $matchedList = [System.Collections.Generic.List[string]]::new()
    foreach ($rel in $tracked) {
        $full = Join-Path $RepoRoot ($rel -replace '/', '\')
        if (-not (Test-Path -LiteralPath $full)) { continue }
        if ($changed.Contains($rel)) { $changedList.Add($full) } else { $matchedList.Add($full) }
    }
    $matchedList | ForEach-Object -Parallel { try { $_.LastWriteTime = $using:tIn } catch { } } -ThrottleLimit $Throttle
    $changedList | ForEach-Object -Parallel { try { $_.LastWriteTime = $using:now } catch { } } -ThrottleLimit $Throttle
    "[replay:BackDate/git] matched(back-dated)={0} changed={1} (baseline {2})" -f $matchedList.Count, $changedList.Count, $baseSha | Write-Host
    return
}

if (-not $haveBase) {
    Write-Host "[replay:BackDate] baseline commit '$baseSha' not resolvable here; using content-hash path"
}

# Fallback / -HashInputs: content-hash classification. Hash each tracked input and compare to the
# baseline input manifest (Sha per repo-relative path).
$base = @{}
Import-Clixml (Join-Path $BaselineDir 'inputs.clixml') | ForEach-Object { $base[$_.Rel] = $_ }

$results = $tracked | ForEach-Object -Parallel {
    $rel = $_
    $full = Join-Path $using:RepoRoot ($rel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $full)) { return [pscustomobject]@{ Cls = 'missing'; Rel = $rel } }
    $b = ($using:base)[$rel]
    try {
        $h = [System.Security.Cryptography.SHA256]::Create()
        $fs = [System.IO.File]::Open($full, 'Open', 'Read', 'ReadWrite')
        try { $hash = $h.ComputeHash($fs) } finally { $fs.Dispose(); $h.Dispose() }
        $sha = [BitConverter]::ToString($hash).Replace('-', '').ToLowerInvariant()
    } catch { return [pscustomobject]@{ Cls = 'error'; Rel = $rel } }

    if ($null -eq $b) { return [pscustomobject]@{ Cls = 'new'; Rel = $rel; Full = $full } }
    if ($sha -eq $b.Sha) { return [pscustomobject]@{ Cls = 'match'; Rel = $rel; Full = $full } }
    return [pscustomobject]@{ Cls = 'changed'; Rel = $rel; Full = $full }
} -ThrottleLimit $Throttle

foreach ($r in $results) {
    switch ($r.Cls) {
        'match'   { (Get-Item -LiteralPath $r.Full -Force).LastWriteTime = $tIn }   # unchanged -> below outputs
        'changed' { (Get-Item -LiteralPath $r.Full -Force).LastWriteTime = $now }   # rebuild
        'new'     { (Get-Item -LiteralPath $r.Full -Force).LastWriteTime = $now }   # rebuild
    }
}

$g = $results | Group-Object Cls -AsHashTable -AsString
# Group-Object -AsHashTable omits absent classes; @($null).Count is 1 in PowerShell, so
# guard each lookup or empty classes misreport as 1.
function Get-Count($h, $k) { if ($h -and $h.ContainsKey($k)) { @($h[$k]).Count } else { 0 } }
$deleted = @($base.Keys | Where-Object { -not (Test-Path -LiteralPath (Join-Path $RepoRoot ($_ -replace '/', '\'))) }).Count
"[replay:BackDate] matched(back-dated)={0} changed={1} new={2} deleted={3}" -f `
    (Get-Count $g 'match'), (Get-Count $g 'changed'), (Get-Count $g 'new'), $deleted | Write-Host
