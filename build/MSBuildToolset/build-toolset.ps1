# Builds the toolset for use in unix msbuild-coreclr
param (
    [string]$NuGetTaskDir = "$PSScriptRoot\..\..\..\NuGet.BuildTasks\src\Microsoft.NuGet.Build.Tasks\bin\Debug"
)

$ErrorActionPreference="Stop"

try
{
    pushd $PSScriptRoot

    dotnet restore
    dotnet build

    $dnxcoreDir = "$PSScriptRoot\bin\Debug\dnxcore50"

    foreach ($TargetRuntime in ("ubuntu.14.04-x64","osx.10.10-x64"))
    {
        dotnet publish -r $TargetRuntime

        $publishDir = "$dnxcoreDir\$TargetRuntime\publish"

        # Set up tasks and targets
        $extensionsMSDir = "$publishDir\Extensions\Microsoft"
        $nugetExtensionsDir = "$extensionsMSDir\NuGet"

        # TODO(https://github.com/dotnet/roslyn/issues/9642)
        # Copy the NuGet tasks/targets
        mkdir -Force $nugetExtensionsDir
        Copy-Item -Force "$NuGetTaskDir\*" $NuGetExtensionsDir

        # Copy the portable targets
        Copy-Item -Force -Recurse (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "MSBuild\Microsoft\Portable") "$extensionsMSDir"

        # Copy over the portable build task
        # TODO(https://github.com/dotnet/roslyn/issues/9640)
        # Copy from a NuGet package instead of a build
        foreach ($item in ("Microsoft.Build.Tasks.CodeAnalysis.dll","Microsoft.CSharp.Core.targets","Microsoft.VisualBasic.Core.targets"))
        {
            Copy-Item (Join-Path $PSScriptRoot\..\..\Binaries\Debug\CodeAnalysisBuildTask $item) $publishDir
        }


        # Copy over the reference assemblies
        $frameworkRefsDir = "$publishDir\reference-assemblies\Framework"
        mkdir -Force "$frameworkRefsDir\.NETFramework"
        Copy-Item -Force -Recurse (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5") "$frameworkRefsDir\.NETFramework"

        Copy-Item -Force -Recurse (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Reference Assemblies\Microsoft\Framework\.NETPortable") $frameworkRefsDir

        # Bug: https://github.com/Microsoft/msbuild/issues/522
        # Reference assembly has a bad name, fixup
        pushd "$publishDir\reference-assemblies\Framework\.NETFramework\v4.5"

        Move-Item System.XML.dll System.Xml.dll

        popd

        # TODO(https://github.com/dotnet/roslyn/issues/9641)
        # Portable CSharp targets have ref with wrong case
        foreach ($version in ("v4.5","v5.0"))
        {
            $file = "$extensionsMSDir\Portable\$version\Microsoft.Portable.CSharp.targets"
            (Get-Content $file) | %{ $_ -replace 'Microsoft.CSharp.Targets','Microsoft.CSharp.targets' } | Out-File $file
        }


        # Copy the helper scripts for csc/vbc
        Copy-Item -Force "$PSScriptRoot\..\..\src\Compilers\CSharp\CscCore\csc" $publishDir
        Copy-Item -Force "$PSScriptRoot\..\..\src\Compilers\VisualBasic\VbcCore\vbc" $publishDir

        Move-Item $publishDir 
    }

    popd
}
catch
{
    write-host "Error: $($_.Exception.Message)"
    exit 1
}
