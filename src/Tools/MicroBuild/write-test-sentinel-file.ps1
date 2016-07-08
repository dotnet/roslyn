param(
  [string] $binariesPath = $(throw "Need a binaries path")
)

# You can write your powershell scripts inline here. 
# You can also pass predefined and custom variables to this scripts using arguments

$sentinelFile = Join-Path $binariesPath AllTestsPassed.sentinel
New-Item -Force $sentinelFile -type file
