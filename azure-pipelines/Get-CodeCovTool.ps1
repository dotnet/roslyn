<#
.SYNOPSIS
    Downloads the CodeCov.io uploader tool and returns the path to it.
#>
[CmdletBinding()]
Param(
)

$toolsPath = & "$PSScriptRoot\Get-TempToolsPath.ps1"
$binaryToolsPath = Join-Path $toolsPath codecov
if (!(Test-Path $binaryToolsPath)) { $null = mkdir $binaryToolsPath }

if ($IsMacOS) {
    $codeCovPath = Join-Path $binaryToolsPath codecov
    $codeCovUrl = "https://uploader.codecov.io/latest/macos/codecov"
}
elseif ($IsLinux) {
    $codeCovPath = Join-Path $binaryToolsPath codecov
    $codeCovUrl = "https://uploader.codecov.io/latest/linux/codecov"
}
else {
    $codeCovPath = Join-Path $binaryToolsPath codecov.exe
    $codeCovUrl = "https://uploader.codecov.io/latest/windows/codecov.exe"
}

if (!(Test-Path $codeCovPath)) {
    Write-Host "Downloading latest codecov upload tool..." -ForegroundColor Yellow
    (New-Object System.Net.WebClient).DownloadFile($codeCovUrl, $codeCovPath)
}

if ($IsMacOS -or $IsLinux) {
    chmod u+x $codeCovPath
}

return (Resolve-Path $codeCovPath).Path
