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
  [string]$bootstrapDir = "",
  [switch]$ci = $false,
  [switch]$help)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Print-Usage() {
  Write-Host "Usage: test-build-correctness.ps1"
  Write-Host "  -configuration            Build configuration ('Debug' or 'Release')"
}

$docBranch = "main"
try {
  if ($help) {
    Print-Usage
    exit 0
  }

  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot
  $prepareMachine = $ci

  # If we're running in a PR, we want to use the documentation from the branch that is being 
  # merged into, not necessarily the documentation in main
  if (Test-Path env:SYSTEM_PULLREQUEST_TARGETBRANCH) {
    $docBranch = $env:SYSTEM_PULLREQUEST_TARGETBRANCH
  }

  if ($enableDumps) {
    $key = "HKLM:\\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps"
    New-Item -Path $key -ErrorAction SilentlyContinue
    New-ItemProperty -Path $key -Name 'DumpType' -PropertyType 'DWord' -Value 2 -Force
    New-ItemProperty -Path $key -Name 'DumpCount' -PropertyType 'DWord' -Value 10 -Force
    New-ItemProperty -Path $key -Name 'DumpFolder' -PropertyType 'String' -Value $LogDir -Force
  }

  if ($bootstrapDir -eq "") {
    Write-Host "Building bootstrap compiler"
    $bootstrapDir = Join-Path $ArtifactsDir (Join-Path "bootstrap" "correctness")
    & eng/make-bootstrap.ps1 -output $bootstrapDir -ci:$ci
    Test-LastExitCode
  }

  Write-Host "Building Roslyn"
  & eng/build.ps1 -restore -build -bootstrapDir:$bootstrapDir -ci:$ci -prepareMachine:$prepareMachine -runAnalyzers:$true -configuration:$configuration -pack -binaryLog -useGlobalNuGetCache:$false -warnAsError:$true -properties:"/p:RoslynEnforceCodeStyle=true"
  Test-LastExitCode

  Subst-TempDir

  # Verify the state of our various build artifacts
  Write-Host "Running BuildBoss"
  $buildBossPath = GetProjectOutputBinary "BuildBoss.exe"
  Exec-Command $buildBossPath "-r `"$RepoRoot/`" -c $configuration -p Roslyn.slnx"
  Write-Host ""

  # Verify the state of our generated syntax files
  Write-Host "Checking generated compiler files"
  Exec-DotNet "run --file eng/generate-compiler-code.cs -- -test -configuration $configuration"
  Exec-DotNet "tool run dotnet-format whitespace . --folder --include-generated --include src/Compilers/CSharp/Portable/Generated/ src/Compilers/VisualBasic/Portable/Generated/ src/ExpressionEvaluator/VisualBasic/Source/ResultProvider/Generated/ --verify-no-changes"
  Write-Host ""

  ExitWithExitCode 0
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  Write-Host "##vso[task.logissue type=error]How to investigate bootstrap failures: https://github.com/dotnet/roslyn/blob/$($docBranch)/docs/contributing/bootstrap-builds.md#Investigating"
  ExitWithExitCode 1
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
