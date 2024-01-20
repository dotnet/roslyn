# This searches a directory for a given package, and sets an Azure DevOps variable to the found version number,
# which is useful if you need to pass that version to a later Azure DevOps build task. The variable set is
# the name of the package, with periods removed, and "PackageVersion" appended. So for example, Microsoft.CodeAnalysis
# would be "MicrosoftCodeAnalysisPackageVersion".

[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$folder,
  [string]$packageName)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

$packageBaseName = (Get-ChildItem -Path $folder -Filter "$packageName*.nupkg").BaseName
$packageVersion = $packageBaseName.Substring($packageName.Length + 1)
$variableName = $packageName.Replace(".", "") + "PackageVersion"
Write-Host "Found version $packageVersion of $packageName in $folder"
Write-Host "##vso[task.setvariable variable=$variableName]$packageVersion"