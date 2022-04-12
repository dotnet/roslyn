#!/usr/bin/env pwsh

Param(
    [string]$Configuration='Debug',
    [string]$Agent='Local',
    [switch]$PublishResults
)

$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$ArtifactStagingFolder = & "$PSScriptRoot/Get-ArtifactsStagingDirectory.ps1"

dotnet test $RepoRoot `
    --no-build `
    -c $Configuration `
    --filter "TestCategory!=FailsInCloudTest" `
    -p:CollectCoverage=true `
    --blame-hang-timeout 60s `
    --blame-crash `
    -bl:"$ArtifactStagingFolder/build_logs/test.binlog" `
    --diag "$ArtifactStagingFolder/test_logs/diag.log;TraceLevel=info" `
    --logger trx

$unknownCounter = 0
Get-ChildItem -Recurse -Path $RepoRoot\test\*.trx |% {
  Copy-Item $_ -Destination $ArtifactStagingFolder/test_logs/

  if ($PublishResults) {
    $x = [xml](Get-Content -Path $_)
    $runTitle = $null
    if ($x.TestRun.TestDefinitions -and $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')) {
      $storage = $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')[0].storage -replace '\\','/'
      if ($storage -match '/(?<tfm>net[^/]+)/(?:(?<rid>[^/]+)/)?(?<lib>[^/]+)\.dll$') {
        if ($matches.rid) {
          $runTitle = "$($matches.lib) ($($matches.tfm), $($matches.rid), $Agent)"
        } else {
          $runTitle = "$($matches.lib) ($($matches.tfm), $Agent)"
        }
      }
    }
    if (!$runTitle) {
      $unknownCounter += 1;
      $runTitle = "unknown$unknownCounter ($Agent)";
    }

    Write-Host "##vso[results.publish type=VSTest;runTitle=$runTitle;publishRunAttachments=true;resultFiles=$_;failTaskOnFailedTests=true;testRunSystem=VSTS - PTR;]"
  }
}
