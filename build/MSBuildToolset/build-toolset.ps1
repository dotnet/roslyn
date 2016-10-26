# Builds the toolset for use in unix msbuild-coreclr
param (
    [string]$NuGetTaskDir,
    [string]$RoslynDropDir,
    [string]$DotNetPath = "dotnet"
)

function ZipFiles($sourceDir, $zipPath)
{
   If (Test-Path $zipPath)
   {
       Remove-Item $zipPath
   }

   Add-Type -Assembly System.IO.Compression.FileSystem
   [System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
}

$ErrorActionPreference="Stop"

try
{
    pushd $PSScriptRoot

    & $DotNetPath restore

    $outputRoot = "$PSScriptRoot\bin\Debug\netcoreapp1.0"

    foreach ($TargetRuntime in ("ubuntu.14.04-x64","osx.10.10-x64"))
    {
        & $DotNetPath publish -r $TargetRuntime

        $publishDir = "$outputRoot\$TargetRuntime\publish"

        # Fix up AssignLinkMetadata
        # TODO(https://github.com/Microsoft/msbuild/issues/544)
        $curVersionTargets = "$publishDir\Microsoft.Common.CurrentVersion.targets"
        (Get-Content $curVersionTargets).replace(';AssignLinkMetadata</PrepareForBuildDependsOn>', '</PrepareForBuildDependsOn>') | Set-Content $curVersionTargets

        # Set up tasks and targets
        $extensionsMSDir = "$publishDir\Extensions\Microsoft"
        $nugetExtensionsDir = "$extensionsMSDir\NuGet"

        # TODO(https://github.com/dotnet/roslyn/issues/9642)
        # Copy the NuGet tasks/targets
        mkdir -Force $nugetExtensionsDir
        Copy-Item -Force "$NuGetTaskDir\*" $NuGetExtensionsDir

        # Copy the portable targets
        write-host "Copy portable targets"
        Copy-Item -Force -Recurse (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "MSBuild\Microsoft\Portable") "$extensionsMSDir"
        
        # Copy over the portable build task
        # TODO(https://github.com/dotnet/roslyn/issues/9640)
        # Copy from a NuGet package instead of a build.
        # Overwrite dlls pulled in by the references listed in project.json 
        # (we want a specific toolset compiler version).
        write-host "Copy portable build task"
        foreach ($item in (
            "CodeAnalysisBuildTask\Microsoft.Build.Tasks.CodeAnalysis.dll",
            "CodeAnalysisBuildTask\Microsoft.CSharp.Core.targets",
            "CodeAnalysisBuildTask\Microsoft.VisualBasic.Core.targets", 
            "System.Reflection.Metadata.dll", 
            "System.Collections.Immutable.dll", 
            "Microsoft.CodeAnalysis.dll", 
            "Microsoft.CodeAnalysis.CSharp.dll", 
            "Microsoft.CodeAnalysis.VisualBasic.dll", 
            "csccore\csc.exe", 
            "vbccore\vbc.exe"))
        {
            Copy-Item (Join-Path $RoslynDropDir "20160525.2" $item) $publishDir
        }

        # Copy over the reference assemblies
        $frameworkRefsDir = "$publishDir\reference-assemblies\Framework"
        mkdir -Force "$frameworkRefsDir\.NETFramework"
        
        write-host "Copy .NETFramework/v4.5 reference assemblies"
        Copy-Item -Force -Recurse (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5") "$frameworkRefsDir\.NETFramework"

        write-host "Copy .NETPortable reference assemblies"
        Copy-Item -Force -Recurse (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Reference Assemblies\Microsoft\Framework\.NETPortable") $frameworkRefsDir

        # Bug: https://github.com/Microsoft/msbuild/issues/522
        # Reference assembly has a bad name, fixup
        pushd "$publishDir\reference-assemblies\Framework\.NETFramework\v4.5"
        Move-Item System.XML.dll System.Xml.dll
        popd

        # TODO(https://github.com/dotnet/roslyn/issues/9641)
        # Portable CSharp targets have ref with wrong case
        write-host "Copy portable targets files"
        foreach ($version in ("v4.5","v5.0"))
        {
            $file = "$extensionsMSDir\Portable\$version\Microsoft.Portable.CSharp.targets"
            (Get-Content $file) | %{ $_ -replace 'Microsoft.CSharp.Targets','Microsoft.CSharp.targets' } | Out-File $file
        }

        # Copy the helper scripts for csc/vbc
        write-host "Copy helper scripts for csc"
        Copy-Item -Force "$PSScriptRoot\..\..\src\Compilers\CSharp\CscCore\csc" $publishDir

        write-host "Copy helper scripts for vbc"
        Copy-Item -Force "$PSScriptRoot\..\..\src\Compilers\VisualBasic\VbcCore\vbc" $publishDir

        $toolsetPackageZip = (Join-Path $outputRoot "$TargetRuntime.zip")
        Write-Host "Zipping $publishDir to $toolsetPackageZip"
        ZipFiles $publishDir $toolsetPackageZip
    }

    popd
}
catch
{
    write-host "Error: $($_.Exception.Message)"
    exit 1
}
