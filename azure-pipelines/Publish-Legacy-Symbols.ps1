Param(
    [string]$Path
)

$ArtifactStagingFolder = & "$PSScriptRoot/Get-ArtifactsStagingDirectory.ps1"
$ArtifactStagingFolder += '/symbols-legacy'
robocopy $Path $ArtifactStagingFolder /mir /njh /njs /ndl /nfl
$WindowsPdbSubDirName = 'symstore'

Get-ChildItem "$ArtifactStagingFolder\*.pdb" -Recurse |% {
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
        # Convert the PDB to legacy Windows PDBs
        Write-Host "Converting PDB for $_" -ForegroundColor DarkGray
        $WindowsPdbDir = "$($_.Directory.FullName)\$WindowsPdbSubDirName"
        if (!(Test-Path $WindowsPdbDir)) { mkdir $WindowsPdbDir | Out-Null }
        $legacyPdbPath = "$WindowsPdbDir\$($_.BaseName).pdb"
        & "$PSScriptRoot\Convert-PDB.ps1" -DllPath $BinaryImagePath -PdbPath $_ -OutputPath $legacyPdbPath
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "PDB conversion of `"$_`" failed."
        }

        Move-Item $legacyPdbPath $_ -Force
    }
}

Write-Host "##vso[artifact.upload containerfolder=symbols-legacy;artifactname=symbols-legacy;]$ArtifactStagingFolder"
