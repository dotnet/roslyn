param(
    [Parameter(Mandatory)][string]$Cold,
    [Parameter(Mandatory)][string]$Warm,
    [string]$OutDir = $null
)
$ErrorActionPreference = 'Stop'
$c = @{}
Import-Clixml $Cold | ForEach-Object { $c[$_.Path] = $_ }
$w = @{}
Import-Clixml $Warm | ForEach-Object { $w[$_.Path] = $_ }

$added   = @()
$removed = @()
$content = @()  # different Sha (real rebuild)
$mtime   = @()  # same Sha, different Mtime (cascade trigger)

foreach ($k in $w.Keys) {
    if (-not $c.ContainsKey($k)) { $added += $w[$k]; continue }
    $a = $c[$k]; $b = $w[$k]
    if ($a.Sha -ne $b.Sha)           { $content += [pscustomobject]@{ Path=$k; SizeCold=$a.Size; SizeWarm=$b.Size; ShaCold=$a.Sha; ShaWarm=$b.Sha }; continue }
    if ($a.Mtime -ne $b.Mtime)       { $mtime   += [pscustomobject]@{ Path=$k; Size=$b.Size;     Sha=$b.Sha } }
}
foreach ($k in $c.Keys) { if (-not $w.ContainsKey($k)) { $removed += $c[$k] } }

"=========================================================="
"SUMMARY"
"=========================================================="
"cold files       : $($c.Count)"
"warm files       : $($w.Count)"
"added (warm only): $($added.Count)"
"removed (cold only): $($removed.Count)"
"content changes  : $($content.Count)"
"mtime-only       : $($mtime.Count)   <-- cascade triggers"
""

"=========================================================="
"CASCADE TRIGGERS (mtime-only, by extension)"
"=========================================================="
$mtime | Group-Object { [IO.Path]::GetExtension($_.Path).ToLowerInvariant() } | Sort-Object Count -Descending | Format-Table Count, Name -AutoSize

"CASCADE TRIGGERS (by basename suffix; last 2 path segments)"
$mtime | Group-Object {
    $p = $_.Path
    $segs = $p -split '[\\/]+'
    $tail = if ($segs.Count -ge 2) { "$($segs[-2])\$($segs[-1])" } else { $segs[-1] }
    # collapse digits and guid-like tokens to make patterns aggregate
    $tail = $tail -replace '\d{2,}','#' -replace '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}','GUID'
    $tail
} | Sort-Object Count -Descending | Select-Object -First 30 | Format-Table Count, Name -AutoSize

"CASCADE TRIGGERS (by project — assembly/config in path)"
$mtime | Group-Object {
    $p = $_.Path
    if ($p -match '\\artifacts\\(?:bin|obj)\\([^\\]+)\\') { $matches[1] } else { '<other>' }
} | Sort-Object Count -Descending | Select-Object -First 20 | Format-Table Count, Name -AutoSize

"CONTENT CHANGES (different Sha — true rebuilds)"
if ($content.Count -gt 0) {
    $content | Group-Object { [IO.Path]::GetExtension($_.Path).ToLowerInvariant() } | Sort-Object Count -Descending | Format-Table Count, Name -AutoSize
    "Top 30 content changes:"
    $content | Select-Object -First 30 | Format-Table Path, SizeCold, SizeWarm -AutoSize
}

"ADDED files (top 20):"
$added | Select-Object -First 20 | ForEach-Object { $_.Path }
"REMOVED files (top 20):"
$removed | Select-Object -First 20 | ForEach-Object { $_.Path }

if ($OutDir) {
    New-Item -ItemType Directory -Force $OutDir | Out-Null
    $mtime   | Export-Csv -NoTypeInformation "$OutDir\cascade-triggers.csv"
    $content | Export-Csv -NoTypeInformation "$OutDir\content-changes.csv"
    $added   | Export-Csv -NoTypeInformation "$OutDir\added.csv"
    $removed | Export-Csv -NoTypeInformation "$OutDir\removed.csv"
    "wrote CSVs to $OutDir"
}
