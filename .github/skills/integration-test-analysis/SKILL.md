---
name: integration-test-analysis
description: Investigate Visual Studio integration test failures from Azure DevOps builds. Use when investigating integration test timeouts, crashes, or failures in the roslyn-integration-CI pipeline. Also use when asked "why are integration tests failing", "integration test timeout", "VS integration tests", or given AzDO build URLs from the roslyn-integration-CI pipeline.
---

# Visual Studio Integration Test Failure Analysis

Investigate Visual Studio integration test failures in the `roslyn-integration-CI` Azure DevOps pipeline. Downloads and analyzes published test artifacts including exception logs, screenshots, MEF errors, and VSIX installer logs.

**Workflow**: Run the script with a BuildId → it fetches the build timeline, identifies failed/canceled integration test jobs, downloads the published test artifacts, and parses exception XMLs and error logs → read the structured output and `[INTEGRATION_TEST_SUMMARY]` JSON → synthesize root cause analysis. The script collects data; you generate the diagnosis.

## When to Use This Skill

Use this skill when:
- Investigating failures or timeouts in the `roslyn-integration-CI` pipeline
- A build URL points to `dev.azure.com/dnceng-public` with integration test jobs (`VS_Integration_*`)
- Asked "why are integration tests timing out", "integration test failures", or "VS integration tests red"
- You need to analyze exception Activity XML logs, screenshots, or MEF errors from integration test runs
- Integration test jobs are canceled (usually due to timeout) and you need to understand why

> ⚠️ This skill is specific to **Roslyn VS integration tests**. For general CI analysis (compiler tests, Helix failures, build errors), use the `ci-analysis` skill instead.

## Quick Start

```sh
# Ensure powershell tool is restored
dotnet tool restore
```

```powershell
# Analyze integration test failures by build ID
dotnet pwsh -File .github/skills/integration-test-analysis/scripts/Get-IntegrationTestStatus.ps1 -BuildId 1354185

# Analyze with verbose artifact download (downloads and parses exception XMLs)
dotnet pwsh -File .github/skills/integration-test-analysis/scripts/Get-IntegrationTestStatus.ps1 -BuildId 1354185 -DownloadArtifacts

# Limit artifact download size (default 200MB)
dotnet pwsh -File .github/skills/integration-test-analysis/scripts/Get-IntegrationTestStatus.ps1 -BuildId 1354185 -DownloadArtifacts -MaxArtifactSizeMB 500
```

## Key Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-BuildId` | Yes | — | Azure DevOps build ID from the roslyn-integration-CI pipeline |
| `-DownloadArtifacts` | No | `$false` | Download and analyze published test artifacts (exception XMLs, screenshots, MEF errors) |
| `-MaxArtifactSizeMB` | No | `200` | Maximum artifact size in MB to download. Artifacts larger than this are skipped with a warning |
| `-Organization` | No | `dnceng-public` | Azure DevOps organization |
| `-Project` | No | `public` | Azure DevOps project |

## What the Script Does

### Step 1: Build Timeline Analysis
1. Fetches the build status and timeline from Azure DevOps
2. Identifies integration test jobs (`VS_Integration_*`) and their status (succeeded/failed/canceled)
3. Calculates job durations and detects timeouts (canceled jobs that ran close to the pipeline timeout limit)
4. Extracts the pipeline timeout parameter for comparison

### Step 2: Job Log Analysis
For each failed or canceled integration test job:
1. Fetches the "Run Integration Tests" step log
2. Parses the test runner output to determine:
   - How many test assemblies were running/queued/completed
   - Which test assembly was running when the failure/timeout occurred
   - VSIX installation status and timing
   - The test runner command line (configuration, architecture, flags)

### Step 3: Published Artifact Analysis (with `-DownloadArtifacts`)
1. Lists all published artifacts and identifies test log artifacts matching the failing configuration
2. Downloads artifacts within the size limit as ZIP files
3. Extracts and analyzes key diagnostic files:

| File Pattern | What It Contains |
|-------------|-----------------|
| `*.Activity.xml` | Exception logs per test — contains full stack traces, VS activity log entries |
| `*.png` / `*.jpg` (in Screenshots/) | Screenshots taken during test execution — shows VS state at failure |
| `*.png` / `*.jpg` (at root level) | Screenshots from timeouts — shows VS state when the job was killed |
| `MEFErrors*.txt` | MEF composition errors — missing exports, failed service activation |
| `VSIXInstaller*.log` | VSIX installation log — shows if Roslyn VSIX sideloading succeeded |
| `ServiceHub*.log` | ServiceHub process logs — shows OOP service activation failures |
| `StartingBuild.png` | Screenshot taken when the test harness first starts VS |

### Step 4: Exception XML Parsing
For each `*.Activity.xml` file found:
1. Parses the XML structure to extract error entries
2. Groups errors by source (e.g., `VisualStudioErrorReportingService`, `GlobalBrokeredServiceContainer`)
3. Identifies the root cause error (e.g., assembly load failures, service activation failures)
4. Extracts affected features and the common error pattern

## Interpreting Results

### Timeout Pattern (Most Common)
When integration test jobs are **canceled** after ~150 minutes:
- Check if the test runner was stuck at "1 running, N queued, 0 completed" — this means the **first test assembly hung**
- Check if VSIX installation succeeded — VSIX install failures prevent tests from running
- Look at timeout screenshots (root-level PNGs) to see the VS state when killed

### Service Activation Failures
When exception XMLs show `ServiceActivationFailedException`:
- Look for the **inner exception** — usually an assembly load failure (e.g., `System.Runtime` version mismatch)
- These cascade: one missing assembly breaks ALL remote Roslyn services
- Check the assembly version being requested vs. what's available on the CI queue

### MEF Composition Errors
When `MEFErrors*.txt` files are present:
- Missing exports indicate VSIX packaging issues
- Failed imports indicate version mismatches between Roslyn components

### VSIX Installation Failures
When `VSIXInstaller*.log` shows errors:
- `FileNotFoundException` during install — VS instance state is corrupted or locked
- Exit code != 0 — the Roslyn VSIX didn't install properly, tests will fail or behave unexpectedly

## Output Format

The script outputs human-readable console text followed by a structured JSON block:

```
[INTEGRATION_TEST_SUMMARY]
{
  "buildId": 1354185,
  "buildUrl": "https://dev.azure.com/...",
  "buildResult": "failed",
  "pipelineTimeout": 150,
  "jobs": [
    {
      "name": "VS_Integration_Debug_64",
      "result": "canceled",
      "durationMinutes": 153.7,
      "timedOut": true,
      "configuration": "Debug",
      "testRunnerStatus": "1 running, 3 queued, 0 completed",
      "vsixInstallSuccess": true,
      "lastTestActivity": "01:11:59"
    }
  ],
  "artifacts": {
    "downloaded": true,
    "exceptionFiles": ["01.15.40-CSharpTyping.TypingInPartialType-AggregateException.Activity.xml"],
    "screenshotFiles": ["StartingBuild.png", "01.15.40-Timeout.png"],
    "mefErrorFiles": [],
    "vsixLogFiles": ["VSIXInstaller-xxx.log"],
    "parsedExceptions": [
      {
        "fileName": "01.15.40-CSharpTyping.TypingInPartialType-AggregateException.Activity.xml",
        "testName": "CSharpTyping.TypingInPartialType",
        "exceptionType": "AggregateException",
        "rootCausePattern": "assembly-load-failure",
        "primaryError": "Could not load file or assembly 'System.Runtime, Version=10.0.0.0'",
        "totalErrors": 147,
        "affectedFeatures": ["Solution Events", "Asset synchronization", "..."],
        "errorSources": { "GlobalBrokeredServiceContainer": 80, "VisualStudioErrorReportingService": 67 },
        "vsVersion": "17.14.0"
      }
    ],
    "parsedMefErrors": [],
    "parsedVsixLogs": [
      { "fileName": "VSIXInstaller-xxx.log", "success": true, "errors": [] }
    ]
  }
}
[/INTEGRATION_TEST_SUMMARY]
```

## Analysis Workflow for the Agent

### Step 0: Gather Context
Before running the script:
- Check the PR description and recent commits for clues (TFM changes, queue changes, new test additions)
- Note the pipeline name — this skill is for `roslyn-integration-CI` only

### Step 1: Run the Script
```powershell
pwsh -File .github/skills/integration-test-analysis/scripts/Get-IntegrationTestStatus.ps1 -BuildId <ID> -DownloadArtifacts
```

### Step 2: Analyze the Output
- Read the human-readable output for the timeline and job status
- Parse the `[INTEGRATION_TEST_SUMMARY]` JSON for structured data
- Examine `artifacts.parsedExceptions`, `artifacts.parsedMefErrors`, and `artifacts.parsedVsixLogs` to understand what went wrong
- Cross-reference job-level signals (e.g., `timedOut`, `testRunnerStatus`, `vsixInstallSuccess`) with the parsed artifact data to synthesize a root cause
- If artifacts were too large to download, inform the user and suggest manual inspection

### Step 3: Cross-Reference
- Compare with PR changes — did a TFM change, queue change, or packaging change cause this?
- Check if the same failure pattern exists in previous builds on the same PR
- Look at the VS version on the queue vs. what the tests expect

## References

- [Integration Test Pipeline](../../azure-pipelines-integration.yml) — Pipeline YAML with queue and timeout parameters
- [Integration Test Helix Template](../../eng/pipelines/test-integration-helix.yml) — Job template for integration tests
- [ServiceHub Configuration](../../eng/targets/GenerateServiceHubConfigurationFiles.targets) — How ServiceHub services are configured
- [Target Frameworks](../../eng/targets/TargetFrameworks.props) — TFM definitions (NetVS, NetRoslyn, etc.)
