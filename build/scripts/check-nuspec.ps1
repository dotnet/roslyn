Param(
    [string]$sourcePath = $null,
    [string]$binariesPath = $null
)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    write-host "Verifying contents of Micrsoft.Net.Compilers.nuspec"
    $deploymentPath = join-path $binariesPath "Exes\Toolset"
    [xml]$x = gc (join-path $sourcePath "src\NuGet\Microsoft.Net.Compilers.nuspec")
    $nuspecAssemblies = 
        $x.package.files.file | 
        %{ $_.src } | 
        ? { (-not $_.Contains("$")) -and (-not $_.EndsWith(".config")) -and (-not $_.EndsWith(".rsp")) } | 
        %{ split-path -leaf $_ };

    # Files whech are deployed but do not need to be included in the NuGet package
    $excludedFiles = @(
        "Toolset_DoNetUse.exe",
        "Microsoft.Build.dll",
        "Microsoft.Build.Framework.dll",
        "Microsoft.Build.Tasks.Core.dll",
        "Microsoft.Build.Utilities.Core.dll",
        "Microsoft.DiaSymReader.Native.arm.dll"
    )

    $deployedFiles = 
        gci -re -in *.exe,*.dll $deploymentPath |
        %{ split-path -leaf $_ } |
        ?{ -not $excludedFiles.Contains($_) };

    # The number 20 is somewhat arbitrary.  The goal is to guarantee we don't succeed by virtue of 
    # verifying an empty directory.  But also don't want the number to be the exact count of assemblies
    # in the NuSpec because it would make it more tedious to do future updates.
    if ($deployedFiles.Count -lt 20)
    {
        write-host "Missing deployed files"
        exit 1
    }

    $allGood = $true

    write-host "`tchecking deployed files"
    foreach ($file in $deployedFiles)
    {
        $name = split-path -leaf $file
        write-host "`t`t$name"
        if ($nuspecAssemblies.Contains($name))
        {
            continue;
        }

        write-host "$file is not included in the NuSpec or exclude list"
        $allGood = $false
    }

    # This verification is not the same as verifying we can pack the NuGet.  Several of the files
    # in the nuspec are necessarily taken from a directory other than the deployment one (for
    # signing reasons).  The reverse check ensures Toolset includes the build of all the products
    # shipped in the nuspec and hence the full set of facades
    write-host "`tchecking nuspec files"
    foreach ($file in $nuspecAssemblies)
    {
        write-host "`tchecking $file"
        $target = join-path $deploymentPath $file
        if (-not (test-path $target))
        {
            write-host "`t`t$file is missing in $deploymentPath"
            $allGood = false
        }
    }

    if (-not $allGood)
    {
        exit 1
    }

    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
