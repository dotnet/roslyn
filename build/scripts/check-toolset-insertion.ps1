Param(
    [string]$sourcePath = $null,
    [string]$binariesPath = $null
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    [Void][Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')

    write-host "Verify Toolset binaries folder"
    
    # Files whech are deployed but do not need to be included in deployment
    $excludedFiles = @(
        "Toolset_DoNetUse.exe",
        "Microsoft.Build.dll",
        "Microsoft.Build.Framework.dll",
        "Microsoft.Build.Tasks.Core.dll",
        "Microsoft.Build.Utilities.Core.dll",
        "Microsoft.DiaSymReader.Native.arm.dll"
    )

    $exludedFromVsix = @(    
        "Microsoft.DiaSymReader.Native.x86.dll", # installed by msbuild setup component
        "Microsoft.DiaSymReader.Native.amd64.dll"  # installed by msbuild setup component
    )
    
    $deploymentPath = join-path $binariesPath "Exes\Toolset"
    $deployedFiles = 
        gci -re -in *.exe,*.dll $deploymentPath |
        %{ split-path -leaf $_ } |
        ?{ -not $excludedFiles.Contains($_) } |
        sort-object;
        
    # The number 20 is somewhat arbitrary.  The goal is to guarantee we don't succeed by virtue of 
    # verifying an empty directory.  But also don't want the number to be the exact count of assemblies
    # in the NuSpec because it would make it more tedious to do future updates.
    if ($deployedFiles.Count -lt 20)
    {
        write-host "Missing deployed files"
        exit 1
    }

    # ----------------------------------------------------------------------------------------------------------------
    
    # We now perform two-way verification on both nuspec and vsix.
    # First we check that all deployed files exist in the package, then we ensure that all package files exist in the deployment folder.
    # Several of the files in the package are necessarily taken from a directory other than the deployment folder (for signing reasons).
    # The reverse check ensures Toolset includes the build of all the products shipped in the package and hence the full set of facades.

    $allGood = $true

    write-host "Verifying contents of Micrsoft.Net.Compilers.nuspec"
    [xml]$nuspecxml = gc (join-path $sourcePath "src\NuGet\Microsoft.Net.Compilers.nuspec")
    $nuspecAssemblies = 
        $nuspecxml.package.files.file | 
        %{ $_.src } | 
        ? { (-not $_.Contains("$")) } | 
        %{ split-path -leaf $_ } |
        sort-object;

    write-host "checking deployed files"
    foreach ($file in $deployedFiles)
    {
        write-host "`t$file"
        if ($nuspecAssemblies.Contains($file))
        {
            continue;
        }

        write-host "`t`t$file is not included in the NuSpec or exclude list"
        $allGood = $false
    }

    write-host "checking nuspec files"
    foreach ($file in $nuspecAssemblies)
    {
        write-host "`t$file"
        $target = join-path $deploymentPath $file
        if (-not (test-path $target))
        {
            write-host "`t`t$file is missing in $deploymentPath"
            $allGood = false
        }
    }

    write-host "Verifying contents of Microsoft.CodeAnalysis.Compilers.vsix"
    $vsixpath = join-path $binariesPath "Insertion\Microsoft.CodeAnalysis.Compilers.vsix"
    $msbuildroslynfiles =  [IO.Compression.ZipFile]::OpenRead($vsixpath).Entries |
        ?{ $_.FullName.StartsWith("Contents/MSBuild/15.0/Bin/Roslyn") } |
        %{ $_.Name } |
        sort-object;
    
    write-host "checking deployed files"
    foreach ($file in $deployedFiles)
    {
        write-host "`t$file"
        if ($msbuildroslynfiles.Contains($file) -or $exludedFromVsix.Contains($file))
        {
            continue;
        }

        write-host "`t`t$file is not included in the vsix or exclude list"
        $allGood = $false
    }

    write-host "checking vsix files"
    foreach ($file in $msbuildroslynfiles)
    {
        write-host "`t$file"
        $target = join-path $deploymentPath $file
        if (-not (test-path $target))
        {
            write-host "`t`t$file is missing in $deploymentPath"
            $allGood = false
        }
    }

    if (-not $allGood)
    {
        throw [System.IO.FileNotFoundException] "A file was missing from setup. Review script output for more details."
    }
    
    write-host "script completed successfully."
    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
