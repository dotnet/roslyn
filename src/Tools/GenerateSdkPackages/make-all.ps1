Param(
    [string]$version = "26014.00",
    [string]$branch = "d15rel",
    [string]$outPath = $null,
    [string]$fakeSign = $null
)

set-strictmode -version 2.0
$ErrorActionPreference="Stop"

function do-work() {

}

try {
    if ($outPath -eq "") {
        write-host "Need an -output value"
        exit 1
    }

    if ($fakeSign -eq "") {
        write-host "Need a -fakeSign value"
        exit 1
    }

    $list = @(
        "Microsoft.VisualStudio.Shell.Design.dll",
        "Microsoft.VisualStudio.CallHierarchy.Package.Definitions.dll"
        "vspkgs\Microsoft.VisualStudio.ComponentModelHost.dll",
        "Microsoft.VisualStudio.CoreUtility.dll",
        "vspkgs\Microsoft.VisualStudio.Editor.dll",
        "Microsoft.VisualStudio.ImageCatalog.dll",
        "Microsoft.VisualStudio.Imaging.dll",
        "Microsoft.VisualStudio.Language.CallHierarchy.dll",
        "Microsoft.VisualStudio.Language.Intellisense.dll",
        "Microsoft.VisualStudio.Language.NavigateTo.Interfaces.dll",
        "Microsoft.VisualStudio.Language.StandardClassification.dll",
        "Microsoft.VisualStudio.Text.Data.dll",
        "Microsoft.VisualStudio.Text.Internal.dll",
        "Microsoft.VisualStudio.Text.Logic.dll",
        "Microsoft.VisualStudio.Text.UI.dll",
        "Microsoft.VisualStudio.Text.UI.Wpf.dll",
        "Microsoft.VisualStudio.Utilities.dll",
        "Microsoft.VisualStudio.Platform.VSEditor.dll",
        "Microsoft.VisualStudio.Platform.VSEditor.Interop.dll",
        "Microsoft.VisualStudio.Shell.Interop.15.0.DesignTime.dll",
        "Microsoft.Internal.Performance.CodeMarkers.DesignTime.dll",
        "PackageAndDeploy\Microsoft.VisualStudio.Shell.Framework.dll",
        "PackageAndDeploy\Microsoft.VisualStudio.Shell.15.0.dll",
        "PackageAndDeploy\Microsoft.VisualStudio.Validation.dll",
        "PackageAndDeploy\Microsoft.VisualStudio.Threading.dll",
        "PackageAndDeploy\Microsoft.VisualStudio.Shell.Immutable.10.0.dll",
        "PackageAndDeploy\Microsoft.VisualStudio.Shell.Immutable.14.0.dll",
        "Progression\Microsoft.VisualStudio.Progression.CodeSchema.dll",
        "Progression\Microsoft.VisualStudio.Progression.Common.dll",
        "Progression\Microsoft.VisualStudio.Progression.Interfaces.dll",
        "StaticAnalysis\Microsoft.VisualStudio.CodeAnalysis.Sdk.UI.dll",
        "VMP\Microsoft.VisualStudio.GraphModel.dll",
        "VMP\Microsoft.VisualStudio.Diagnostics.PerformanceProvider.dll",
        "VSTelemetryPackage\Microsoft.VisualStudio.Telemetry.dll"
        )

    $dropPath = "\\cpvsbuild\drops\VS\$branch\raw\$version\binaries.x86ret\bin\i386"

    $baseNuspecPath = join-path $PSScriptRoot "base.nuspec"
    $shortVersion = $version.Substring(0, $version.IndexOf('.'))
    $packageVersion = "15.0.$shortVersion-alpha"
    $dllPath = join-path $outPath "Dlls"
    $packagePath = join-path $outPath "Packages"

    write-host "Drop path is $dropPath"
    write-host "Package version $packageVersion"
    write-host "Out path is $outPath"

    mkdir $outPath -ErrorAction SilentlyContinue | out-null
    mkdir $dllPath -ErrorAction SilentlyContinue | out-null
    mkdir $packagePath -ErrorAction SilentlyContinue | out-null
    pushd $outPath
    try {

        foreach ($item in $list) {
            $name = split-path -leaf $item
            $simpleName = [IO.Path]::GetFileNameWithoutExtension($name) 
            write-host "Packing $simpleName"
            $sourceFilePath = join-path $dropPath $item
            $filePath = join-path $dllPath $name
            if (-not (test-path $sourceFilePath)) {
                write-host "Could not locate $sourceFilePath"
                continue;
            }

            cp $sourceFilePath $filePath
            & $fakeSign -f $filePath
            & nuget pack $baseNuspecPath -OutputDirectory $packagePath -Properties name=$simpleName`;version=$packageVersion`;filePath=$filePath
        }
    }
    finally {
        popd
    }
}
catch [exception] {
    write-host $_.Exception
    exit -1
}
