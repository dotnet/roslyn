param(
    [Parameter(Mandatory)][string[]]$Roots,
    [Parameter(Mandatory)][string]$Out,
    # Optional stat-cache: a prior Snap-Tree manifest (.clixml). Any file whose (Mtime,Size)
    # still match its prior entry is assumed unchanged and its stored Sha is reused WITHOUT
    # re-hashing. A build always rewrites (and re-timestamps) an output it regenerates, so an
    # unchanged mtime reliably implies unchanged content for build outputs; size is a second
    # guard. This turns the warm-case snapshot from "hash everything" into "hash only the few
    # files the build actually touched". Keyed on absolute Path, stable when the checkout path
    # is pinned (the warm-clone assumption); a mismatch just falls through to a full hash, so it
    # only ever costs perf, never correctness.
    [string]$PriorManifest = '',
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$files = @(Get-ChildItem $Roots -Recurse -File -ErrorAction SilentlyContinue)

# Build the stat-cache lookup (Path -> prior entry) from the prior manifest, if supplied.
$prior = @{}
if ($PriorManifest -and (Test-Path $PriorManifest)) {
    foreach ($e in @(Import-Clixml -Path $PriorManifest)) {
        if ($e.Path) { $prior[$e.Path] = $e }
    }
    Write-Host ("[snap] stat-cache: {0} prior entries from {1}" -f $prior.Count, $PriorManifest)
}
Write-Host ("[snap] {0} files; hashing with {1} threads (stat-cache {2})..." -f `
    $files.Count, $Throttle, $(if ($prior.Count) { 'on' } else { 'off' }))

$results = $files | ForEach-Object -Parallel {
    $priorTable = $using:prior
    try {
        $mtime = $_.LastWriteTimeUtc.Ticks
        $size  = $_.Length
        # Cheap check first: prior snapshot has this exact (Path,Mtime,Size) -> reuse its Sha.
        $p = $priorTable[$_.FullName]
        if ($null -ne $p -and $p.Mtime -eq $mtime -and $p.Size -eq $size) {
            [pscustomobject]@{ Path = $_.FullName; Sha = $p.Sha; Mtime = $mtime; Size = $size; Reused = $true }
        }
        else {
            # Expensive check: changed / new / no prior -> hash it.
            $h = [System.Security.Cryptography.SHA256]::Create()
            $fs = [System.IO.File]::Open($_.FullName, 'Open', 'Read', 'ReadWrite')
            try {
                $hash = $h.ComputeHash($fs)
            } finally {
                $fs.Dispose(); $h.Dispose()
            }
            [pscustomobject]@{
                Path   = $_.FullName
                Sha    = [BitConverter]::ToString($hash).Replace('-','').ToLowerInvariant()
                Mtime  = $mtime
                Size   = $size
                Reused = $false
            }
        }
    } catch {
        # File may have been removed during enumeration
    }
} -ThrottleLimit $Throttle

$results | Export-Clixml -Path $Out -Depth 2
$sw.Stop()
$reused = @($results | Where-Object { $_.Reused }).Count
$hashed = $results.Count - $reused
Write-Host ("[snap] wrote {0} entries to {1} in {2} (reused {3}, hashed {4})" -f `
    $results.Count, $Out, $sw.Elapsed, $reused, $hashed)
