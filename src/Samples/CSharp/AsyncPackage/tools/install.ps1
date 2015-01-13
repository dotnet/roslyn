param($installPath, $toolsPath, $package, $project)

$p = Get-Project

$analyzerPath = join-path $toolsPath "analyzers"
$analyzerFilePath = join-path $analyzerPath "AsyncPackage.dll"

$p.Object.AnalyzerReferences.Add("$analyzerFilePath")