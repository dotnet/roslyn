# VMR Build Topology and Staleness Diagnosis

## Overview

When backflow PRs are missing across multiple repositories simultaneously, the root cause
is usually not Maestro — it's that the VMR can't build successfully, so no new channel
builds are produced, and subscriptions have nothing to trigger on.

This reference explains how to diagnose that situation using publicly available signals.

## Build Pipeline Structure

The VMR (`dotnet/dotnet`) has two tiers of builds:

### Public CI (validation only)
- **AzDO org**: `dnceng-public`
- **Project**: `public` (ID: `cbb18261-c48f-4abb-8651-8cdcb5474649`)
- **Pipeline**: `dotnet-unified-build` (definition 278)
- **Purpose**: Validates PRs and runs scheduled CI on `refs/heads/main` and release branches
- **Does NOT publish** to Maestro channels — cannot trigger subscriptions

### Official builds (channel publishing)
- **AzDO org**: `dnceng` (internal, requires auth)
- **Purpose**: Produces signed builds that publish to Maestro channels (e.g., `.NET 11.0.1xx SDK`)
- **These are the builds that trigger Maestro subscriptions and create backflow PRs**
- Not queryable without internal access

### Key insight
When investigating stale backflow, the **public CI builds are a useful proxy**. If the public
scheduled build on `refs/heads/main` is failing, the official build is almost certainly
failing too (they build the same source). A string of failed public builds strongly suggests
the official pipeline is also broken.

## Checking Official Build Freshness (aka.ms)

The most direct way to check if official VMR builds are producing output is to query
the SDK blob storage via `aka.ms` shortlinks. When official builds succeed, they publish
SDK artifacts to `ci.dot.net`. We can check when the latest build was published.

### How it works

1. Resolve the aka.ms redirect (returns 301 with the blob URL):
   ```
   https://aka.ms/dotnet/{channel}/daily/dotnet-sdk-win-x64.zip
   ```
   Example channels: `11.0.1xx`, `11.0.1xx-preview1`, `10.0.3xx`, `10.0.1xx`

2. The 301 Location header gives the actual blob URL on `ci.dot.net`, which includes
   the version number in the path.

3. HEAD the blob URL — the `Last-Modified` header tells you exactly when the build was
   published.

### Example (PowerShell)

```powershell
Add-Type -AssemblyName System.Net.Http
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [System.Net.Http.HttpClient]::new($handler)

# Step 1: Resolve aka.ms → ci.dot.net blob URL
$resp = $client.GetAsync("https://aka.ms/dotnet/11.0.1xx/daily/dotnet-sdk-win-x64.zip").Result
$blobUrl = $resp.Headers.Location.ToString()  # Only if StatusCode is 301
$resp.Dispose()

# Step 2: HEAD the blob for Last-Modified
$head = Invoke-WebRequest -Uri $blobUrl -Method Head -UseBasicParsing
$published = [DateTimeOffset]::Parse($head.Headers['Last-Modified']).UtcDateTime
$age = [DateTime]::UtcNow - $published

$client.Dispose()
$handler.Dispose()
```

### Interpreting results
- **< 1 day old**: Official builds are healthy for this channel
- **1-2 days old**: Normal for daily builds, especially over weekends
- **3+ days old**: Official builds are likely failing — investigate further
- **Multiple channels stale simultaneously**: Strong signal of a systemic VMR build problem

### Validating with darc (when auth is available)

The aka.ms approach is an auth-free proxy. When `darc` is installed and authenticated,
you can get the authoritative answer directly from Maestro:

```bash
# Latest build on a channel (exact match for what triggers subscriptions)
darc get-latest-build --repo dotnet/dotnet --channel ".NET 11.0.1xx SDK"

# Check what build a subscription last acted on
darc get-subscriptions --source-repo dotnet/dotnet --target-repo dotnet/aspnetcore
```

The `Date Produced` from `darc get-latest-build` will be ~6 hours earlier than the
aka.ms blob `Last-Modified` (due to signing/publishing delay), but they refer to the
same build. If the subscription's `Last Build` SHA matches the channel's latest build,
then Maestro already fired — no newer builds exist.

### Channel-to-branch mapping

| Channel | VMR branch | Backflow targets |
|---------|-----------|-----------------|
| `11.0.1xx` | `main` | runtime, sdk, aspnetcore (main) |
| `11.0.1xx-preview1` | `release/11.0.1xx-preview1` | runtime, sdk, aspnetcore (preview) |
| `10.0.3xx` | `release/10.0.3xx` | sdk (release/10.0.3xx) |
| `10.0.2xx` | `release/10.0.2xx` | sdk (release/10.0.2xx) |
| `10.0.1xx` | `release/10.0.1xx` | runtime, sdk, aspnetcore (release/10.0) |

### Cross-referencing with Version.Details.xml and PR metadata

There are two sources of truth for what VMR build a repo is synced to:

**1. `eng/Version.Details.xml` in the target repo (authoritative):**
```xml
<Source Uri="https://github.com/dotnet/dotnet" Mapping="sdk"
  Sha="ec846aee7f12180381c444dfeeba0c5022e1d110" BarId="297974" />
```
- `Sha` = the exact VMR commit the repo is synced to
- `BarId` = the Maestro build ID (queryable via `darc get-build --id 297974` for date/channel)
- Dependency version strings encode build dates (e.g., `26069.105` → year 26, day-code 069)

**2. Backflow PR body (when a PR is open):**
```
- **Date Produced**: February 4, 2026 11:05:10 AM UTC
- **Build**: [20260203.11](...) ([300217](https://maestro.dot.net/channel/8298/.../build/300217))
```

**Comparing against aka.ms build date:**
- If they match → the backflow PR is based on the latest successful build
- If the aka.ms build is newer → a newer build succeeded but hasn't triggered backflow yet
- If the aka.ms build matches the PR but is old → no new successful builds since

## Querying Public VMR CI Builds

Public CI builds (separate from official builds) can confirm whether the VMR source is
buildable. These don't publish to channels but use the same source.

### AzDO REST API endpoints

Recent scheduled builds on a branch:
```
GET https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=278&branchName=refs/heads/main&$top=5&api-version=7.0
```

Last successful build:
```
GET https://dev.azure.com/dnceng-public/public/_apis/build/builds?definitions=278&branchName=refs/heads/main&resultFilter=succeeded&$top=1&api-version=7.0
```

Build timeline (to find failing jobs):
```
GET https://dev.azure.com/dnceng-public/public/_apis/build/builds/{buildId}/timeline?api-version=7.0
```

### Interpreting results
- **`reason: schedule`** — Scheduled daily builds, closest proxy to official builds
- **`reason: pullRequest`** — PR validation only
- **`result: failed`** with consecutive scheduled builds — strong signal of broken VMR
- Check the timeline for which jobs/stages failed to understand the root cause

## Diagnosing Widespread Backflow Staleness

### Pattern: Multiple repos missing backflow simultaneously

When `CheckMissing` shows missing backflow across 3+ repos (e.g., runtime, SDK, aspnetcore
all stale), this is almost always a VMR build problem, not a Maestro problem.

**Diagnosis steps:**

1. **Check public VMR builds**: Query the last 5 scheduled builds on the affected branch.
   If all are failing, the VMR build is broken.

2. **Find the failure**: Get the timeline of the most recent failed build. Look for failed
   stages/jobs — common failures include:
   - **macOS signing** (SignTool crashes on non-PE files)
   - **Windows build** (individual repo build failures within the VMR)
   - **Source-build validation** (packaging or dependency issues)

3. **Check for known issues**: Search `dotnet/dotnet` issues with label `[Operational Issue]`
   or search for the error message.

4. **Check the last successful build date**: A gap of days or weeks confirms the VMR has been
   broken for an extended period.

### Pattern: Single repo missing backflow

When only one repo is missing backflow but others are healthy, the issue is more likely:
- Maestro subscription disabled or misconfigured
- The specific repo's forward flow is blocking (conflict or staleness)
- Channel mismatch

Use `darc get-subscriptions --source-repo dotnet/dotnet --target-repo dotnet/<repo>` to check.

## The Bootstrap / Chicken-and-Egg Problem

The VMR builds arcade and other infrastructure from source. When an infrastructure fix
(e.g., in `dotnet/arcade`) is needed to unblock the VMR build itself, a circular dependency
can occur:

1. Arcade fix merges in `dotnet/arcade`
2. Arcade forward-flows to VMR (`dotnet/dotnet`)
3. VMR now has the fix **in source**, but the build tooling used to build may still be the
   old version (from a previous successful bootstrap)
4. The build fails because the **bootstrap SDK** (cached from a prior build) doesn't have
   the fix yet

**Resolution** (by VMR maintainers):
- Re-bootstrap: Build a new `source-built-sdks` package from a working state
- Manual intervention: Patch the bootstrap or skip the failing step
- Wait for a full re-bootstrap cycle after a milestone release

This is not something that can be fixed by triggering subscriptions or resolving conflicts.
When you see this pattern, flag it as needing VMR infrastructure team intervention.

## Channels and Subscription Flow

```
dotnet/arcade ──forward flow──► dotnet/dotnet (VMR)
dotnet/runtime ─forward flow──► dotnet/dotnet (VMR)
dotnet/sdk ────forward flow──► dotnet/dotnet (VMR)
    ...other repos...

dotnet/dotnet (VMR)
    │
    ├── official build succeeds
    │       │
    │       ▼
    │   publishes to channel (e.g., ".NET 11.0.1xx SDK")
    │       │
    │       ▼
    │   Maestro fires subscriptions
    │       │
    │       ├──► dotnet/runtime backflow PR
    │       ├──► dotnet/sdk backflow PR
    │       ├──► dotnet/aspnetcore backflow PR
    │       └──► ...etc
    │
    └── official build FAILS
            │
            ▼
        nothing publishes → no subscriptions fire → all backflow stalls
```

## Quick Reference: Common VMR Build Failures

| Failure | Symptom | Root cause |
|---------|---------|------------|
| SignTool crash | `Unknown file format` in Sign.proj on macOS | Non-PE file in signing input (e.g., tar.gz) |
| Repo build failure | `error MSB...` in a specific repo's build | Source incompatibility within VMR |
| Source-build validation | Packaging or prebuilt detection errors | New prebuilt dependency introduced |
| Infrastructure timeout | Build exceeds time limit | Resource contention or build perf regression |
