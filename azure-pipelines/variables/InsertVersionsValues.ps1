# When you need binding redirects in the VS repo updated to match
# assemblies that you build here, remove the "return" statement
# and update the hashtable below with the T4 macro you'll use for
# your libraries as defined in the src\ProductData\AssemblyVersions.tt file.
return

$MacroName = 'LibraryNoDotsVersion'
$SampleProject = "$PSScriptRoot\..\..\src\LibraryName"
try {
    return [string]::join(',',(@{
        ($MacroName) = & { (dotnet tool run nbgv get-version --project $SampleProject --format json | ConvertFrom-Json).AssemblyVersion };
    }.GetEnumerator() |% { "$($_.key)=$($_.value)" }))
}
catch {
    Write-Error $_
    Write-Error "Failed. Diagnostic data proceeds:"
    [string]::join(',',(@{
        ($MacroName) = & {
            Write-Host "dotnet tool run nbgv get-version --project `"$SampleProject`" --format json"
            $result1 = dotnet tool run nbgv get-version --project $SampleProject --format json
            Write-Host $result1
            $result2 = dotnet tool run nbgv get-version --project $SampleProject --format json | ConvertFrom-Json
            Write-Host $result2
            Write-Host $result2.AssemblyVersion
            Write-Host (dotnet tool run nbgv get-version --project $SampleProject --format json | ConvertFrom-Json).AssemblyVersion
            Write-Output (dotnet tool run nbgv get-version --project $SampleProject --format json | ConvertFrom-Json).AssemblyVersion
        };
    }.GetEnumerator() |% { "$($_.key)=$($_.value)" }))

    Write-Host "one more..."
    $r = & { (dotnet tool run nbgv get-version --project $SampleProject --format json | ConvertFrom-Json).AssemblyVersion };
    Write-Host $r
}
