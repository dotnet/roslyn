<#
.SYNOPSIS
    Collect the list of PDBs built in this repo, after converting them from portable to Windows PDBs.
.PARAMETER Path
    The root path to recursively search for PDBs.
.PARAMETER Tests
    A switch indicating to find test-related PDBs instead of product-only PDBs.
.PARAMETER ConvertToWindowsPDBs
    A switch to convert and return paths to Windows PDBs instead of portable PDBs.
    Ignored on non-Windows agents.
#>
[CmdletBinding()]
param (
    [parameter(Mandatory=$true)]
    [string]$Path,
    [switch]$Tests,
    [switch]$ConvertToWindowsPDBs=$true
)

$WindowsPdbSubDirName = "symstore"

$ActivityName = "Collecting symbols from $Path"
Write-Progress -Activity $ActivityName -CurrentOperation "Discovery PDB files"
$PDBs = Get-ChildItem -rec "$Path/*.pdb" |? { $_.FullName -notmatch "\W$WindowsPdbSubDirName\W" }

# Filter PDBs to product OR test related.
$testregex = "unittest|tests"
if ($Tests) {
    $PDBs = $PDBs |? { $_.FullName -match $testregex }
} else {
    $PDBs = $PDBs |? { $_.FullName -notmatch $testregex }
}

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
} |% {
    # Collect the DLLs/EXEs as well.
    $dllPath = "$($_.Directory)/$($_.BaseName).dll"
    $exePath = "$($_.Directory)/$($_.BaseName).exe"
    if (Test-Path $dllPath) {
        $BinaryImagePath = $dllPath
    } elseif (Test-Path $exePath) {
        $BinaryImagePath = $exePath
    }

    Write-Output $BinaryImagePath

    if ($ConvertToWindowsPDBs -and -not ($IsMacOS -or $IsLinux)) {
        # Convert the PDB to legacy Windows PDBs
        Write-Host "Converting PDB for $_" -ForegroundColor DarkGray
        $WindowsPdbDir = "$($_.Directory.FullName)\$WindowsPdbSubDirName"
        if (!(Test-Path $WindowsPdbDir)) { mkdir $WindowsPdbDir | Out-Null }
        & "$PSScriptRoot\Convert-PDB.ps1" -DllPath $BinaryImagePath -PdbPath $_ -OutputPath "$WindowsPdbDir\$($_.BaseName).pdb"
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "PDB conversion of `"$_`" failed."
        }

        Write-Output "$WindowsPdbDir\$($_.BaseName).pdb"
    } else {
        Write-Output $_.FullName
    }
}
