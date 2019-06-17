<#
.SYNOPSIS
Expands this template into an actual project, taking values for placeholders
.PARAMETER Name
The name of the library
#>
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string]$LibraryName,
    [Parameter(Mandatory=$true)]
    [string]$Author,
    [Parameter()]
    [string]$CodeCovToken,
    [Parameter()]
    [string]$CIFeed
)

function Replace-Placeholders {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        [Parameter(Mandatory=$true)]
        $Replacements
    )

    $Path = Resolve-Path $Path
    Write-Host "Replacing tokens in `"$Path`""
    $content = Get-Content -Path $Path | Out-String
    $Replacements.GetEnumerator() |% {
        $modifiedContent = $content -replace $_.Key,$_.Value
        if ($modifiedContent -eq $content) {
            Write-Error "No $($_.Key) token found to replace."
        }
        $content = $modifiedContent
    }
    $content = $content.TrimEnd(("`r","`n"))
    [System.IO.File]::WriteAllLines($Path, $content) # Don't use Set-Content because that adds a UTF8 BOM
    git add $Path
}

# Try to find sn.exe if it isn't on the PATH
$sn = Get-Command sn -ErrorAction SilentlyContinue
if (-not $sn) {
    $snExes = Get-ChildItem -Recurse "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\sn.exe"
    if ($snExes) {
        $sn = Get-Command $snExes[0].FullName
    } else {
        Write-Error "sn command not found on PATH and SDK could not be found."
        exit(1)
    }
}

# Verify all commands we use are on the PATH
('git','dotnet') |% {
    if (-not (Get-Command $_ -ErrorAction SilentlyContinue)) {
        Write-Error "$_ command not found on PATH."
        exit(1)
    }
}

Push-Location $PSScriptRoot
try {
    # Rename project directories and solution
    Set-Location src
    git mv Library.sln "$LibraryName.sln"
    git mv Library/Library.csproj "Library/$LibraryName.csproj"
    git mv Library "$LibraryName"
    git mv Library.Tests/Library.Tests.csproj "Library.Tests/$LibraryName.Tests.csproj"
    git mv Library.Tests "$LibraryName.Tests"

    # Refresh solution file both to update paths and give the projects unique GUIDs
    dotnet sln remove Library/Library.csproj
    dotnet sln remove Library.Tests/Library.Tests.csproj
    dotnet sln add "$LibraryName"
    dotnet sln add "$LibraryName.Tests"
    git add "$LibraryName.sln"

    # Update project reference in test project. Add before removal to keep the same ItemGroup in place.
    dotnet add "$LibraryName.Tests" reference "$LibraryName"
    dotnet remove "$LibraryName.Tests" reference Library/Library.csproj
    git add "$LibraryName.Tests/$LibraryName.Tests.csproj"

    # Establish a new strong-name key
    & $sn.Path -k 2048 strongname.snk
    git add strongname.snk

    Set-Location ..

    # Replace placeholders in source files
    Replace-Placeholders -Path "src/$LibraryName/Calculator.cs" -Replacements @{
        'Library'=$LibraryName
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "src/$LibraryName.Tests/CalculatorTests.cs" -Replacements @{
        'Library'=$LibraryName
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "LICENSE" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "src/stylecop.json" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "src/Directory.Build.props" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "README.md" -Replacements @{
        "(?m)^.*\[NuGet package\][^`r`n]*"="[![NuGet package](https://img.shields.io/nuget/v/$LibraryName.svg)](https://nuget.org/packages/$LibraryName)"
        "(?m)^.*\[Build Status\].*`r?`n"=""
        "(?m)^.*\[codecov\].*`r?`n"=""
    }

    # Specially handle azure-pipelines.yml edits
    $YmlReplacements = @{
        "(?m).*expand-template\.yml(?:\r)?\n" = ""
    }
    if ($CodeCovToken) {
        $YmlReplacements['(codecov_token: ).*(#.*)'] = "`$1$CodeCovToken"
    } else {
        $YmlReplacements['(codecov_token: ).*(#.*)'] = "#`$1`$2"
    }
    if ($CIFeed) {
        $YmlReplacements['(ci_feed: ).*(#.*)'] = "`$1$CIFeed"
    } else {
        $YmlReplacements['(ci_feed: ).*(#.*)'] = "#`$1`$2"
    }
    Replace-Placeholders -Path "azure-pipelines.yml" -Replacements $YmlReplacements

    # Self destruct
    $Invocation = (Get-Variable MyInvocation -Scope 1).Value
    git rm Expand-Template.ps1
    git rm :/azure-pipelines/expand-template.yml

    # Self-integrity check
    Get-ChildItem -Recurse -File -Exclude bin,obj,README.md,Expand-Template.ps1 |? { -not $_.FullName.Contains("obj") } |% {
        $PLACEHOLDERS = Get-Content -Path $_.FullName |? { $_.Contains('PLACEHOLDER') }
        if ($PLACEHOLDERS) {
            Write-Error "PLACEHOLDER discovered in $($_.FullName)"
        }
    }
} finally {
    Pop-Location
}

# When testing this script, all the changes can be quickly reverted with this command:
# git reset HEAD :/README.md :/LICENSE :/azure-pipelines.yml :/src :/azure-pipelines; git co -- :/README.md :/LICENSE :/azure-pipelines.yml :/src :/azure-pipelines; git clean -fd :/src
