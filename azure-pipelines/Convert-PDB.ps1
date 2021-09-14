<#
.SYNOPSIS
    Converts between Windows PDB and Portable PDB formats.
.PARAMETER DllPath
    The path to the DLL whose PDB is to be converted.
.PARAMETER PdbPath
    The path to the PDB to convert. May be omitted if the DLL was compiled on this machine and the PDB is still at its original path.
.PARAMETER OutputPath
    The path of the output PDB to write.
#>
#Function Convert-PortableToWindowsPDB() {
    Param(
        [Parameter(Mandatory=$true,Position=0)]
        [string]$DllPath,
        [Parameter()]
        [string]$PdbPath,
        [Parameter(Mandatory=$true,Position=1)]
        [string]$OutputPath
    )

    if ($IsMacOS -or $IsLinux) {
        Write-Error "This script only works on Windows"
        return
    }

    $version = '1.1.0-beta2-21101-01'
    $baseDir = "$PSScriptRoot/../obj/tools"
    $pdb2pdbpath = "$baseDir/Microsoft.DiaSymReader.Pdb2Pdb.$version/tools/Pdb2Pdb.exe"
    if (-not (Test-Path $pdb2pdbpath)) {
        if (-not (Test-Path $baseDir)) { New-Item -Type Directory -Path $baseDir | Out-Null }
        $baseDir = (Resolve-Path $baseDir).Path # Normalize it
        Write-Verbose "& (& $PSScriptRoot/Get-NuGetTool.ps1) install Microsoft.DiaSymReader.Pdb2Pdb -version $version -PackageSaveMode nuspec -OutputDirectory $baseDir -Source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json | Out-Null"
        & (& $PSScriptRoot/Get-NuGetTool.ps1) install Microsoft.DiaSymReader.Pdb2Pdb -version $version -PackageSaveMode nuspec -OutputDirectory $baseDir -Source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json | Out-Null
    }

    $args = $DllPath,'/out',$OutputPath,'/nowarn','0021'
    if ($PdbPath) {
        $args += '/pdb',$PdbPath
    }

    Write-Verbose "$pdb2pdbpath $args"
    & $pdb2pdbpath $args
#}
