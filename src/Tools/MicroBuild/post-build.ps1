# Run our post build steps.  This occurs after all of our shipping binaries are built and fully 
# signed.  The steps we need to execute are:
#
#   1. Run GitLink on the PDBs so we have proper source linking.
#   2. Setup the <Configuration>\Index directory for simple / efficient indexing in official builds


param([string]$config = "Release")
Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

# Get the path to all of the shipping EXE / DLL files.
function Get-BinaryList() {
    $list = @()
    $config = Join-Path $repoDir "build\config\SignToolData.json"
    $j = ConvertFrom-Json (Get-Content -raw $config)
    foreach ($entry in $j.sign) {
        foreach ($v in $entry.values) { 
            $ext = [IO.Path]::GetExtension($v)
            if (($ext -eq ".dll") -or ($ext -eq ".exe")) {
                $path = Join-Path $configDir $v
                $list += $path;
            }
        }
    }

    return $list
}

# Run GitLink on all of our PDB files in their original location.  
function Run-GitLink($binaryLst) {

    $gitlinkVersion = Get-PackageVersion "GitLink"
    $gitlink = Join-Path (Get-PackagesDir) "GitLink\$gitlinkVersion\build\GitLink.exe"

    Write-Host "Running GitLink"
    foreach ($filePath in $binaryList) { 
        $pdbPath = [IO.Path]::ChangeExtension($filePath, ".pdb")
        Write-Host "`t$pdbPath"
        Exec-Block { & $gitlink -u "https://github.com/dotnet/roslyn" $pdbPath --baseDir $repoDir } | Write-Host
    }
}

# Create single directory to hold all of the items that we need to be indexed by 
# our official build system.  This is done to both keep the indexing script simple
# and efficient by not indexing binaries that are simply unneeded. 
function Create-Index($binaryList) { 
    $indexDir = Join-Path $configDir "Index"
    Remove-Item -re -fo $indexDir -ErrorAction SilentlyContinue
    Create-Directory $indexDir

    Write-Host "Creating the Index directory"
    foreach ($filePath in $binaryList) {
        $dirName = Split-Path -leaf (Split-Path -parent $filePath)
        $targetDir = Join-Path $indexDir $dirName
        Create-Directory $targetDir 

        Copy-Item $filePath $targetDir
        $pdbPath = [IO.Path]::ChangeExtension($filePath, ".pdb")
        if (Test-Path $pdbPath) { 
            Copy-Item $pdbPath $targetDir
        }
    }
}

try {
    . (Join-Path $PSScriptRoot "..\..\..\build\scripts\build-utils.ps1")

    $configDir = Join-Path $binariesDir $config
    $binaryList = Get-BinaryList
    Run-GitLink $binaryList
    Create-Index $binaryList

    exit 0
}
catch {
    Write-Host $_
    exit 1
}
