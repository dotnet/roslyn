param (
    [string]$nugetVersion = $(throw "Need a nuget version"),
    [string]$destDir = $(throw "Need a path to download too"),
    [string]$binariesDir = $(throw "Need path to Binaries directory"))
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    $scratchDir = join-path $binariesDir "NuGet"
    if (-not (test-path $scratchDir)) {
        mkdir $scratchDir | out-null
    }

    if (-not (test-path $destDir)) {
        mkdir $destDir | out-null
    }

    $destFile = join-path $destDir "NuGet.exe"
    $scratchFile = join-path $scratchDir "NuGet.exe"
    $versionFile = join-path $scratchDir "version.txt"

    # Check and see if we already have a NuGet.exe which exists and is the correct
    # version.
    if ((test-path $destFile) -and (test-path $scratchFile) -and (test-path $versionFile)) {
        $destHash = (get-filehash $destFile -algorithm MD5).Hash
        $scratchHash = (get-filehash $scratchFile -algorithm MD5).Hash
        $scratchVersion = gc $versionFile
        if (($destHash -eq $scratchHash) -and ($scratchVersion -eq $nugetVersion)) {
            write-host "Using existing NuGet.exe at version $nuGetVersion"
            exit 0
        }
    }

    write-host "Downloading NuGet.exe"
    $webClient = New-Object -TypeName "System.Net.WebClient"
    $webClient.DownloadFile("https://dist.nuget.org/win-x86-commandline/v$nugetVersion/NuGet.exe", $scratchFile)
    $nugetVersion | out-file $versionFile
    cp $scratchFile $destFile
    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
