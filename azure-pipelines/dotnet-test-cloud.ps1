#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs tests as they are run in cloud test runs.
.PARAMETER Configuration
    The configuration within which to run tests
.PARAMETER Agent
    The name of the agent. This is used in preparing test run titles.
.PARAMETER PublishResults
    A switch to publish results to Azure Pipelines.
.PARAMETER x86
    A switch to run the tests in an x86 process.
.PARAMETER dotnet32
    The path to a 32-bit dotnet executable to use.
#>
[CmdletBinding()]
Param(
    [string]$Configuration='Debug',
    [string]$Agent='Local',
    [switch]$PublishResults,
    [switch]$x86,
    [string]$dotnet32
)

$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$ArtifactStagingFolder = & "$PSScriptRoot/Get-ArtifactsStagingDirectory.ps1"
$TestLogsPath = "$ArtifactStagingFolder/test_logs"
if (!(Test-Path $TestLogsPath)) { New-Item -ItemType Directory -Path $TestLogsPath | Out-Null }

$dotnet = 'dotnet'
if ($x86) {
  $x86RunTitleSuffix = ", x86"
  if ($dotnet32) {
    $dotnet = $dotnet32
  } else {
    $dotnet32Possibilities = "$PSScriptRoot\../obj/tools/x86/.dotnet/dotnet.exe", "$env:AGENT_TOOLSDIRECTORY/x86/dotnet/dotnet.exe", "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
    $dotnet32Matches = $dotnet32Possibilities |? { Test-Path $_ }
    if ($dotnet32Matches) {
      $dotnet = Resolve-Path @($dotnet32Matches)[0]
      Write-Host "Running tests using `"$dotnet`"" -ForegroundColor DarkGray
    } else {
      Write-Error "Unable to find 32-bit dotnet.exe"
      return 1
    }
  }
}

$dotnetTestArgs = @()
$dotnetTestArgs2 = @()

# The GitHubActions test logger fails when combined with certain switches, but only on mac/linux.
# We avoid those switches in that specific context.
# Failure symptoms when using the wrong switch combinations on mac/linux are (depending on the switches) EITHER:
# - The test runner fails with exit code 1 (and no error message)
# - The test runner succeeds but the GitHubActions logger only adds annotations on Windows agents.
# See https://github.com/Tyrrrz/GitHubActionsTestLogger/discussions/37 for more info.
# Thus, the mess of conditions you see below, in order to get GitHubActions to work
# without undermining other value we have when running in other contexts.
if ($env:GITHUB_WORKFLOW -and ($IsLinux -or $IsMacOS)) {
    $dotnetTestArgs += '--collect','Xplat Code Coverage'
    $dotnetTestArgs2 += 'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover'
} else {
    $dotnetTestArgs += '--diag','$$TestLogsPath/diag.log;TraceLevel=info'
    $dotnetTestArgs += '--collect','Code Coverage;Format=cobertura'
    $dotnetTestArgs += '--settings',"$PSScriptRoot/test.runsettings"
}

if ($env:GITHUB_WORKFLOW) {
    $dotnetTestArgs += '--logger','GitHubActions'
    $dotnetTestArgs2 += 'RunConfiguration.CollectSourceInformation=true'
}

& $dotnet test $RepoRoot `
    --no-build `
    -c $Configuration `
    --filter "TestCategory!=FailsInCloudTest" `
    --blame-hang-timeout 60s `
    --blame-crash `
    -bl:"$ArtifactStagingFolder/build_logs/test.binlog" `
    --logger trx `
    @dotnetTestArgs `
    -- `
    @dotnetTestArgs2

$unknownCounter = 0
Get-ChildItem -Recurse -Path $RepoRoot\test\*.trx |% {
  Copy-Item $_ -Destination $TestLogsPath/

  if ($PublishResults) {
    $x = [xml](Get-Content -LiteralPath $_)
    $runTitle = $null
    if ($x.TestRun.TestDefinitions -and $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')) {
      $storage = $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')[0].storage -replace '\\','/'
      if ($storage -match '/(?<tfm>net[^/]+)/(?:(?<rid>[^/]+)/)?(?<lib>[^/]+)\.dll$') {
        if ($matches.rid) {
          $runTitle = "$($matches.lib) ($($matches.tfm), $($matches.rid), $Agent)"
        } else {
          $runTitle = "$($matches.lib) ($($matches.tfm)$x86RunTitleSuffix, $Agent)"
        }
      }
    }
    if (!$runTitle) {
      $unknownCounter += 1;
      $runTitle = "unknown$unknownCounter ($Agent$x86RunTitleSuffix)";
    }

    Write-Host "##vso[results.publish type=VSTest;runTitle=$runTitle;publishRunAttachments=true;resultFiles=$_;failTaskOnFailedTests=true;testRunSystem=VSTS - PTR;]"
  }
}
