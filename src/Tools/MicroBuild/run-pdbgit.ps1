<#

    Run PDB Git on our PDB files to enable proper source server support

#>
Param(
    [string]$sourcePath = $null,
    [string]$binariesPath = $null
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

function Get-PackagesPath {
    $packagesPath = $env:NUGET_PACKAGES
    if ($packagesPath -eq $null) {
        $packagesPath = join-path $env:UserProfile ".nuget\packages\"
    }

    return $packagesPath
}

try
{
    [xml]$deps = get-content (join-path $sourcePath "build\Targets\Dependencies.props")
    $pdbGitVersion = $deps.Project.PropertyGroup.PdbGitVersion
    $packagesPath = Get-PackagesPath
    $pdbGit = join-path $packagesPath "pdbgit\$pdbGitVersion\tools\PdbGit.exe"

    $signToolDataPath = join-path $sourcePath "build\config\SignToolData.json"
    $signToolData = convertfrom-json (get-content $signToolDataPath -raw)
    $allGood = $true
    $count = 0

    foreach ($relativePath in $signToolData.sign | %{ $_.values }) {
        $binaryPath = join-Path $binariesPath $relativePath
        $pdbPath = [IO.Path]::ChangeExtension($binaryPath, ".pdb")
        if (-not (test-path $pdbPath)) {
            continue;
        }

        write-host "Running pdbgit on $pdbPath"
        & $pdbGit $pdbPath -u https://github.com/dotnet/roslyn --baseDir $sourcePath 
        $count++
        if (-not $?) {
            $allGood = $false
            write-host "FAILED!!!"
        }
    }

    if (-not $allGood) {
        exit 1
    }

    # The number 20 is a bit arbitrary here.  Don't want to count the exact number of PDBs 
    # beacuse that makes it tedious to add new binaries.  20 feels like a good number here
    # because if we every linked less than that it would almost certainly be an error that
    # needed to be investigated.
    if ($count -lt 20) {
        write-host "ERROR!!! Suspiciously low number of PDBs found: $count."
        exit 1
    }

    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
