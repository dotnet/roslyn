# Create the PerfTests directory under Binaries\$(Configuration).  There are still a number
# of tools (in roslyn and roslyn-internal) that depend on this combined directory.
param ([string]$script:binDir = $(throw "Need the binaries directory"))
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    [string]$target = join-path $binDir "PerfTests"
    write-host "PerfTests: $target"
    if (-not (test-path $target)) {
        mkdir $target | out-null
    }

    pushd $bindir
    foreach ($subDir in @("Dlls", "UnitTests")) {
        pushd $subDir
        foreach ($path in gci -re -in "PerfTests") {
            write-host "`tcopying $path"
            copy -force -recurse "$path\*" $target
        }
        popd
    }
    popd
    exit 0
}
catch
{
    write-host "Error: $($_.Exception.Message)"
    exit 1
}

