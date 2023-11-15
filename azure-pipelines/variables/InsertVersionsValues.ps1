# When you need binding redirects in the VS repo updated to match
# assemblies that you build here, remove the "return" statement
# and update the hashtable below with the T4 macro you'll use for
# your libraries as defined in the src\ProductData\AssemblyVersions.tt file.
return

$MacroName = 'ExtensionTestingVersion'
$SampleProject = "$PSScriptRoot\..\..\src\Microsoft.VisualStudio.Extensibility.Testing.Xunit"
[string]::join(',',(@{
    ($MacroName) = & { (dotnet nbgv get-version --project $SampleProject --format json | ConvertFrom-Json).AssemblyVersion };
}.GetEnumerator() |% { "$($_.key)=$($_.value)" }))
