<#
.SYNOPSIS
    Collect the list of PDBs built in this repo.
.PARAMETER Path
    The directory to recursively search for PDBs.
.PARAMETER Tests
    A switch indicating to find PDBs only for test binaries instead of only for shipping shipping binaries.
#>
[CmdletBinding()]
param (
    [parameter(Mandatory=$true)]
    [string]$Path,
    [switch]$Tests
)

$ActivityName = "Collecting symbols from $Path"
Write-Progress -Activity $ActivityName -CurrentOperation "Discovery PDB files"
$PDBs = Get-ChildItem -rec "$Path/*.pdb"

# Filter PDBs to product OR test related.
$testregex = "unittest|tests|\.test\."

Write-Progress -Activity $ActivityName -CurrentOperation "De-duplicating symbols"
$PDBsByHash = @{}
$i = 0
$PDBs |% {
    Write-Progress -Activity $ActivityName -CurrentOperation "De-duplicating symbols" -PercentComplete (100 * $i / $PDBs.Length)
    $hash = Get-FileHash $_
    $i++
    Add-Member -InputObject $_ -MemberType NoteProperty -Name Hash -Value $hash.Hash
    Write-Output $_
} | Sort-Object CreationTime |% {
    # De-dupe based on hash. Prefer the first match so we take the first built copy.
    if (-not $PDBsByHash.ContainsKey($_.Hash)) {
        $PDBsByHash.Add($_.Hash, $_.FullName)
        Write-Output $_
    }
} |? {
    if ($Tests) {
        $_.FullName -match $testregex
    } else {
        $_.FullName -notmatch $testregex
    }
} |% {
    # Collect the DLLs/EXEs as well.
    $dllPath = "$($_.Directory)/$($_.BaseName).dll"
    $exePath = "$($_.Directory)/$($_.BaseName).exe"
    if (Test-Path $dllPath) {
        $BinaryImagePath = $dllPath
    } elseif (Test-Path $exePath) {
        $BinaryImagePath = $exePath
    } else {
        Write-Warning "`"$_`" found with no matching binary file."
        $BinaryImagePath = $null
    }

    if ($BinaryImagePath) {
        Write-Output $BinaryImagePath
        Write-Output $_.FullName
    }
}
