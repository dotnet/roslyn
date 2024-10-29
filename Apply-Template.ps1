#!/usr/bin/env pwsh

<#
.SYNOPSIS
Applies the template to another repo in a semi-destructive way.
Always apply to a clean working copy so that undesired updates can be easily reverted.
.PARAMETER Path
The path to the root of the repo to be updated with the latest version of this template.
#>

[CmdletBinding(SupportsShouldProcess, ConfirmImpact='Medium')]
Param(
    [Parameter(Mandatory=$true)]
    [ValidateScript({Test-Path $_})]
    [string]$Path
)

Push-Location $Path
try {
    # Look for our own initial commit in the target repo's history.
    # If it's there, they've already switched to using git merge to freshen up.
    # Using Apply-Template would just complicate future merges, so block it.
    git merge-base --is-ancestor 05f49ce799c1f9cc696d53eea89699d80f59f833 HEAD | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Error 'The target repo already has Library.Template history merged into it. Use `git merge` instead of this script to freshen your repo. See the README.md file for details.'
        exit 1
    }
} finally {
    Pop-Location
}

Write-Host "Updating $Path"
robocopy /mir $PSScriptRoot/azure-pipelines $Path/azure-pipelines
robocopy /mir $PSScriptRoot/.config $Path/.config
robocopy /mir $PSScriptRoot/.devcontainer $Path/.devcontainer
robocopy /mir $PSScriptRoot/.github $Path/.github
robocopy /mir $PSScriptRoot/.vscode $Path/.vscode
robocopy /mir $PSScriptRoot/tools $Path/tools
robocopy $PSScriptRoot $Path Directory.Build.* Directory.Packages.props global.json init.* azure-pipelines.yml .gitignore .gitattributes .editorconfig
robocopy $PSScriptRoot/src $Path/src Directory.Build.* .editorconfig AssemblyInfo.cs AssemblyInfo.vb
robocopy $PSScriptRoot/test $Path/test Directory.Build.* .editorconfig
Remove-Item $Path/azure-pipelines/expand-template.yml
