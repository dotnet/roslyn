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
    --blame-hang-timeout 30s `
    --blame-crash `
    -bl:"$ArtifactStagingFolder/build_logs/test.binlog" `
    --diag "$ArtifactStagingFolder/test_logs/diag.log;TraceLevel=info" `
    --logger trx

$unknownCounter = 0
Get-ChildItem -Recurse -Path $RepoRoot\test\*.trx |% {
  Copy-Item $_ -Destination $ArtifactStagingFolder/test_logs/

  if ($PublishResults) {
    $x = [xml](Get-Content -Path $_)
    $storage = $x.TestRun.TestDefinitions.GetElementsByTagName('UnitTest')[0].storage -replace '\\','/'
    if ($storage -match '/(?<tfm>[^/]+)/(?<lib>[^/]+)\.dll$') {
        $runTitle = "$($matches.lib) ($($matches.tfm), $Agent)"
    } else {
        $unknownCounter += 1;
        $runTitle = "unknown$unknownCounter ($Agent)";
    }

    Write-Host "##vso[results.publish type=VSTest;runTitle=$runTitle;publishRunAttachments=true;resultFiles=$_;failTaskOnFailedTests=true;testRunSystem=VSTS - PTR;]"
  }
}
