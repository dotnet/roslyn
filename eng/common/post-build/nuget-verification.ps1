<#
.SYNOPSIS
    Verifies NuGet packages using dotnet nuget verify.
.DESCRIPTION
    Initializes the .NET CLI and runs 'dotnet nuget verify' on the provided NuGet packages. 
    This script writes an error if any of the provided packages fail verification.
.PARAMETER PackagesPath
    Path to the directory containing NuGet packages to verify.
.EXAMPLE
    PS> .\nuget-verification.ps1 -PackagesPath C:\packages
    Verifies all .nupkg files in the specified directory.
#>

param(
  [Parameter(Mandatory=$true)][string] $PackagesPath # Path to where the packages to be validated are
)

# `tools.ps1` checks $ci to perform some actions. Since the post-build
# scripts don't necessarily execute in the same agent that run the
# build.ps1/sh script this variable isn't automatically set.
$ci = $true
$disableConfigureToolsetImport = $true
. $PSScriptRoot\..\tools.ps1

try {
  $fence = New-Object -TypeName string -ArgumentList '=', 80

  # Initialize the dotnet CLI
  $dotnetRoot = InitializeDotNetCli -install:$true
  $dotnet = Join-Path $dotnetRoot (GetExecutableFileName 'dotnet')

  Write-Host "Using dotnet: $dotnet"
  Write-Host " "

  # Get all .nupkg files in the packages path
  $packageFiles = Get-ChildItem -Path $PackagesPath -Filter '*.nupkg' -File

  if ($packageFiles.Count -eq 0) {
      Write-Host "No .nupkg files found in $PackagesPath"
      Write-Output "dotnet nuget verify succeeded (no packages to verify)."
      return
  }

  # Get the full paths of the package files
  $packagePaths = $packageFiles | ForEach-Object { $_.FullName }

  # Execute dotnet nuget verify
  Write-Host "Executing dotnet nuget verify..."
  Write-Host $fence
  & $dotnet nuget verify $packagePaths
  Write-Host $fence
  Write-Host " "

  # Respond to the exit code.
  if ($LASTEXITCODE -ne 0) {
      Write-PipelineTelemetryError -Category 'NuGetValidation' -Message "dotnet nuget verify found some problems."
      ExitWithExitCode 1
  } else {
      Write-Output "dotnet nuget verify succeeded."
  }
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'NuGetValidation' -Message $_
  ExitWithExitCode 1
}
