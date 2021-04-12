# The new project system does not support designers. This blocks our ability to make XAML
# changes through the designer. A rare event but necessary on occasiona.
#
# This script helps with the times where XAML changes are needed. It will temporarily revert
# the projects which have XAML files to the legacy project system. The XAML edits can then
# be processed and the coversion can be reverted. 
#
# Bug tracking project system getting designer support
# https://github.com/dotnet/project-system/issues/1467

[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

$projectList = @(
    'src\VisualStudio\Core\Impl\ServicesVisualStudioImpl.csproj',
    'src\VisualStudio\CSharp\Impl\CSharpVisualStudio.csproj',
    'src\VisualStudio\VisualBasic\Impl\BasicVisualStudio.vbproj',
    'src\EditorFeatures\Core\EditorFeatures.csproj',
    'src\VisualStudio\Core\Def\ServicesVisualStudio.csproj')

# Edit all of the project files to no longer have a TargetFramework element. This is what
# causes the selector to use the new project system. 
function Change-ProjectFiles() { 
    foreach ($proj in $projectList) {
        $proj = Join-Path $RepoRoot $proj
        $lines = Get-Content $proj
        for ($i = 0; $i -lt $lines.Length; $i++) { 
            $line = $lines[$i]
            if ($line -match ".*TargetFramework") {
                $lines[$i] = "<!-- DO NOT MERGE --> <TargetFrameworkVersion>v4.6</TargetFrameworkVersion>"
            }
        }

        [IO.File]::WriteAllLines($proj, $lines)
    }
}

# Change the solution file to use the legacy project system GUID.
function Change-Solution() {
    $solution = Join-Path $RepoRoot "Roslyn.sln"
    $lines = Get-Content $solution
    for ($i = 0; $i -lt $lines.Length; $i++) { 
        $line = $lines[$i]
        foreach ($proj in $projectList) {
            if ($line -like "*$proj*") {
                Write-Host "Found $line"
                $ext = [IO.Path]::GetExtension($proj)
                if ($ext -eq ".csproj") { 
                    $oldGuid = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"
                    $newGuid = "9A19103F-16F7-4668-BE54-9A1E7A4F7556"
                } else { 
                    $oldGuid = "F184B08F-C81C-45F6-A57F-5ABD9991F28F"
                    $newGuid = "778DAE3C-4631-46EA-AA77-85C1314464D9"
                } 

                $line = $line.Replace($newGuid, $oldGuid)
                Write-Host "New Line $line"
                $lines[$i] = $line
            }
        }
    }

    [IO.File]::WriteAllLines($solution, $lines)
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")
    Push-Location $RepoRoot

    Change-ProjectFiles
    Change-Solution

    exit 0

}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}
finally {
    Pop-Location
}
