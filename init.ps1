<#
.SYNOPSIS
Installs dependencies required to build and test the projects in this repository.
#>
[CmdletBinding(SupportsShouldProcess=$true)]
Param (
)

& "$PSScriptRoot\tools\Install-DotNetSdk.ps1"
