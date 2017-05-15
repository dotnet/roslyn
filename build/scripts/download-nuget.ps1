param ([string]$nugetVersion = "4.1.0")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $scratchDir = Join-Path $binariesDir "NuGet"
    Create-Directory $scratchDir

    $destFile = Join-Path $repoDir "NuGet.exe"
    $scratchFile = Join-Path $scratchDir "NuGet.exe"
    $versionFile = Join-Path $scratchDir "version.txt"

    # Check and see if we already have a NuGet.exe which exists and is the correct
    # version.
    if ((Test-Path $destFile) -and (Test-Path $scratchFile) -and (Test-Path $versionFile)) {
        $destHash = (Get-FileHash $destFile -algorithm MD5).Hash
        $scratchHash = (Get-FileHash $scratchFile -algorithm MD5).Hash
        $scratchVersion = Get-Content $versionFile
        if (($destHash -eq $scratchHash) -and ($scratchVersion -eq $nugetVersion)) {
            Write-Host "Using existing NuGet.exe at version $nuGetVersion"
            exit 0
        }
    }

    Write-Host "Downloading NuGet.exe"
    $webClient = New-Object -TypeName "System.Net.WebClient"
    $webClient.DownloadFile("https://dist.nuget.org/win-x86-commandline/v$nugetVersion/NuGet.exe", $scratchFile)
    $nugetVersion | Out-File $versionFile
    Copy-Item $scratchFile $destFile
    exit 0
}
catch [exception] {
    Write-Host $_.Exception
    exit 1
}
