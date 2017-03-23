# Run GitLink on the binaries that we are producing.

param([string]$config = "Release")
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

try {
    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")

    $configDir = Join-Path $binariesDir $config
    $gitlinkVersion = Get-PackageVersion "GitLink"
    $gitlink = Join-Path (Get-PackagesDir) "GitLink\$gitlinkVersion\build\GitLink.exe"

    Write-Host "Running GitLink"

    $config = Join-Path $repoDir "build\config\SignToolData.json"
    $j = ConvertFrom-Json (Get-Content -raw $config)
    foreach ($entry in $j.sign) {
        foreach ($v in $entry.values) { 
            $ext = [IO.Path]::GetExtension($v)
            if (($ext -eq ".dll") -or ($ext -eq ".exe")) {
                $name = [IO.Path]::ChangeExtension($v, ".pdb")
                $pdbPath = Join-Path $configDir $name
                Write-Host "`t$pdbPath"

                $output = & $gitlink -u "https://github.com/dotnet/roslyn" $pdbPath
                if (-not $?) {
                    Write-Host "Error!!!"
                    Write-Host $output
                    exit 1
                }
            }
        }
    }

    exit 0
}
catch [exception] {
    write-host $_.Exception
    exit 1
}
