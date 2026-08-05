#requires -Version 7.0

param(
    [switch]$SkipCompile,
    [string]$GhAwVersion = 'v0.80.9'
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$generatedPaths = @(
    '.github/aw',
    '.github/workflows/*.lock.yml',
    '.github/workflows/agentic_commands.yml',
    '.github/workflows/agentics-maintenance.yml'
)

$ghAwChecksums = @{
    'v0.80.9/darwin-amd64' = '8d89c10de8c2e08e98953c4f46e0ff97cad407a124d03f06cad77ebdc260a696'
    'v0.80.9/darwin-arm64' = '5799be001beb518c14919c7c906f8842052fad367b79c6aa934b6079826b1b79'
    'v0.80.9/linux-386' = '3ccdc5eaad6e32b2705d61829b63b5a41b6f23a239793b724ca5762e6d088726'
    'v0.80.9/linux-amd64' = '22373e1530f13f1c491a968fc293deda53ff254822be16ab345efc6f4d33fbe5'
    'v0.80.9/linux-arm' = '8df37d15e4d627410a6200baeff9fb61dcf6df96ef3a6ccc90048b5ff1a23e27'
    'v0.80.9/linux-arm64' = '65ea14f7dc6b5a4f4fee3fc588d931ed7ffebbf5b955b213eb92251f0c807a70'
    'v0.80.9/windows-amd64.exe' = 'f94d418d8579e9dc59cce98f052561ff95ed024d897ff58f1b26857ee690b45b'
    'v0.80.9/windows-arm64.exe' = '6c226f96abe02fbf0014704d5b94fe0093db61b8a9c9c34a233afd1b74daac03'
}

function Get-GhAwAssetName {
    $architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()

    if ($IsWindows) {
        switch ($architecture) {
            'Arm64' { return 'windows-arm64.exe' }
            default { return 'windows-amd64.exe' }
        }
    }

    if ($IsMacOS) {
        if ($architecture -eq 'Arm64') {
            return 'darwin-arm64'
        }

        return 'darwin-amd64'
    }

    switch ($architecture) {
        'Arm64' { return 'linux-arm64' }
        'Arm' { return 'linux-arm' }
        'X86' { return 'linux-386' }
        default { return 'linux-amd64' }
    }
}

function Assert-GhAwChecksum {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$AssetName
    )

    $checksumKey = "$GhAwVersion/$AssetName"
    $expectedChecksum = $ghAwChecksums[$checksumKey]
    if ([string]::IsNullOrWhiteSpace($expectedChecksum)) {
        throw "No pinned checksum is registered for gh-aw asset '$checksumKey'."
    }

    $actualChecksum = (Get-FileHash -Algorithm SHA256 -Path $Path).Hash.ToLowerInvariant()
    if ($actualChecksum -ne $expectedChecksum) {
        throw "Downloaded gh-aw asset '$checksumKey' failed SHA256 verification. Expected $expectedChecksum, got $actualChecksum."
    }
}

function Get-GhAwExecutable {
    $assetName = Get-GhAwAssetName
    $installDirectory = Join-Path $repoRoot 'artifacts\tools\gh-aw'
    $destinationFileName = if ($IsWindows) { 'gh-aw.exe' } else { 'gh-aw' }
    $destination = Join-Path $installDirectory $destinationFileName

    if (Test-Path $destination) {
        Assert-GhAwChecksum -Path $destination -AssetName $assetName
        return (Resolve-Path $destination).Path
    }

    New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null

    $uri = "https://github.com/github/gh-aw/releases/download/$GhAwVersion/$assetName"
    Write-Host "Downloading gh-aw $GhAwVersion from $uri"
    Invoke-WebRequest -Uri $uri -OutFile $destination
    Assert-GhAwChecksum -Path $destination -AssetName $assetName

    if (-not $IsWindows) {
        chmod +x $destination
    }

    return (Resolve-Path $destination).Path
}

function Invoke-GhAwCompile {
    if ($SkipCompile) {
        Write-Host 'Skipping gh-aw compile because -SkipCompile was specified.'
        return
    }

    Write-Host 'Running gh-aw compile --validate --no-check-update...'
    $ghAwExecutable = Get-GhAwExecutable

    if ($IsWindows) {
        $stdout = [System.IO.Path]::GetTempFileName()
        $stderr = [System.IO.Path]::GetTempFileName()

        try {
            $process = Start-Process `
                -FilePath $ghAwExecutable `
                -ArgumentList @('compile', '--validate', '--no-check-update') `
                -NoNewWindow `
                -RedirectStandardOutput $stdout `
                -RedirectStandardError $stderr `
                -PassThru `
                -Wait

            Get-Content -Path $stdout -ErrorAction SilentlyContinue | Write-Host
            Get-Content -Path $stderr -ErrorAction SilentlyContinue | Write-Host

            if ($process.ExitCode -ne 0) {
                throw "gh-aw compile failed with exit code $($process.ExitCode)."
            }
        }
        finally {
            Remove-Item -Path $stdout, $stderr -ErrorAction SilentlyContinue
        }

        return
    }

    & $ghAwExecutable compile --validate --no-check-update
}

function Get-GeneratedDiff {
    return (& git diff --binary -- $generatedPaths) -join [Environment]::NewLine
}

function Get-UntrackedGeneratedFiles {
    return (& git ls-files --others --exclude-standard -- $generatedPaths)
}

function Assert-GeneratedFilesUnchanged {
    Write-Host 'Checking that generated Agentic Workflow files are up to date...'

    $diff = Get-GeneratedDiff
    $untrackedFiles = @(Get-UntrackedGeneratedFiles)

    if (-not [string]::IsNullOrEmpty($diff) -or $untrackedFiles.Length -ne 0) {
        if (-not [string]::IsNullOrEmpty($diff)) {
            $diff | Write-Host
        }

        if ($untrackedFiles.Length -ne 0) {
            Write-Host 'Untracked generated Agentic Workflow files:'
            $untrackedFiles | ForEach-Object { Write-Host "  $_" }
        }

        throw 'Agentic Workflow generated files are stale. Run gh-aw compile --validate --no-check-update and commit the resulting changes.'
    }
}

Push-Location $repoRoot
try {
    Invoke-GhAwCompile
    Assert-GeneratedFilesUnchanged

    Write-Host 'Agentic Workflow generated files are up to date.'
}
finally {
    Pop-Location
}
