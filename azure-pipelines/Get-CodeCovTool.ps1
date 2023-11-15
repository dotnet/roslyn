<#
.SYNOPSIS
    Downloads the CodeCov.io uploader tool and returns the path to it.
.PARAMETER AllowSkipVerify
    Allows skipping signature verification of the downloaded tool if gpg is not installed.
#>
[CmdletBinding()]
Param(
    [switch]$AllowSkipVerify
)

if ($IsMacOS) {
    $codeCovUrl = "https://uploader.codecov.io/latest/macos/codecov"
    $toolName = 'codecov'
}
elseif ($IsLinux) {
    $codeCovUrl = "https://uploader.codecov.io/latest/linux/codecov"
    $toolName = 'codecov'
}
else {
    $codeCovUrl = "https://uploader.codecov.io/latest/windows/codecov.exe"
    $toolName = 'codecov.exe'
}

$shaSuffix = ".SHA256SUM"
$sigSuffix = $shaSuffix + ".sig"

Function Get-FileFromWeb([Uri]$Uri, $OutDir) {
    $OutFile = Join-Path $OutDir $Uri.Segments[-1]
    if (!(Test-Path $OutFile)) {
        Write-Verbose "Downloading $Uri..."
        if (!(Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
        try {
            (New-Object System.Net.WebClient).DownloadFile($Uri, $OutFile)
        } finally {
            # This try/finally causes the script to abort
        }
    }

    $OutFile
}

$toolsPath = & "$PSScriptRoot\Get-TempToolsPath.ps1"
$binaryToolsPath = Join-Path $toolsPath codecov
$testingPath = Join-Path $binaryToolsPath unverified
$finalToolPath = Join-Path $binaryToolsPath $toolName

if (!(Test-Path $finalToolPath)) {
    if (Test-Path $testingPath) {
        Remove-Item -Recurse -Force $testingPath # ensure we download all matching files
    }
    $tool = Get-FileFromWeb $codeCovUrl $testingPath
    $sha = Get-FileFromWeb "$codeCovUrl$shaSuffix" $testingPath
    $sig = Get-FileFromWeb "$codeCovUrl$sigSuffix" $testingPath
    $key = Get-FileFromWeb https://keybase.io/codecovsecurity/pgp_keys.asc $testingPath

    if ((Get-Command gpg -ErrorAction SilentlyContinue)) {
        Write-Host "Importing codecov key" -ForegroundColor Yellow
        gpg --import $key
        Write-Host "Verifying signature on codecov hash" -ForegroundColor Yellow
        gpg --verify $sig $sha
    } else {
        if ($AllowSkipVerify) {
            Write-Warning "gpg not found. Unable to verify hash signature."
        } else {
            throw "gpg not found. Unable to verify hash signature. Install gpg or add -AllowSkipVerify to override."
        }
    }

    Write-Host "Verifying hash on downloaded tool" -ForegroundColor Yellow
    $actualHash = (Get-FileHash -Path $tool -Algorithm SHA256).Hash
    $expectedHash = (Get-Content $sha).Split()[0]
    if ($actualHash -ne $expectedHash) {
        # Validation failed. Delete the tool so we can't execute it.
        #Remove-Item $codeCovPath
        throw "codecov uploader tool failed signature validation."
    }

    Copy-Item $tool $finalToolPath

    if ($IsMacOS -or $IsLinux) {
        chmod u+x $finalToolPath
    }
}

return $finalToolPath
