Param(
    [string]$binariesPath = $null,
    [switch]$test
)
set-strictmode -version 2.0
$ErrorActionPreference="Stop"

try
{
    $items = @(
        "ExpressionEvaluatorPackage.vsix",
        "VisualStudioInteractiveComponents\Roslyn.VisualStudio.InteractiveComponents.vsix",
        "VisualStudioSetup.Next\Roslyn.VisualStudio.Setup.Next.vsix",
        "VisualStudioSetup\Roslyn.VisualStudio.Setup.vsix",
        "Microsoft.CodeAnalysis.ExpressionEvaluator.json",
        "Microsoft.CodeAnalysis.VisualStudio.Setup.json",
        "Microsoft.CodeAnalysis.VisualStudio.Setup.Next.json",
        "Microsoft.CodeAnalysis.VisualStudio.InteractiveComponents.json",
        "Microsoft.CodeAnalysis.LanguageServices.vsman",
        "Microsoft.CodeAnalysis.Compilers.json",
        "Microsoft.CodeAnalysis.Compilers.vsix",
        "Microsoft.CodeAnalysis.Compilers.vsman",
        "PortableFacades.vsman")
    $baseDestPath = join-path $binariesPath "Insertion"
    if (-not (test-path $baseDestPath))
    {
        mkdir $baseDestPath | out-null
    }

    foreach ($item in $items) 
    {
        $sourcePath = join-path $binariesPath $item
        $destPath = join-path $baseDestPath (split-path -leaf $item)

        # Many of these files are only produced in the Official MicroBuild runs.  On test runs, which run locally,
        # we need to guard agains this.
        if ((-not (test-path $sourcePath)) -and $test)
        {
            write-host "Skip copying $sourcePath for test run"
            continue;
        }

        if (test-path $destPath)
        {
            write-host "Skipping $item as it already exists in $destPath"
            continue;
        }

        write-host "Copying $sourcePath to $destPath"
        copy $sourcePath $destPath
    }

    exit 0
}
catch [exception]
{
    write-host $_.Exception
    exit -1
}
