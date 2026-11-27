# VMR Codeflow Reference

## Key Concepts

### Codeflow Types
- **Backflow** (VMR → product repo): Automated PRs created by Maestro that bring VMR source updates + dependency updates into product repos (e.g., `dotnet/sdk`). These are titled `[branch] Source code updates from dotnet/dotnet`.
- **Forward flow** (product repo → VMR): Changes from product repos flowing into the VMR. These are titled `[branch] Source code updates from dotnet/<repo>`.

### Staleness
When a product repo pushes changes to the VMR (forward flow merges) while a backflow PR is already open, Maestro blocks further codeflow updates to that PR. The bot posts a warning comment with options:
1. Merge the PR as-is, then Maestro creates a new PR with remaining changes
2. Close the PR and let Maestro open a fresh one (loses manual commits)
3. Force trigger: `darc trigger-subscriptions --id <subscription-id> --force` (manual commits may be reverted)

### Key Files
- **`src/source-manifest.json`** (in VMR): Tracks the exact commit SHA for each product repo synchronized into the VMR. This is the authoritative source of truth.
- **`eng/Version.Details.xml`** (in product repos): Tracks dependencies and includes a `<Source>` tag for codeflow tracking.

## PR Body Metadata Format

Codeflow PRs have structured metadata in their body:

```
[marker]: <> (Begin:<subscription-id>)
## From https://github.com/dotnet/dotnet
- **Subscription**: [<subscription-id>](https://maestro.dot.net/subscriptions?search=<subscription-id>)
- **Build**: [<build-number>](<azdo-build-url>) ([<bar-id>](<maestro-channel-url>))
- **Date Produced**: <date>
- **Commit**: [<vmr-commit-sha>](<vmr-commit-url>)
- **Commit Diff**: [<from>...<to>](<compare-url>)
- **Branch**: [<branch>](<branch-url>)
[marker]: <> (End:<subscription-id>)
```

## Darc CLI Commands

The `darc` tool (Dependency ARcade) manages dependency flow in the .NET ecosystem. Install via `eng\common\darc-init.ps1` in any arcade-enabled repo.

### Essential Commands for Codeflow Analysis

#### Get subscription details
```bash
# Find all subscriptions flowing to a repo
darc get-subscriptions --target-repo dotnet/sdk --source-repo dotnet/dotnet

# Output shows subscription ID, channel, update frequency, merge policies
```

#### Trigger a codeflow update
```bash
# Normal trigger (only works if not stale)
darc trigger-subscriptions --id <subscription-id>

# Force trigger (works even when stale, but may revert manual commits)
darc trigger-subscriptions --id <subscription-id> --force

# Trigger with a specific build
darc trigger-subscriptions --id <subscription-id> --build <bar-build-id>
```

#### Get build information
```bash
# Get BAR build details by ID (found in PR body or AzDO logs)
darc get-build --id <bar-build-id>

# Get latest build for a repo on a channel
darc get-latest-build --repo dotnet/dotnet --channel ".NET 11 Preview 1"
```

#### Check subscription health
```bash
# See if dependencies are missing subscriptions or have issues
darc get-health --channel ".NET 11 Preview 1"
```

#### Simulate a subscription update locally
```bash
# Dry-run to see what a subscription would update
darc update-dependencies --subscription <subscription-id> --dry-run
```

### VMR-Specific Commands

```bash
# Resolve codeflow conflicts locally
darc vmr resolve-conflict --subscription <subscription-id> --build <bar-build-id>

# Flow source from VMR → local repo
darc vmr backflow --subscription <subscription-id>

# Flow source from local repo → local VMR
darc vmr forwardflow --subscription <subscription-id>

# Get version (SHA) of a repo in the VMR
darc vmr get-version

# Diff VMR vs product repos
darc vmr diff
```

### Halting and Restarting Dependency Flow

- **Disable default channel**: `darc default-channel-status --disable --id <id>` — stops new builds from flowing
- **Disable subscription**: `darc subscription-status --disable --id <id>` — stops flow between specific repos
- **Pin dependency**: Add `Pinned="true"` to dependency in `Version.Details.xml` — prevents specific dependency from updating

## API Endpoints

### GitHub API
- PR details: `GET /repos/{owner}/{repo}/pulls/{pr_number}`
- PR comments: `GET /repos/{owner}/{repo}/issues/{pr_number}/comments`
- PR commits: `GET /repos/{owner}/{repo}/pulls/{pr_number}/commits`
- Compare commits: `GET /repos/{owner}/{repo}/compare/{base}...{head}`
- File contents: `GET /repos/{owner}/{repo}/contents/{path}?ref={branch}`

### VMR Source Manifest
```
GET /repos/dotnet/dotnet/contents/src/source-manifest.json?ref={branch}
```
Returns JSON with `repositories[]` array, each having `path`, `remoteUri`, `commitSha`.

### Maestro/BAR REST API
Base URL: `https://maestro.dot.net`
- Swagger: `https://maestro.dot.net/swagger`
- Get subscriptions: `GET /api/subscriptions`
- Get builds: `GET /api/builds`
- Get build by ID: `GET /api/builds/{id}`

## Common Scenarios

### 1. Codeflow is stale — a fix landed but hasn't reached the PR
**Symptoms**: Tests failing on the codeflow PR; the fix is merged in a product repo.
**Diagnosis**: Compare `source-manifest.json` on VMR branch HEAD vs the PR's VMR snapshot commit.
**Resolution**: Close PR + reopen, or force trigger the subscription.

### 2. Opposite codeflow merged — staleness warning
**Symptoms**: Maestro bot comment saying "codeflow cannot continue".
**Diagnosis**: Check PR comments for the warning. Check if forward flow PRs merged after the backflow PR was opened.
**Resolution**: Follow the options in the bot's comment.

### 3. Manual commits on the codeflow PR
**Symptoms**: Developers added manual fixes to unblock the PR (baseline updates, workarounds).
**Diagnosis**: Analyze PR commits to identify non-maestro commits.
**Risk**: Closing the PR loses these. Force-triggering may revert them.
