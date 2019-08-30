[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function MarkShipped([string]$dir) {
    $shippedFilePath = Join-Path $dir "PublicAPI.Shipped.txt"
    $shipped = Get-Content $shippedFilePath
    $unshippedFilePath = Join-Path $dir "PublicAPI.Unshipped.txt"
    $unshipped = Get-Content $unshippedFilePath
    $removed = @()
    $removedPrefix = "*REMOVED*";
    Write-Host "Processing $dir"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            if ($item.StartsWith($removedPrefix)) {
                $item = $item.Substring($removedPrefix.Length)
                $removed += $item
            }
            else {
                $shipped += $item
            }
        }
    }

    $shipped | Sort-Object | ?{ -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    "" | Out-File $unshippedFilePath -Encoding Ascii
}

try {
    . (Join-Path $PSScriptRoot "..\..\eng\build-utils.ps1")
    Push-Location $RepoRoot

    foreach ($file in Get-ChildItem -re -in "PublicApi.Shipped.txt") {
        $dir = Split-Path -parent $file
        MarkShipped $dir
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
finally {
    Pop-Location
}
