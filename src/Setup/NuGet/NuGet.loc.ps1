
param(
    [Parameter(Mandatory=$true)]
    [string]$DropBasePath,
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [Parameter(Mandatory=$true)]
    [string]$BinariesPath
)

[array]$compilerNuGets = @(
    ,"Microsoft.CodeAnalysis.Common"
    ,"Microsoft.CodeAnalysis.Compilers"
    ,"Microsoft.CodeAnalysis.CSharp"
    ,"Microsoft.CodeAnalysis.VisualBasic"
)

[array]$ideNuGets = @(
    ,"Microsoft.CodeAnalysis.EditorFeatures.Text"
    ,"Microsoft.CodeAnalysis.CSharp.Workspaces"
    ,"Microsoft.CodeAnalysis.VisualBasic.Workspaces"
    ,"Microsoft.CodeAnalysis.Workspaces.Common"
    ,"Microsoft.VisualStudio.LanguageServices"
)

# Table of languages we want to localize from ISO code to culture name
$languages = @(
    ,@("CHS", "zh-Hans", "Chinese (Simplified)")
    ,@("CHT", "zh-Hant", "Chinese (Traditional)")
    ,@("DEU", "de", "German")
    ,@("ESN", "es", "Spanish")
    ,@("FRA", "fr", "French")
    ,@("ITA", "it", "Italian")
    ,@("JPN", "ja", "Japanese")
    ,@("KOR", "ko", "Korean")
    ,@("RUS", "ru", "Russian")
  #  ,@("CSY", "cs", "Czech")
  #  ,@("PTB", "pt", "Portugeuese")
  #  ,@("PLK", "pl", "Polish")
  #  ,@("TRK", "tr", "Turkish")
)

$author = "Microsoft"
$projectUrl = "http://msdn.com/roslyn"
$releaseNotes = 'Preview of Microsoft .NET Compiler Platform ("Roslyn")'
$prereleaseUrlRedist = "http://go.microsoft.com/fwlink/?LinkId=394369"
$prereleaseUrlNonRedist = "http://go.microsoft.com/fwlink/?LinkId=394372"
$tags = ""

$outdir = "$PSScriptRoot\..\..\..\Open\Binaries\NuGet.loc"

# Load in the NuGet.exe information
. "$PSScriptRoot\..\..\..\Open\builds\scripts\LoadNuGetInfo.ps1"
write-host "NuGet.exe path is $nugetexe"

$nugetArgs = @("pack", "-OutputDirectory", $outdir, "-prop", "authors=$author",
    "-prop", "projectUrl=$projectUrl", "-prop", "currentVersion=$Version",
    "-prop", "releaseNotes=""$releaseNotes""", "-prop", "tags=$tags"
)

function GetResourcesDirectory($isoLanguage, [bool]$isCompilerNuget)
{
    $resourcesPath = $DropBasePath
    $compilerSuffix = "binaries.x86ret\Localized\simship\$isoLanguage\ExternalApis\Roslyn\"
    $ideSuffix = "binaries.x86ret\Localized\$isoLanguage\ExternalApis\Roslyn\"

    # Now for the special casing
    if ($isoLanguage -eq "JPN")
    {
        $resourcesPath = Join-Path $resourcesPath "LOC"
    }
    elseif (@("CSY","PLK","PTB","TRK") -icontains $isoLanguage)
    {
        $resourcesPath = Join-Path $resourcesPath "CLE"
    }
    else
    {
        $resourcesPath = Join-Path $resourcesPath $isoLanguage
    }
    
    if ($isCompilerNuget)
    {
        $resourcesPath = Join-Path $resourcesPath $compilerSuffix
    }
    else
    {
        $resourcesPath = Join-Path $resourcesPath $ideSuffix
    }
    $resourcesPath
}

if (!(Test-Path $outdir))
{
    mkdir $outdir
}

foreach ($lang in $languages)
{
    # Generate compiler library NuGets
    foreach ($nugetName in $compilerNugets)
    {
        $resourcesDir = GetResourcesDirectory $lang[0] $true
        $fullArgs = $nugetArgs + @("-prop", "langCode=$($lang[1])", "-prop", 
                    "language=$($lang[2])", "-BasePath", "$resourcesDir",
                    "-prop", "licenseUrl=$prereleaseUrlNonRedist",
                    "$PSScriptRoot\NuSpec.loc\$nugetName.loc.nuspec")
        & "$nugetExe" $fullArgs
    }

    # Generate IDE library NuGets
    foreach ($nugetName in $ideNugets)
    {
        $resourcesDir = GetResourcesDirectory $lang[0] $false
        $fullArgs = $nugetArgs + @("-prop", "langCode=$($lang[1])", 
                    "-prop", "language=$($lang[2])", "-BasePath", "$resourcesDir",
                    "-prop", "licenseUrl=$prereleaseUrlRedist"
                    "$PSScriptRoot\NuSpec.loc\$nugetName.loc.nuspec")
        & "$nugetExe" $fullArgs
    }

    # Generate Microsoft.Net.Compilers
    $randomDirName = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    mkdir $randomDirName
    cp @("$BinariesPath\Microsoft.CodeAnalysis.dll",
         "$BinariesPath\Microsoft.CodeAnalysis.CSharp.dll",
         "$BinariesPath\Microsoft.CodeAnalysis.VisualBasic.dll",
         "$BinariesPath\Microsoft.Net.ToolsetCompilers.props",
         "$BinariesPath\csc.exe",
         "$BinariesPath\vbc.exe",
         "$BinariesPath\System.Collections.Immutable.dll",
         "$BinariesPath\System.Reflection.Metadata.dll",
         "$BinariesPath\VBCSCompiler.exe",
         "$BinariesPath\VBCSCompiler.exe.config",
         "$BinariesPath\ThirdPartyNotices.rtf") $randomDirName

    $resourcesDir = GetResourcesDirectory $lang[0] $true
    cp $resourcesDir\* $randomDirName

    $fullArgs = $nugetArgs + @("-prop", "langCode=$($lang[1])", 
                "-prop", "language=$($lang[2])", "-BasePath", "$randomDirName",
                "-prop", "licenseUrl=$prereleaseUrlRedist"
                "$PSScriptRoot\NuSpec.loc\Microsoft.Net.Compilers.loc.nuspec")
    & "$nugetExe" $fullArgs
    
    rm -r -Force $randomDirName
}
