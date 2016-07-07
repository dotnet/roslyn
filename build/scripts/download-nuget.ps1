param (
    [string]$nugetVersion = $(throw "Need a nuget version"),
    [string]$destPath = $(throw "Need a path to download too"))
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

if (-not (test-path $destPath)) {
    mkdir $destPath | out-null
}

$webClient = New-Object -TypeName "System.Net.WebClient"
$webClient.DownloadFile("https://dist.nuget.org/win-x86-commandline/v$nugetVersion/NuGet.exe", (join-path $destPath "NuGet.exe"))
