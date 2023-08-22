<#
  This script drives the Jenkins verification that our build is correct.  In particular:

    - Our build has no double writes
    - Our project.json files are consistent
    - Our build files are well structured
    - Our solution states are consistent
    - Our generated files are consistent

#>

[CmdletBinding(PositionalBinding=$false)]
param(
  [string]$configuration = "Debug",
  [switch]$enableDumps = $false,
  [string]$bootstrapToolset = "",
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-build-correctness.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
}

try {
  if ($help) {
    Print-Usage
    exit 0
  }

  $ci = $true

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  if ($enableDumps) {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    New-Item -Path $key -ErrorAction SilentlyContinue
    New-ItemProperty -Path $key -Name 'DumpType' -PropertyType 'DWord' -Value 2 -Force
    New-ItemProperty -Path $key -Name 'DumpCount' -PropertyType 'DWord' -Value 2 -Force
    New-ItemProperty -Path $key -Name 'DumpFolder' -PropertyType 'String' -Value $LogDir -Force
  }

  Write-Host "Building Roslyn"
  Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -restore -build -bootstrap -bootstrapConfiguration:Debug -bootstrapToolset:$bootstrapToolset -ci:$ci -runAnalyzers:$true -configuration:$configuration -pack -binaryLog -useGlobalNuGetCache:$false -warnAsError:$true -properties "/p:RoslynEnforceCodeStyle=true"}

  Subst-TempDir

  # Verify the state of our various build artifacts
  Write-Host "Running BuildBoss"
  $buildBossPath = GetProjectOutputBinary "BuildBoss.exe"
  Write-Host "$buildBossPath -r `"$RepoRoot/`" -c $configuration -p Roslyn.sln"
  Exec-Console $buildBossPath "-r `"$RepoRoot/`" -c $configuration" -p Roslyn.sln
  Write-Host ""

  # Verify the state of our generated syntax files
  Write-Host "Checking generated compiler files"
  Exec-Block { & (Join-Path $PSScriptRoot "generate-compiler-code.ps1") -test -configuration:$configuration }
  Exec-Console dotnet "tool run dotnet-format whitespace . --folder --include-generated --include src/Compilers/CSharp/Portable/Generated/ src/Compilers/VisualBasic/Portable/Generated/ src/ExpressionEvaluator/VisualBasic/Source/ResultProvider/Generated/ --verify-no-changes"
  Write-Host ""

  exit 0
}
catch [exception] {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host "##vso[task.logissue type=error]How to investigate bootstrap failures: https://github.com/dotnet/roslyn/blob/main/docs/compilers/Bootstrap%20Builds.md#Investigating"
  exit 1
}
finally {
  if ($enableDumps) {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    Remove-ItemProperty -Path $key -Name 'DumpType'
    Remove-ItemProperty -Path $key -Name 'DumpCount'
    Remove-ItemProperty -Path $key -Name 'DumpFolder'
  }

  Unsubst-TempDir
  Pop-Location
}
