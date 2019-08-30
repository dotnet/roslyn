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

  Write-Host "Building Roslyn"
  Exec-Block { & (Join-Path $PSScriptRoot "build.ps1") -restore -build -ci:$ci -configuration:$configuration -pack -binaryLog -useGlobalNuGetCache:$false -warnAsError:$false -properties "/p:RoslynEnforceCodeStyle=true"}

  # Verify the state of our various build artifacts
  Write-Host "Running BuildBoss"
  $buildBossPath = GetProjectOutputBinary "BuildBoss.exe"
  Exec-Console $buildBossPath "-r `"$RepoRoot`" -c $configuration" -p Roslyn.sln
  Write-Host ""

  # Verify the state of our generated syntax files
  Write-Host "Checking generated compiler files"
  Exec-Block { & (Join-Path $PSScriptRoot "generate-compiler-code.ps1") -test -configuration:$configuration }
  Write-Host ""
  
  # Verify the state of creating run settings for OptProf
  Write-Host "Checking OptProf run settings generation"

  # create a fake BootstrapperInfo.json file
  $bootstrapperInfoPath = Join-Path $TempDir "BootstrapperInfo.json"
  $bootstrapperInfoContent = "[{""BuildDrop"": ""https://vsdrop.corp.microsoft.com/file/v1/Products/42.42.42.42/42.42.42.42""}]"
  $bootstrapperInfoContent | Set-Content $bootstrapperInfoPath

  # generate run settings
  Exec-Block { & (Join-Path $PSScriptRoot "common\sdk-task.ps1") -configuration:$configuration -task VisualStudio.BuildIbcTrainingSettings /p:VisualStudioDropName="Products/DummyDrop" /p:BootstrapperInfoPath=$bootstrapperInfoPath }
  
  exit 0
}
catch [exception] {
  Write-Host $_
  Write-Host $_.Exception
  exit 1
}
finally {
  Pop-Location
}
