<#
.SYNOPSIS
Creates a VS Code workspace for local Roslyn and Razor manual testing and optionally launches VS Code.

.DESCRIPTION
Builds the local language server and Razor extension outputs, writes a .code-workspace file that points
the C# extension at those local bits, and launches VS Code against a chosen project folder.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$WorkspacePath,

    [switch]$SkipBuild,

    [switch]$NoLaunch,

    [switch]$NewWindow
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
}

function Get-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path))
    {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Invoke-DotNetBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectFile,

        [Parameter(Mandatory = $true)]
        [string]$BuildConfiguration
    )

    & dotnet build $ProjectFile -c $BuildConfiguration --nologo
    if ($LASTEXITCODE -ne 0)
    {
        throw "Build failed for '$ProjectFile'."
    }
}

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path))
    {
        throw "$Description was not found at '$Path'."
    }
}

function Get-DefaultWorkspacePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedProjectPath
    )

    $projectName = Split-Path -Leaf ($ResolvedProjectPath.TrimEnd('\', '/'))
    if ([string]::IsNullOrWhiteSpace($projectName))
    {
        $projectName = 'local'
    }

    return Join-Path $RepoRoot "artifacts\$projectName-local-test.code-workspace"
}

$repoRoot = Get-RepositoryRoot
$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedWorkspacePath = if ($WorkspacePath)
{
    Get-AbsolutePath -Path $WorkspacePath -BasePath $repoRoot
}
else
{
    Get-DefaultWorkspacePath -RepoRoot $repoRoot -ResolvedProjectPath $resolvedProjectPath
}

$languageServerProject = Join-Path $repoRoot 'src\LanguageServer\Microsoft.CodeAnalysis.LanguageServer\Microsoft.CodeAnalysis.LanguageServer.csproj'
$razorExtensionProject = Join-Path $repoRoot 'src\Razor\src\Razor\src\Microsoft.VisualStudioCode.RazorExtension\Microsoft.VisualStudioCode.RazorExtension.csproj'

if (-not $SkipBuild)
{
    Invoke-DotNetBuild -ProjectFile $languageServerProject -BuildConfiguration $Configuration
    Invoke-DotNetBuild -ProjectFile $razorExtensionProject -BuildConfiguration $Configuration
}

$languageServerPath = Join-Path $repoRoot "artifacts\bin\Microsoft.CodeAnalysis.LanguageServer\$Configuration\net10.0\Microsoft.CodeAnalysis.LanguageServer.dll"
$razorExtensionPath = Join-Path $repoRoot "artifacts\bin\Microsoft.VisualStudioCode.RazorExtension\$Configuration\net10.0"

Assert-PathExists -Path $languageServerPath -Description 'Local Roslyn language server'
Assert-PathExists -Path $razorExtensionPath -Description 'Local Razor VS Code extension output'

$workspaceDirectory = Split-Path -Parent $resolvedWorkspacePath
if (-not [string]::IsNullOrWhiteSpace($workspaceDirectory))
{
    New-Item -ItemType Directory -Path $workspaceDirectory -Force | Out-Null
}

$workspace = [ordered]@{
    folders  = @(
        [ordered]@{
            path = $resolvedProjectPath
        }
    )
    settings = [ordered]@{
        'dotnet.server.path' = $languageServerPath
        'dotnet.server.componentPaths' = [ordered]@{
            razorExtension = $razorExtensionPath
        }
    }
}

$workspaceJson = $workspace | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($resolvedWorkspacePath, $workspaceJson + [Environment]::NewLine)

Write-Host "Workspace: $resolvedWorkspacePath"
Write-Host "Project folder: $resolvedProjectPath"
Write-Host "Roslyn language server: $languageServerPath"
Write-Host "Razor extension: $razorExtensionPath"

if ($NoLaunch)
{
    return
}

if (-not (Get-Command code -ErrorAction SilentlyContinue))
{
    throw "VS Code command line launcher 'code' was not found on PATH."
}

$arguments = if ($NewWindow)
{
    @($resolvedWorkspacePath)
}
else
{
    @('--reuse-window', $resolvedWorkspacePath)
}

Start-Process code -ArgumentList $arguments | Out-Null
