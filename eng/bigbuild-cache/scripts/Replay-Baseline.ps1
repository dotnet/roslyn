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

# BackDate: load the baseline input manifest keyed by repo-relative path.
$base = @{}
Import-Clixml (Join-Path $BaselineDir 'inputs.clixml') | ForEach-Object { $base[$_.Rel] = $_ }

# Guarantee the back-date target is older than the oldest output.
$outMin = (Import-Clixml (Join-Path $BaselineDir 'outputs.clixml') | Measure-Object -Property Mtime -Minimum).Minimum
$floor = [DateTime]::FromFileTimeUtc(0)
if ($outMin) { $floor = ([DateTime]::new([long]$outMin, [DateTimeKind]::Utc)).AddHours(-1) }

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
    if ($sha -eq $b.Sha) { return [pscustomobject]@{ Cls = 'match'; Rel = $rel; Full = $full; Mtime = $b.Mtime } }
    return [pscustomobject]@{ Cls = 'changed'; Rel = $rel; Full = $full }
} -ThrottleLimit $Throttle

$now = Get-Date
$floorTicks = $floor.ToUniversalTime().Ticks
foreach ($r in $results) {
    switch ($r.Cls) {
        'match' {
            $t = [long]$r.Mtime
            if ($t -ge $floorTicks) { $t = $floorTicks }   # keep strictly older than outputs
            (Get-Item -LiteralPath $r.Full -Force).LastWriteTimeUtc = [DateTime]::new($t, [DateTimeKind]::Utc)
        }
        'changed' { (Get-Item -LiteralPath $r.Full -Force).LastWriteTime = $now }
        'new'     { (Get-Item -LiteralPath $r.Full -Force).LastWriteTime = $now }
    }
}

$g = $results | Group-Object Cls -AsHashTable -AsString
# Group-Object -AsHashTable omits absent classes; @($null).Count is 1 in PowerShell, so
# guard each lookup or empty classes misreport as 1.
function Get-Count($h, $k) { if ($h -and $h.ContainsKey($k)) { @($h[$k]).Count } else { 0 } }
$deleted = @($base.Keys | Where-Object { -not (Test-Path -LiteralPath (Join-Path $RepoRoot ($_ -replace '/', '\'))) }).Count
"[replay:BackDate] matched(back-dated)={0} changed={1} new={2} deleted={3}" -f `
    (Get-Count $g 'match'), (Get-Count $g 'changed'), (Get-Count $g 'new'), $deleted | Write-Host
