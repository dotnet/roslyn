# This script will ensure that the MSBuild toolset of the specified 
# version is downloaded and deployed to Binaries\Toolset\MSBuild
#
# This is a no-op if it is already present at the appropriate version

[CmdletBinding(PositionalBinding=$false)]
param ($version = "0.1.7-alpha")

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    $toolsDir = Join-Path $binariesDir "Toolset"
    $scratchDir = Join-Path $toolsDir "Scratch"

    Create-Directory $toolsDir
    Create-Directory $scratchDir
    Push-Location $toolsDir

    $scratchFile = Join-Path $scratchDir "msbuild.txt"
    $scratchVersion = Get-Content -raw $scratchFile -ErrorAction SilentlyContinue
    if ($scratchVersion -eq ($version + [Environment]::NewLine)) { 
        exit 0
    }

    $tempFileName = "msbuild-$version.zip"
    $tempFile = Join-Path $scratchDir $tempFileName
    if (-not (Test-Path $tempFile)) { 
        $url = "https://jdashstorage.blob.core.windows.net/msbuild/msbuild-$version.zip" 
        $webClient = New-Object -TypeName "System.Net.WebClient"
        $webClient.DownloadFile($url, $tempFile)
    }
    
    Remove-Item -re (Join-Path $toolsDir "msbuild") -ErrorAction SilentlyContinue
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($tempFile, $toolsDir)

    $version | Out-File $scratchFile 
    exit 0
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}
