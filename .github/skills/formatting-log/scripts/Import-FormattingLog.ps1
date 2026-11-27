<#
.SYNOPSIS
Imports a locally downloaded Razor formatting log zip into FormattingLogTest.

.DESCRIPTION
Developer Community and Azure DevOps feedback attachments must be downloaded by the user first.
Pass the local archive path with -ZipPath.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,

    [string]$TestName,

    [string]$WorkItemUrl,

    [ValidateSet('NotNull', 'Null')]
    [string]$Expectation = 'NotNull',

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($ZipPath -match '^[A-Za-z][A-Za-z0-9+\-.]*://')
{
    throw "ZipPath must be a local file path. Download the formatting log archive first, then pass the local zip path."
}

function Get-RepositoryRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
}

function ConvertTo-TestName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ZipBaseName
    )

    $candidate = $ZipBaseName
    if ($candidate -match '^(Full|Range)_(?<Name>.+)_razor$')
    {
        $candidate = $Matches['Name']
    }

    $parts = $candidate -split '[^A-Za-z0-9]+' | Where-Object { $_ }
    if (-not $parts)
    {
        throw "Couldn't derive a test name from '$ZipBaseName'. Pass -TestName explicitly."
    }

    $normalized = ($parts | ForEach-Object {
            if ($_.Length -eq 1)
            {
                $_.ToUpperInvariant()
            }
            else
            {
                $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
            }
        }) -join ''

    if ($normalized -match '^[0-9]')
    {
        $normalized = "Case$normalized"
    }

    return $normalized
}

function Assert-ValidTestName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$')
    {
        throw "Test name '$Name' is not a valid C# identifier. Pass -TestName with a valid identifier."
    }
}

function Get-ContentRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExtractPath
    )

    $items = @(Get-ChildItem -LiteralPath $ExtractPath -Force)
    if ($items.Count -eq 1 -and $items[0].PSIsContainer)
    {
        return $items[0].FullName
    }

    return $ExtractPath
}

function Copy-FormattingLogFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    foreach ($item in Get-ChildItem -LiteralPath $SourcePath -Force)
    {
        Copy-Item -LiteralPath $item.FullName -Destination $DestinationPath -Recurse -Force
    }
}

function Add-FormattingLogTest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TestFilePath,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string]$IssueUrl,

        [Parameter(Mandatory = $true)]
        [ValidateSet('NotNull', 'Null')]
        [string]$AssertionType,

        [switch]$OverwriteExisting
    )

    $contents = [System.IO.File]::ReadAllText($TestFilePath)
    $methodPattern = "(?m)^\s*public\s+async\s+Task\s+$([regex]::Escape($Name))\s*\("
    if ([regex]::IsMatch($contents, $methodPattern))
    {
        if ($OverwriteExisting)
        {
            return
        }

        throw "FormattingLogTest already contains a test named '$Name'."
    }

    $newline = "`r`n"
    $workItemAttribute = if ([string]::IsNullOrWhiteSpace($IssueUrl))
    {
        ''
    }
    else
    {
        "    [WorkItem(`"$IssueUrl`")]$newline"
    }

    $assertion = if ($AssertionType -eq 'Null') { 'Assert.Null' } else { 'Assert.NotNull' }
    $methodBlock = "    [Fact]$newline$workItemAttribute    public async Task $Name()$newline        => $assertion(await GetFormattingEditsAsync());$newline$newline"

    $marker = '    private async Task<TextEdit[]?> GetFormattingEditsAsync'
    $markerIndex = $contents.IndexOf($marker)
    if ($markerIndex -lt 0)
    {
        throw "Couldn't find the insertion point in '$TestFilePath'."
    }

    $updatedContents = $contents.Insert($markerIndex, $methodBlock)
    $encoding = [System.Text.UTF8Encoding]::new($true)
    [System.IO.File]::WriteAllText($TestFilePath, $updatedContents, $encoding)
}

$resolvedZipPath = (Resolve-Path -LiteralPath $ZipPath).Path
$repoRoot = Get-RepositoryRoot
$testFilePath = Join-Path $repoRoot 'src\Razor\src\Razor\test\Microsoft.VisualStudio.LanguageServices.Razor.UnitTests\Cohost\Formatting\FormattingLogTest.cs'
$assetRoot = Join-Path $repoRoot 'src\Razor\src\Razor\test\Microsoft.VisualStudio.LanguageServices.Razor.UnitTests\TestFiles\FormattingLog'

if (-not $TestName)
{
    $TestName = ConvertTo-TestName -ZipBaseName ([System.IO.Path]::GetFileNameWithoutExtension($resolvedZipPath))
}

Assert-ValidTestName -Name $TestName

$destinationPath = Join-Path $assetRoot $TestName
if (Test-Path -LiteralPath $destinationPath)
{
    if (-not $Force)
    {
        throw "Destination folder '$destinationPath' already exists. Pass -Force to overwrite its contents."
    }

    Remove-Item -LiteralPath $destinationPath -Recurse -Force
}

$extractPath = Join-Path ([System.IO.Path]::GetTempPath()) ("formatting-log-" + [Guid]::NewGuid().ToString('N'))

try
{
    New-Item -ItemType Directory -Path $extractPath | Out-Null
    Expand-Archive -LiteralPath $resolvedZipPath -DestinationPath $extractPath -Force

    $contentRoot = Get-ContentRoot -ExtractPath $extractPath
    if (-not (Get-ChildItem -LiteralPath $contentRoot -Force))
    {
        throw "The archive '$resolvedZipPath' did not contain any files to import."
    }

    New-Item -ItemType Directory -Path $destinationPath | Out-Null
    Copy-FormattingLogFiles -SourcePath $contentRoot -DestinationPath $destinationPath

    Add-FormattingLogTest -TestFilePath $testFilePath -Name $TestName -IssueUrl $WorkItemUrl -AssertionType $Expectation -OverwriteExisting:$Force
}
finally
{
    if (Test-Path -LiteralPath $extractPath)
    {
        Remove-Item -LiteralPath $extractPath -Recurse -Force
    }
}

Write-Host "Imported formatting log archive '$resolvedZipPath' as test '$TestName'."
Write-Host "Assets: $destinationPath"
Write-Host "Test file: $testFilePath"
