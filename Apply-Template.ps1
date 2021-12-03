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

Write-Host "Updating $Path"
robocopy /mir $PSScriptRoot/azure-pipelines $Path/azure-pipelines
robocopy /mir $PSScriptRoot/.devcontainer $Path/.devcontainer
robocopy /mir $PSScriptRoot/.github $Path/.github
robocopy /mir $PSScriptRoot/.vscode $Path/.vscode
robocopy /mir $PSScriptRoot/tools $Path/tools
robocopy $PSScriptRoot $Path Directory.Build.* global.json init.* azure-pipelines.yml .gitignore .gitattributes .editorconfig
robocopy $PSScriptRoot/src $Path/src Directory.Build.* .editorconfig AssemblyInfo.cs
robocopy $PSScriptRoot/test $Path/test Directory.Build.* .editorconfig
Remove-Item $Path/azure-pipelines/expand-template.yml
