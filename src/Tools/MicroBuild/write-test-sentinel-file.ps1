param(
  [string] $binariesPath = ""
)

if ($binariesPath -eq "") {
    write-host "Need a binaries path"
    exit 1
}

$sentinelFile = Join-Path $binariesPath AllTestsPassed.sentinel
New-Item -Force $sentinelFile -type file
