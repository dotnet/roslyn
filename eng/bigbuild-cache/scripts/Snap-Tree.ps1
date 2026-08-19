param(
    [Parameter(Mandatory)][string[]]$Roots,
    [Parameter(Mandatory)][string]$Out,
    [int]$Throttle = 16
)
$ErrorActionPreference = 'Stop'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$files = @(Get-ChildItem $Roots -Recurse -File -ErrorAction SilentlyContinue)
Write-Host ("[snap] {0} files; hashing with {1} threads..." -f $files.Count, $Throttle)

$results = $files | ForEach-Object -Parallel {
    try {
        $h = [System.Security.Cryptography.SHA256]::Create()
        $fs = [System.IO.File]::Open($_.FullName, 'Open', 'Read', 'ReadWrite')
        try {
            $hash = $h.ComputeHash($fs)
        } finally {
            $fs.Dispose(); $h.Dispose()
        }
        [pscustomobject]@{
            Path  = $_.FullName
            Sha   = [BitConverter]::ToString($hash).Replace('-','').ToLowerInvariant()
            Mtime = $_.LastWriteTimeUtc.Ticks
            Size  = $_.Length
        }
    } catch {
        # File may have been removed during enumeration
    }
} -ThrottleLimit $Throttle

$results | Export-Clixml -Path $Out -Depth 2
$sw.Stop()
Write-Host ("[snap] wrote {0} entries to {1} in {2}" -f $results.Count, $Out, $sw.Elapsed)
