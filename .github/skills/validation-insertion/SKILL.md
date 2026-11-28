---
name: validation-insertion
description: >-
  Create a Visual Studio validation insertion for a Roslyn PR by running Roslyn
  PR Validation, finding the generated insertion PR and its source branch, and
  monitoring builds, RPS/DDRITs, and Speedometer through a results summary.
  Use when asked to test a Roslyn PR in VS, create a validation insertion,
  or get insertion performance numbers.
---

# Roslyn Validation Insertion

Validate an existing Roslyn PR in Visual Studio without merging it. Follow the
payload from the Roslyn commit through the insertion PR and CloudBuild to the
actual performance runs; a successful trigger alone is not a performance result.

## Scope and permissions

- Creating a validation insertion runs a pipeline that creates a draft VS PR.
  Do not merge it, enable auto-complete, or mark it ready for review.
- Do not post comments, reviews, or edit PR descriptions without explicit
  permission for the proposed content.
- VS source changes are optional, not part of the default workflow. Obtain
  explicit permission for any cherry-pick, commit, push, or signing override.
  Honor advance authorization already given; do not repeatedly seek approval
  for the same operation. Never force-push or rewrite history.
- Use authenticated tooling and configured Git remotes unchanged. Do not print
  tokens or persist credentials in monitoring state.
- Keep run-specific state and reports in the session workspace, not this skill
  or other checked-in documentation.

## 1. Capture the request and configure monitoring

Get the Roslyn PR number or URL and read its live metadata and head SHA:

```powershell
gh pr view <pr> --repo dotnet/roslyn --json number,url,title,headRefOid,baseRefName,state
```

Use the full head SHA as `CommitSHA`. Determine the VS target from
`eng/config/PublishData.json` on the pipeline branch unless the user requested
an override. Record any requested toolset or optimization settings; preserve
the pipeline defaults otherwise.

During monitoring setup, use the host's question tool to ask:

> Do you have a local Visual Studio repository you want me to use for context
> and skills when diagnosing failed builds, DDRITs, or Speedometer runs?
> Provide its path, or choose to skip failure diagnosis.

Offer a path field and a skip option. If the user already supplied a checkout
for this purpose, confirm that choice rather than asking them to find it again.
If they decline or have no checkout, record that failure diagnosis is disabled.
Continue queueing, monitoring, collecting reported failures, and summarizing
results, but skip source-based investigation and potential-cause analysis. Do
not search for another checkout or infer permission from one on disk.

If a checkout is supplied, verify the path, read its agent instructions, and
discover its applicable build, PR, test, and performance investigation skills.
Use those skills for diagnosis, subject to the host's skill-loading consent.
This grants read-only context, not permission to modify that checkout.

Create session state containing the PR/head, target, settings, optional VS path,
diagnosis choice, run IDs and URLs, source/target/merge SHAs, policy evaluation
IDs, child runs, payload evidence, failures, retries, and monitoring handle.
Reload this state when resuming rather than restarting the insertion.

## 2. Run Roslyn PR Validation

The authoritative pipeline is [`azure-pipelines-pr-validation.yml`](../../../azure-pipelines-pr-validation.yml),
with insertion behavior in [`eng/pipelines/insert.yml`](../../../eng/pipelines/insert.yml).
Verify the live definition and parameters before queueing. The DevDiv entry
point is **Roslyn PR Validation**, definition **8972**, organization
`https://dev.azure.com/devdiv`, project **DevDiv**.

Run the pipeline from its own `main` branch, passing the Roslyn PR commit as a
parameter, not as the pipeline's source revision:

```powershell
az pipelines run --id 8972 --branch refs/heads/main `
  --org https://dev.azure.com/devdiv --project DevDiv `
  --parameters PRNumber=<pr> CommitSHA=<full-head-sha> EnforceLatestCommit=true `
    InsertToolset=true SkipApplyOptimizationData=false VisualStudioBranchName=default
```

On Windows, omit `VisualStudioCherryPickSHA` to use its default. Passing the
literal `(default)` through `az.cmd` can break command parsing. Do not supply
a cherry-pick unless the user requested it.

Save the returned build ID and URL immediately. Monitor Build, Publish, and
Insert to VS. A successful build without a completed insertion is not enough.
Record the BAR build ID when available. Verify the `PRNumber` and commit tags,
parameters, and insertion logs: the build's top-level `sourceVersion` identifies
the pipeline checkout, not necessarily the Roslyn payload.

With `EnforceLatestCommit=true`, setup verifies the head and normally builds
the Roslyn PR merge ref, as implemented in `eng/setup-pr-validation.ps1`.
If the head moved, report the mismatch and refresh the requested revision;
do not silently disable this protection or reuse stale results.

## 3. Find the real insertion PR and branch

Read the **Create VS Insertion PR** task output in **Insert to VS** for the
created PR link. If absent, query active VS PRs narrowly by target branch and
creation window, then inspect candidate descriptions and insertion provenance.
Use the available AzDO tools, or the Azure DevOps CLI:

```powershell
az repos pr list --repository VS --target-branch <vs-target> --status active `
  --org https://dev.azure.com/devdiv --project DevDiv
az repos pr show --id <insertion-pr> --org https://dev.azure.com/devdiv
```

Require evidence linking the candidate to this validation build and Roslyn PR
and SHA. A matching title, newest timestamp, or an earlier insertion of the same
PR is insufficient. If evidence is ambiguous, resolve it before queueing runs.

Record the PR URL and its actual `sourceRefName`, `targetRefName`, and
`lastMergeSourceCommit`, `lastMergeTargetCommit`, and `lastMergeCommit` IDs.
The source is the bot-created insertion branch, not the Roslyn branch or the
synthetic `refs/pull/<insertion-pr>/merge` ref. Keep the insertion draft.

If an authorized VS patch is required, use a separate worktree from that source
branch. Verify the patch is not already present, preserve the requested content
and message, push normally through the configured remote, and verify the PR's
new source and regenerated merge commits before proceeding. Use
`git -c commit.gpgsign=false cherry-pick --no-edit <sha>` only when that signing
override is authorized. If an optimized VS remote hides the branch, consult the
checkout's repository tooling; do not substitute a remote URL or force-push.
Do not require a local VS checkout when no source modification is needed.

## 4. Queue policy-backed validation

Discover the insertion PR's live policy evaluations:

```powershell
az repos pr policy list --id <insertion-pr> --org https://dev.azure.com/devdiv
```

Match policies by their configured build definitions. These are discovery hints,
not reusable evaluation IDs:

| Policy | Definition |
|--------|------------|
| CloudBuild - PR | 10310 |
| Request Perf DDRITs | 11362 |
| Request Speedometer Perf Run | 14844 |

Reuse existing queued/running runs only if they belong to the intended payload.
Queue missing or stale evaluations using the live IDs:

```powershell
az repos pr policy queue --id <insertion-pr> --evaluation-id <evaluation-id> `
  --org https://dev.azure.com/devdiv
```

Draft PR policies may not automatically restart after a push. Verify their run
IDs and revisions explicitly. Queue CloudBuild and the standard DDRIT/RPS and
Speedometer trigger policies; follow any required-test dependencies. Do not
queue Speedometer Heavy or guess at result-pipeline definitions. The insertion
pipeline can request Speedometer automatically, but that run is reusable only
if its payload is still current.

For each performance trigger, inspect its `BuildArtifacts/buildId.json` artifact
and resolve the referenced CloudBuild. Require a successful CloudBuild on
`refs/pull/<insertion-pr>/merge` at the intended merge SHA. The trigger's source
SHA alone does not establish its tested payload. Follow child run IDs and links
from the trigger logs/artifacts and live policies until the actual performance
results appear. Persist those relationships in session state.

## 5. Monitor to actual results

Set up the host's persistent monitoring facility when available, normally at
30-minute intervals for unattended runs. Save its handle and include the session
state path in the monitoring prompt. Do not claim unattended monitoring if no
facility is active; report that limitation. Avoid duplicate schedules and runs.

Each check should:

1. Reload state; query the known builds, PR policies, and discovered child runs.
   Use narrow time windows and definitions when discovering new runs. Re-fetch
   logs only for newly failed tasks or deeper investigation.
2. Check for source, target, or merge revision changes and preserve separate
   attempts. Mark older results superseded rather than mixing their payloads.
3. Distinguish queued, running, failed-before-tests, completed-with-regressions,
   and completed-without-reported-regressions. A rejected CloudBuild can fail
   both triggers before any performance test executes.
4. Collect build and required-test failures, DDRIT/RPS reports, and standard
   Speedometer reports, including missing scenarios and incomplete runs.
5. If diagnosis was enabled, use the supplied VS context and skills to trace
   failures. Separate observed errors, demonstrated root causes, and hypotheses.
   Compare the same failure on the target branch or another insertion before
   attributing it to Roslyn. Do not dismiss failures as infrastructure without
   evidence. If diagnosis was skipped, report the pipeline's error and blocker
   without proposing causes.
6. Retry only within the user's authorization and with evidence that the retry
   can help. If asked to wait for a branch fix, continue checking at the agreed
   cadence after failures; verify a relevant fix or successful corresponding
   branch build before requeueing CloudBuild and stale dependent perf policies.
   Do not loop blind retries or automatically patch source.
7. Save progress and retry reasons, and continue until both performance suites
   have terminal results for the intended payload. A failed trigger is a terminal
   blocked attempt, not a performance verdict. If recovery monitoring was
   requested, keep it active until recovery succeeds or the user stops it.

Stop the monitoring schedule when its goal is met or the user cancels it. If
the attempt is blocked without authorized recovery, deliver the blocker and
stop rather than claiming that performance validation completed.

## 6. Summarize for the user

Lead with the overall verdict: usable results, regressions, still pending, or
blocked with no performance numbers. Link the Roslyn PR, validation build,
insertion PR, CloudBuild, and actual DDRIT/RPS and Speedometer results. Include
the tested Roslyn SHA and VS source/merge revisions so the payload is auditable.

Use a compact table for Build/tests, DDRIT/RPS, and Speedometer with status and
evidence links. For performance results, identify the baseline and candidate,
scenario/metric, units, direction, absolute and percentage delta when available,
and the report's significance/noise classification. Separate meaningful
regressions, improvements, and inconclusive measurements. Do not invent
thresholds, interpret missing results as passes, or call a trigger success a pass.

Include failure analysis and potential causes only when the user opted into VS
diagnosis, with supporting evidence and uncertainty. Otherwise state that
diagnosis was skipped. Report remaining blockers, superseded attempts, and
whether monitoring remains active. Leave all PRs unmerged.
