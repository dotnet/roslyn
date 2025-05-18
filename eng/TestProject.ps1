# Find all .csproj files recursively
$csprojFiles = Get-ChildItem -Recurse -Filter *.csproj

$importRegex = '<ProjectReference\s+Include="([^"]+TypeForwards\.csproj)"'

foreach ($csproj in $csprojFiles) {
    $content = Get-Content $csproj.FullName -Raw
    $matches = [regex]::Matches($content, $importRegex)
    foreach ($match in $matches) {
        $importPath = $match.Groups[1].Value

        # Resolve relative paths
        $resolvedPath = $importPath
        if (!(Test-Path $importPath)) {
            $resolvedPath = Join-Path $csproj.DirectoryName $importPath
        }
        $resolvedPath = [System.IO.Path]::GetFullPath($resolvedPath)

        # Skip error reporting if the import path contains a '$' (likely an MSBuild variable)
        if ($importPath -notmatch '\$') {
            if (!(Test-Path $resolvedPath)) {
                Write-Error "Missing import: '$importPath' in project '$($csproj.FullName)'. Resolved path: '$resolvedPath'"
            } else {
                Write-Host "Import found: '$importPath' in project '$($csproj.FullName)'. Resolved path: '$resolvedPath'"
            }
        }
    }
}