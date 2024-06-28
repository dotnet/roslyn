#!/usr/bin/env pwsh

<#
.SYNOPSIS
Expands this template into an actual project, taking values for placeholders
.PARAMETER LibraryName
The name of the library. Should consist only of alphanumeric characters and periods.
.PARAMETER Author
The name to use in copyright and owner notices.
.PARAMETER CodeCovToken
A token obtained from codecov.io for your repo. If not specified, code coverage results will not be published to codecov.io,
but can be added later by editing the Azure Pipelines YAML file.
.PARAMETER CIFeed
The `/{guid}` path to the Azure Pipelines artifact feed to push your nuget package to as part of your CI.
.PARAMETER Squash
A switch that causes all of git history to be squashed to just one initial commit for the template, and one for its expansion.
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact='Medium')]
Param(
    [Parameter(Mandatory=$true)]
    [string]$LibraryName,
    [Parameter(Mandatory=$true)]
    [string]$Author,
    [Parameter()]
    [string]$CodeCovToken,
    [Parameter()]
    [string]$CIFeed,
    [Parameter()]
    [switch]$Squash
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
    if ($IsMacOS -or $IsLinux) {
        Write-Error "sn command not found on PATH. Install mono and/or vote up this issue: https://github.com/dotnet/sdk/issues/13560"
        exit(1)
    }
    $snExes = Get-ChildItem -Recurse "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\sn.exe"
    if ($snExes) {
        $sn = Get-Command $snExes[0].FullName
    } else {
        Write-Error "sn command not found on PATH and SDK could not be found."
        exit(1)
    }
}

if (-not (& "$PSScriptRoot\tools\Check-DotNetSdk.ps1")) {
    if ($PSCmdlet.ShouldProcess('Install .NET Core SDK?')) {
        & "$PSScriptRoot\tools\Install-DotNetSdk.ps1"
    } else {
        Write-Error "Matching .NET Core SDK version not found. Install now?"
        exit 1
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
    if ($Squash) {
        $originalCommitId = git rev-parse HEAD
        git reset --soft $(git rev-list --max-parents=0 HEAD)
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        git commit --amend -qm "Initial template from https://github.com/AArnott/Library.Template" -m "Original commit from template $originalCommitId"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    git config core.safecrlf false # Avoid warnings when adding files with mangled line endings

    # Rename project directories and solution
    git mv Library.sln "$LibraryName.sln"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git mv src/Library/Library.csproj "src/Library/$LibraryName.csproj"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git mv src/Library "src/$LibraryName"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git mv test/Library.Tests/Library.Tests.csproj "test/Library.Tests/$LibraryName.Tests.csproj"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git mv test/Library.Tests "test/$LibraryName.Tests"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Refresh solution file both to update paths and give the projects unique GUIDs
    dotnet sln remove src/Library/Library.csproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet sln remove test/Library.Tests/Library.Tests.csproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet sln add "src/$LibraryName"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet sln add "test/$LibraryName.Tests"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git add "$LibraryName.sln"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Update project reference in test project. Add before removal to keep the same ItemGroup in place.
    dotnet add "test/$LibraryName.Tests" reference "src/$LibraryName"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet remove "test/$LibraryName.Tests" reference src/Library/Library.csproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git add "test/$LibraryName.Tests/$LibraryName.Tests.csproj"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Establish a new strong-name key
    & $sn.Path -k 2048 strongname.snk
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git add strongname.snk
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Replace placeholders in source files
    Replace-Placeholders -Path "src/$LibraryName/Calculator.cs" -Replacements @{
        'Library'=$LibraryName
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "test/$LibraryName.Tests/CalculatorTests.cs" -Replacements @{
        'Library'=$LibraryName
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "src/AssemblyInfo.cs" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "src/AssemblyInfo.vb" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "LICENSE" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "stylecop.json" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "Directory.Build.props" -Replacements @{
        'COMPANY-PLACEHOLDER'=$Author
    }
    Replace-Placeholders -Path "README.md" -Replacements @{
        "(?m)^.*\[NuGet package\][^`r`n]*"="[![NuGet package](https://img.shields.io/nuget/v/$LibraryName.svg)](https://nuget.org/packages/$LibraryName)"
        "(?m)^.*\[Azure Pipelines status\].*`r?`n"=""
        "(?m)^.*\[GitHub Actions status\].*`r?`n"=""
        "(?m)^.*\[codecov\].*`r?`n"=""
    }

    # Specially handle azure-pipelines .yml edits
    Replace-Placeholders -Path "azure-pipelines/build.yml" -Replacements @{
        "(?m).*expand-template\.yml(?:\r)?\n" = ""
    }

    $YmlReplacements = @{}
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
    git rm Expand-Template.* Apply-Template.ps1
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    git rm :/azure-pipelines/expand-template.yml
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Self-integrity check
    Get-ChildItem -Recurse -File -Exclude bin,obj,README.md,Expand-Template.* |? { -not $_.FullName.Contains("obj") } |% {
        $PLACEHOLDERS = Get-Content -Path $_.FullName |? { $_.Contains('PLACEHOLDER') }
        if ($PLACEHOLDERS) {
            Write-Error "PLACEHOLDER discovered in $($_.FullName)"
        }
    }

    # Commit the changes
    git commit -qm "Expanded template for $LibraryName" -m "This expansion done by the (now removed) Expand-Template.ps1 script."
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host -ForegroundColor Green "Template successfully expanded."

    if ($env:PS1UnderCmd) {
        # We're running under the Expand-Template.cmd script.
        # Since we just deleted it from disk cmd.exe will complain. Just advise the user it's OK.
        Write-Host -ForegroundColor Green 'Disregard an error you may see: "The batch file cannot be found." We just cleaned up after ourselves.'
    }

} finally {
    git config --local --unset core.safecrlf
    Pop-Location
}

# When testing this script, all the changes can be quickly reverted with this command:
# git reset HEAD :/README.md :/LICENSE :/azure-pipelines.yml :/src :/test :/azure-pipelines; git co -- :/README.md :/LICENSE :/azure-pipelines.yml :/src :/azure-pipelines; git clean -fd :/src :/test
