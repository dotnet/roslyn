---
name: snap
description: "Perform a branch snap (release branch cut) for dotnet repos like dotnet/roslyn. Use when: snapping a branch, cutting a release branch, creating a release branch, merging main into release, updating VS insertion config, updating darc subscriptions for a snap, moving milestones, or asked about snap workflow."
argument-hint: Which repo and branches to snap (e.g., snap main to release/dev18.6)?
---

# Branch Snap

Perform a branch snap (release branch cut) for dotnet repositories. A snap shifts the content of named branches forward by one VS minor version in a cascade.

> **IMPORTANT**: This skill makes destructive changes (creates branches, opens PRs, updates subscriptions, moves milestones). Always gather info first, present the full plan, and get explicit user confirmation before executing any modifications.

> **NOTE**: This skill works for multiple dotnet repos (e.g., `dotnet/roslyn`). Do not assume `dotnet/roslyn` — always confirm the repo.

> **SKILL MAINTENANCE**: If you deviate from this skill during execution (e.g., a step doesn't work as described, a new step is needed, or the process has changed), remind the user to update this skill file so future snaps benefit from the fix.

> **SESSION**: A snap spans multiple days (initial snap, then a follow-up after the scheduled VS snap). Recommend the user reuse the same chat session throughout one snap cycle so context (PR numbers, branch names, channel IDs, schedule dates, etc.) is preserved. If starting a new session, review session memory for prior snap state.

## Branch Model

Roslyn (and similar repos) use three named branches that cascade during a snap:

| Branch | Role | Example before snap | Example after snap |
|--------|------|--------------------|--------------------|
| `main` | Active development, inserts to VS main | 18.6 | **18.7** (bumped) |
| `release/insiders` | Preview/insiders ring, inserts to `rel/insiders` | 18.5 | **18.6** (gets main's content) |
| `release/stable` | Stable ring, inserts to `rel/stable` | 18.4 | **18.5** (gets insiders' content) |

A "snap for 18.6" means: main is currently at 18.6 and that content moves to insiders. The cascade is:

1. **Merge `release/insiders` → `release/stable`**: stable gets the old insiders content (18.5)
2. **Merge `main` → `release/insiders`**: insiders gets main's content (18.6) up to a specific commit
3. **Bump `main`** version to 18.7

After the snap, the old stable VS version (18.4 in this example) is retired from the named-branch cascade. If that content must continue servicing an SDK band, preserve it first by creating a long-lived branch such as `release/10.0.4xx` directly from the pre-snap `release/stable` commit. The previous SDK servicing branch can then be retired if its flow is no longer needed.

Older `release/dev{version}` branches (e.g., `release/dev18.3` and below) remain for servicing only.

## Prerequisites

Before starting, verify these CLI tools are available:
- `gh` — GitHub CLI, authenticated (`gh auth status`). The user may need to switch accounts via `gh auth switch` to one with push access to their fork.
- `darc` — .NET Arcade/BAR CLI for subscription management (`darc authenticate` must have been run)

Test with:
```
gh --version
darc get-subscriptions --exact --source-repo https://github.com/dotnet/roslyn --target-repo https://github.com/dotnet/dotnet
```

### Fork-based workflow

Snap PRs are typically opened from a user's fork. Ask the user for their fork (e.g., `{username}/roslyn`). Branches are created in the fork via the GitHub API, and PRs are opened cross-fork with `--head {forkOwner}:{branchName}`. Verify the `gh` account has push access to the fork (`gh api repos/{forkOwner}/{repo} --jq '.permissions'`).

## Workflow

The snap process has three phases: **Gather**, **Plan**, **Execute**.

### Phase 1: Gather Information

Collect all relevant state before proposing any changes:

#### 1.1 Determine repo

- Ask which repo (default: the current repo via `gh repo set-default --view`). Note that if working in the Roslyn repo (dotnet/roslyn), then the snap will apply to Roslyn and Razor, as they are both in the repo, so multiple version updates will be necessary.

#### 1.2 Discover branches and versions automatically

Use darc default channels and version files to infer the branch structure. Do **not** ask the user for branches or versions — infer them and present for confirmation.

**Step A — List source-repository default channels** to discover active branches and the build channels they publish to:
```
darc get-default-channels --source-repo https://github.com/{owner}/{repo}
```
Identify the three named branches (`main`, `release/insiders`, `release/stable`), current servicing branches, and their VS and SDK source channels.

> **Do not use this list alone to populate the email's SDK columns.** A source channel selects builds for subscriptions; it does not identify every SDK destination consuming those builds. One source channel can feed multiple VMR branches and therefore multiple SDK versions. Determine actual SDK destinations from subscriptions in step 1.3.

**Step B — Read versions and configs** from all three branches:
- Fetch `eng/Versions.props` from each branch to get the current version.
  - For roslyn: VS version = `Major + 13`.`Minor` (e.g., Roslyn 5.6 → VS 18.6).
- Razor versions are also in `eng/Versions.props` (same file as Roslyn's version), using `Razor`-prefixed property names. Razor has **two independent versions**:
  - **Razor VSIX/Addin version**: use `<RazorVsixVersionPrefix>` directly (e.g., `18.8.1`). Tracks the Visual Studio version like the rest of the snap cascade. `<RazorAddinMajorVersion>` tracks the major.minor (e.g., `18.8`).
  - **Razor SDK version**: read `<RazorMajorVersion>` and `<RazorMinorVersion>` (e.g., `10.4`). Tracks the **.NET SDK band** Razor ships into, **not** the VS version. The mapping is `.NET <Major>.0.<Band>xx SDK` <-> Razor `<Major>.<Band>` (e.g., `.NET 10.0.4xx SDK` <-> Razor `10.4`, `.NET 11.0.1xx SDK` <-> Razor `11.1`).
  - Note: `src/Razor/Directory.Build.props` maps these `Razor`-prefixed properties to the standard MSBuild properties (`MajorVersion`, `MinorVersion`, etc.) for Razor projects. During a snap, only edit `eng/Versions.props` — the Razor props file should not need changes.
- Fetch `eng/config/PublishData.json` from each branch to get insertion config (`vsBranch`, `insertionCreateDraftPR`, `insertionTitlePrefix`).
  - The JSON key is `branchInfo` (roslyn).

**Step B.1 -- Detect Razor SDK version drift** for each branch (roslyn only):
- For each branch, find the SDK default-channels assigned to it from Step A (e.g., `.NET 10.0.4xx SDK`).
- Among those, locate the channel whose major matches the branch's current Razor `RazorMajorVersion`. If multiple channels of the same major are temporarily assigned (e.g., both `.NET 10.0.4xx SDK` and `.NET 10.0.5xx SDK`), pick the **lowest** band -- Razor is versioned to the lowest SDK band it ships into, since higher bands roll forward and can consume the same package.
- Apply the mapping `.NET <Major>.0.<Band>xx SDK` -> expected Razor `<Major>.<Band>`.
- If no matching SDK channel exists for that branch (common on `main` immediately after a previous snap, before the next SDK band channel has been created), record the branch as "no matching SDK channel -- Razor SDK version bump deferred".
- If the expected version differs from the branch's current `RazorMajorVersion.RazorMinorVersion`, flag it as a drift to fix (during snap, see Phase 2 step 5b; during follow-up, see Phase 3 step 3.9).

**Step C — Infer the snap cascade** from the discovered state:
- The snap version is whatever `main` currently targets (e.g., 18.6).
- After snap: main bumps +1 minor, insiders gets main's current version, stable gets insiders' current version.
- Record the pre-snap `release/stable` commit as the candidate source for a new SDK servicing branch.
- Do not infer SDK flow changes yet. Determine them from the subscription topology in step 1.3; SDK flows do not necessarily move one-to-one with the VS branch cascade.

Present a summary like:
```
Snap for VS 18.6 on dotnet/roslyn:
  main:              18.6 -> 18.7
  release/insiders:  18.5 -> 18.6 (receives current main content)
  release/stable:    18.4 -> 18.5 (receives current insiders content)
  Old stable 18.4:   preserve in an SDK servicing branch or retire
  SDK destinations:  pending subscription analysis in step 1.3
```
If Step B.1 detected any Razor SDK version drift on the branches as they exist **today** (e.g., `main` already flows to `.NET 10.0.4xx SDK` but `eng/Versions.props` still says `RazorMajorVersion=10, RazorMinorVersion=0`), call it out explicitly -- it likely means a previous snap missed the bump and should be fixed in the same snap PR. Confirm with the user before proceeding.

#### 1.3 Check darc subscriptions and determine SDK destinations

List existing forward flows, backflows, and VMR default channels:
```
darc get-subscriptions --exact --source-repo https://github.com/{owner}/{repo} --target-repo https://github.com/dotnet/dotnet
darc get-subscriptions --exact --source-repo https://github.com/dotnet/dotnet --target-repo https://github.com/{owner}/{repo}
darc get-default-channels --source-repo https://github.com/{owner}/{repo}
darc get-default-channels --source-repo https://github.com/dotnet/dotnet
```

Also check flows to SDK and runtime if relevant:
```
darc get-subscriptions --exact --source-repo https://github.com/{owner}/{repo} --target-repo https://github.com/dotnet/sdk
darc get-subscriptions --exact --source-repo https://github.com/{owner}/{repo} --target-repo https://github.com/dotnet/runtime
```

Build the current SDK-flow matrix as follows:
1. For each enabled forward subscription from the source repository to `dotnet/dotnet`, record its source channel and VMR target branch.
2. Map the target branch to its SDK name using the VMR default channels (for example, VMR `main` -> `.NET 12.0.1xx SDK`, `release/11.0.1xx` -> `.NET 11.0.1xx SDK`).
3. Associate the subscription's source channel with the source branches that publish to that channel.
4. Preserve every distinct SDK destination. A branch may legitimately feed multiple SDKs; do not collapse the list to one channel or assume the newest SDK replaces the previous one.

Then propose the after-snap flow matrix from the actual servicing requirements:
- Preserve existing `main` SDK destinations unless the snap explicitly retires one.
- Add the snapped content's required SDK destination to `release/insiders`.
- If old stable content must continue feeding an SDK band, plan a new `release/<sdk-band>` branch from the pre-snap stable commit and transfer that SDK flow to it.
- Identify the previous SDK servicing branch and flow as a retirement candidate.

Present both matrices for confirmation. Do not make subscription or default-channel changes yet.

#### 1.4 Read Visual Studio schedules and draft the pre-snap announcement

The normal cadence is:
1. Send the announcement on Friday.
2. Perform the Roslyn snap the following Monday, normally near end of day PT.
3. Visual Studio snaps `main` → `rel/insiders` that Friday at the time listed in the schedule.

Infer these dates from the snapped version's VS schedule instead of asking the user to supply all of them:
- Start with the scheduled VS `main` → `rel/insiders` snap.
- Recommend the preceding Monday as the Roslyn snap date.
- Recommend the Friday before that Monday as the announcement date.

Present the inferred cadence and ask the user to confirm the Roslyn snap date/time. Treat this as a default, not a fixed rule: holidays, schedule exceptions, or explicit team plans may require different dates.

Two VS versions are involved, and their schedules have different purposes:
- **Snapped version** (the current `main` version): supplies the upcoming VS `main` → `rel/insiders` snap date and the `release/insiders` QB window. For a snap for 18.11, use the Dev18.11 schedule.
- **After-snap main version** (current version + 1 minor): supplies the start of the new `main` feature-development cycle. For a snap for 18.11, use Dev18.12 only for this context; do **not** use its later QB dates in the 18.11 announcement.

Use an MCP server to find the schedule:
1. Search the `DevDiv.wiki` repository in organization/project `devdiv/DevDiv` for `Dev{version} Schedule` with `project_search` method `wiki`.
2. Read the returned page path with `code_read` method `content`.
3. For the snapped version, extract:
   - `Snap main to rel/insiders` date, start time, and notes.
   - The first `QB Mode` row whose branch is `rel/insiders`, including start, end, and submission deadline.
   - Its final build, sign-off, and ship dates for context.
4. For the after-snap main version, extract the first feature-development start on `main`.

Do not silently substitute one version's schedule for the other. If the schedule is missing, ambiguous, places the VS snap on an unexpected day, or conflicts with the proposed Roslyn snap date, stop and ask the user to confirm the cadence. The Roslyn snap must precede the VS `main` → `rel/insiders` snap; the user-confirmed Roslyn date/time defines the content cutoff.

Using the info from steps 1.1–1.3 and the validated schedules, draft the email.

The email should follow this format:

> **Subject:** Snap for {VS version} on {day of week}, {date}
>
> Hi all,
>
> We will **snap for {VS version} on {day of week}, {date} ~ {time} PT.** main will point to {new VS version} after the snap.
>
> Following is important information about branches and dates.
>
> | Branch | Current VS | Current SDK | After-snap VS | After-snap SDK | Notes |
> |---|---|---|---|---|---|

Fill in each row using the VS versions and insertion settings discovered in step 1.2 and the SDK-flow matrices discovered in step 1.3. If a branch feeds multiple SDKs, list all of them in both the current and after-snap columns; do not imply that adding a new SDK destination retires an existing one. Show the intended after-snap state without exposing pending InfraSwat configuration work.

> QB mode for `release/insiders` is from {QB start} to {QB end} ({deadline}).
>
> If there's anything that needs to be checked in for QB mode, please contact the InfraSwat team.

**Adapt the table** to the actual branch structure:
- Include all active named branches, the new SDK servicing branch (if any), and the servicing branch planned for retirement.
- Use `—` for cells with no applicable VS or SDK destination.
- Keep notes action-oriented and concise, for example:
  - `main`: "Insertions will be drafts until VS snaps"
  - `release/insiders`: "Will receive the current main content"
  - `release/stable`: "Will receive the current release/insiders content"
  - new servicing branch: "Will receive the current release/stable content"
  - retiring servicing branch: "Will be retired after the snap"
- Fill current SDK columns from existing forward subscriptions and VMR target channels, not only source-repository default channels.
- Fill after-snap SDK columns from the confirmed planned flow matrix.
- Derive the QB sentence from the snapped version's first `rel/insiders` QB row. Preserve the schedule's full weekday/date wording and deadline.
- Include the snapped version's schedule link when available.

Present the draft to the user for review and editing before they send it.

#### 1.5 Find recent PRs and milestones

- List last merged PRs to source branch:
  ```
  gh pr list --repo {owner}/{repo} --search "is:merged base:{sourceBranch} sort:updated-desc" --json number,title,mergedAt,mergeCommit --limit 5
  ```
- List PRs in the `Next` milestone:
  ```
  gh pr list --repo {owner}/{repo} --search "is:merged milestone:Next base:{sourceBranch} sort:updated-desc" --json number,title,mergedAt,mergeCommit
  ```
- List closed issues in the `Next` milestone:
  ```
  gh issue list --repo {owner}/{repo} --search "is:closed milestone:Next" --json number,title
  ```
- List all milestones:
  ```
  gh api repos/{owner}/{repo}/milestones --paginate --jq ".[] | {number:.number,title:.title}"
  ```

#### 1.6 Determine snap point

- Use the user-confirmed Roslyn snap date/time from step 1.4 as the cutoff. Do not use the later VS snap time as the cutoff.
- If the cutoff is in the future, record that snap-point selection is deferred and repeat this step immediately before execution.
- List enough recently merged PRs to cover the cutoff, parse `mergedAt`, and select the latest PR merged to `main` at or before the cutoff. For example:
  ```powershell
  $cutoff = [DateTimeOffset]::Parse("{confirmed cutoff with UTC offset}")
  $prs = gh pr list --repo {owner}/{repo} --search "is:merged base:main sort:updated-desc" --json number,title,mergedAt,mergeCommit --limit 100 | ConvertFrom-Json
  $candidate = $prs |
      Where-Object { [DateTimeOffset]::Parse($_.mergedAt) -le $cutoff } |
      Sort-Object { [DateTimeOffset]::Parse($_.mergedAt) } -Descending |
      Select-Object -First 1
  if ($null -eq $candidate) { throw "No merged PR found at or before the confirmed snap cutoff" }
  $candidate
  ```
- Present the candidate PR number, title, merge time, and merge commit SHA as the recommended snap point. Ask the user to confirm it; do not choose it silently.
- After confirmation, verify the commit exists:
  ```
  gh api repos/{owner}/{repo}/commits/{mergeCommitOid}
  ```
- Verify reachability after fetching the upstream remote in step 3.1.
- If the user chooses a different PR, use that PR's merge commit and record the explicit override.

### Phase 2: Present the Plan

After gathering, present **all** planned actions in a numbered list for the user to review. The plan typically includes:

1. **Merge `release/insiders` → `release/stable`**: Open a draft snap PR to bring insiders' content (e.g., 18.5) into stable. Construct a custom `PublishData.json` with `insertionTitlePrefix` = `[Stable]`. Ask the user whether `vsBranch` should temporarily be `rel/insiders` (interim redirect before VS snaps) or `rel/stable` (no redirect). Default is typically **no redirect**.

2. **Merge `main` → `release/insiders`**: Open a draft snap PR to bring main's content (e.g., 18.6) into insiders, up to the chosen snap commit. Construct a custom `PublishData.json` with `insertionTitlePrefix` = `[Insiders]` and `vsBranch` = `main` (temporary — VS hasn't snapped yet, so `rel/insiders` still points to the old version).

3. **Update `PublishData.json` on `main`**: Set `insertionCreateDraftPR` to `true`. Until the snapped version's scheduled VS `main` → `rel/insiders` snap, main's insertions should be drafts to avoid merging into the wrong VS branch. This change goes in the same PR as the version bump.

4. **Update `Versions.props` on `main`**: Bump the minor version (e.g., 5.6.0 → 5.7.0) and reset `PreReleaseVersionLabel` to `1`.

5. **Update Razor versions in `eng/Versions.props`** (roslyn only) -- Razor has two independent versions, both stored as `Razor`-prefixed properties in `eng/Versions.props`:

   **5a. Razor VSIX/Addin version**: bump `<RazorVsixVersionPrefix>` and `<RazorAddinMajorVersion>` minor to track the new VS version (e.g., `18.7.1` -> `18.8.1`, `18.7` -> `18.8`). Always done as part of the snap.

   **5b. Razor SDK version**: only update if the branch's SDK default-channel doesn't match the current `<RazorMajorVersion>.<RazorMinorVersion>`. Use the discovery from Phase 1 / Step B.1:
     - If `main` flows to `.NET <Major>.0.<Band>xx SDK` and `<Major>.<Band>` differs from the current `<RazorMajorVersion>.<RazorMinorVersion>` in `eng/Versions.props`, set `<RazorMajorVersion>` and `<RazorMinorVersion>` to match (e.g., `.NET 10.0.4xx SDK` -> `<RazorMajorVersion>10</RazorMajorVersion>`, `<RazorMinorVersion>4</RazorMinorVersion>`). Leave `<RazorPatchVersion>` as `0`.
     - If no SDK channel matching Razor's current major exists on `main` yet (the next SDK band channel hasn't been created), **skip 5b** and add it to the post-VS-snap follow-up (step 3.9). Do **not** predict the next band -- only update when darc confirms the channel.

6. **Update SARIF files** (roslyn only): Replace old version string with new version in all `.sarif` files under `src/RoslynAnalyzers/` (search recursively).

7. **Darc channel changes**: Update default channels to reflect the new version each branch carries:
   - `release/insiders` → VS channel for the snapped version (e.g., `VS 18.6`)
   - `release/stable` → VS channel for what was previously insiders (e.g., `VS 18.5`)
   - Preserve every confirmed SDK destination that remains supported; adding an SDK destination does not implicitly remove another.
   - Add the snapped content's required SDK source channel and subscription/backflow changes to `release/insiders`.
   - Transfer the old stable SDK flow to the new `release/<sdk-band>` servicing branch when one is being created.
   - Remove the previous servicing branch's default channel and subscriptions when that flow is being retired.
   - Add a future SDK channel only when it exists and the user confirms the new flow.

8. **Update the InfraSwat dashboard manually**: After the Maestro configuration PR merges, update the Roslyn build widgets on the [dnceng Roslyn/Razor InfraSwat dashboard](https://dev.azure.com/dnceng/internal/_dashboards/dashboard/7cd4c2dc-8e75-4cb6-9936-e937c0e496c4) so their displayed VS/SDK versions and configured branches match the post-snap state.

9. **Move milestones**: Assign the target milestone (e.g., `18.6`) to the PRs included since the previous snap. Create the milestone if it doesn't exist.

10. **Preserve or retire old stable**: If old stable content still serves an SDK band, create `release/<sdk-band>` (for example, `release/10.0.4xx`) directly from the pre-snap `release/stable` commit **before** stable is overwritten. Transfer the SDK flow to that branch. If the previous servicing branch (for example, `release/10.0.3xx`) is no longer needed, retire its default channel and subscriptions. If old stable is fully retired, skip branch creation and remove its obsolete flows.

**`PublishData.json` interim handling**: During the schedule-defined gap between the Roslyn snap and the snapped version's VS `main` → `rel/insiders` snap, named branches need temporary insertion target overrides because VS branch names haven't shifted yet. These temporary changes are included directly in the snap merge PRs (for non-main branches) and reverted after VS snaps (see step 3.9).

Present the plan clearly and ask: **"Shall I proceed with these changes?"**

### Phase 3: Execute (after confirmation)

Only proceed after explicit user confirmation. Execute changes in this order:

#### Merge strategy: "take source" (conflict-free)

Snap merges use a **"take source"** strategy (inspired by the VS repo's `Merge-ToMoreStableBranch`). Instead of letting Git three-way merge source and target (which conflicts on config files), we create a merge commit whose **tree is taken entirely from the source branch** using `git commit-tree` plumbing. The commit has two parents (target branch tip + source commit) so Git records it as a proper merge, but the content comes exclusively from the source — no merge conflicts are possible.

Files that must differ per named branch (specifically `eng/config/PublishData.json`, which controls VS insertion target) are **replaced with a custom version** constructed from the source's package list and the correct `branchInfo` values for the target branch. This mirrors how `snap.cs` pushes the correct `PublishData.json` to the snap branch.

This approach does NOT touch the user's working tree — all operations use git plumbing (temp index, `commit-tree`, `write-tree`) which do not read or modify the working tree or the repo's main index.

#### 3.1 Set up remotes and fetch

Ask the user for the **path to their local clone** of the repo (e.g., `D:\roslyn`). Default to the current workspace folder if it matches the repo being snapped.

If the snap point was deferred because the cutoff was in the future, run step 1.6 now and obtain explicit confirmation of the recommended PR and merge commit before creating any branches.

**Find the fork remote name** — the user likely already has a remote for their fork. List remotes (`git remote -v`) and find the one pointing to `{forkOwner}/{repo}`. Use that name as `{forkRemote}` throughout. Do **not** create a new remote if one already exists.

Also identify the upstream remote (typically `origin` or `dotnet`) — the one pointing to `{owner}/{repo}`. Use that as `{upstreamRemote}`.

```bash
cd {repoPath}
git fetch {upstreamRemote} release/insiders release/stable main
git merge-base --is-ancestor {snapCommitSha} {upstreamRemote}/main
```

If the reachability check fails, stop; do not create any branches from the unverified commit.

Before changing `release/insiders`, record the previous snap's source boundary for milestone assignment:
```powershell
$previousSnapMergeSha = git log {upstreamRemote}/release/insiders --format=%H --grep="^Merge main into release/insiders$" -n 1
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($previousSnapMergeSha)) {
    throw "Could not find the previous main-to-insiders snap merge"
}

$parents = (git show -s --format=%P $previousSnapMergeSha).Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)
if ($LASTEXITCODE -ne 0 -or $parents.Count -ne 2) {
    throw "Previous snap merge does not have exactly two parents"
}

$previousSnapCommitSha = $parents[1]
```

Persist `$previousSnapCommitSha` with the current `{snapCommitSha}` in the session state.

#### 3.2 Preserve old stable in an SDK servicing branch (if applicable)

This step must happen **before** the insiders → stable merge changes `release/stable`. Reconfirm the exact servicing branch name and pre-snap stable SHA from the approved plan. SDK servicing branches use `release/<sdk-band>` (for example, `release/10.0.4xx`).

The servicing branch is a direct branch in the upstream repository, not a fork branch or a PR. Its initial tree and history must exactly match the pre-snap `release/stable` tip; do not rewrite `PublishData.json` or create a synthetic commit.

```powershell
$oldStableSha = git rev-parse "{upstreamRemote}/release/stable"
if ($LASTEXITCODE -ne 0) { throw "Could not resolve pre-snap release/stable" }

$existingRefs = gh api "repos/{owner}/{repo}/git/matching-refs/heads/release/{sdkBand}" | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Could not check servicing branch" }
if ($existingRefs.Count -ne 0) { throw "release/{sdkBand} already exists; stop and verify its commit" }

gh api -X POST "repos/{owner}/{repo}/git/refs" `
  --field ref="refs/heads/release/{sdkBand}" `
  --field sha="$oldStableSha"
if ($LASTEXITCODE -ne 0) { throw "Could not create release/{sdkBand}" }

$createdSha = gh api "repos/{owner}/{repo}/git/ref/heads/release/{sdkBand}" --jq ".object.sha"
if ($LASTEXITCODE -ne 0 -or $createdSha -ne $oldStableSha) {
    throw "release/{sdkBand} was not created at the expected pre-snap stable SHA"
}
```

Record the branch name and SHA in the session state. If old stable is being retired completely, explicitly record that this step was skipped.

The new branch is SDK-only, but it inherits the old stable branch's automatic VS insertion stage. Open a branch-specific PR that changes the official pipeline's `Insert to VS` stage from a dependency on `build` to a manual trigger:
```yaml
    - stage: insert
      trigger: manual
      displayName: Insert to VS
```

This keeps official build and BAR publishing automatic while preventing the older preserved VS packages from being inserted into a newer `rel/stable` branch. Use the prior SDK servicing branch's equivalent change as the pattern (for example, commit `96105ea4e14f7330f111835111edcaefa03b8c88` for `release/10.0.3xx`).

#### 3.3 Merge insiders → stable

```bash
# Resolve refs
SOURCE_COMMIT=$(git rev-parse {upstreamRemote}/release/insiders)
SOURCE_TREE=$(git rev-parse "{upstreamRemote}/release/insiders^{tree}")
TARGET_COMMIT=$(git rev-parse {upstreamRemote}/release/stable)

# Commit 1: Pure merge (take-source strategy)
MERGE_COMMIT=$(git commit-tree "$SOURCE_TREE" \
  -p "$TARGET_COMMIT" -p "$SOURCE_COMMIT" \
  -m "Merge release/insiders into release/stable")

# Commit 2: Update PublishData.json for the stable branch.
# Use source's PD as base (it has the up-to-date package list),
# then set the correct branchInfo values for release/stable.
#   - vsBranch: ask user (default: rel/stable; or rel/insiders for interim redirect)
#   - insertionTitlePrefix: [Stable]
#   - insertionCreateDraftPR: false
# See "Constructing custom PublishData.json" below for the JSON manipulation approach.
NEW_PD_BLOB=$(...)  # git hash-object -w of the modified JSON
TEMP_INDEX=$(mktemp)
GIT_INDEX_FILE=$TEMP_INDEX git read-tree $SOURCE_TREE
GIT_INDEX_FILE=$TEMP_INDEX git update-index --add --cacheinfo 100644,$NEW_PD_BLOB,eng/config/PublishData.json
CONFIG_TREE=$(GIT_INDEX_FILE=$TEMP_INDEX git write-tree)
rm -f "$TEMP_INDEX"
CONFIG_COMMIT=$(git commit-tree "$CONFIG_TREE" \
  -p "$MERGE_COMMIT" \
  -m "Update PublishData.json for release/stable")

# Create branch, push to fork, open draft PR
git branch snap-insiders-to-stable "$CONFIG_COMMIT"
git push {forkRemote} snap-insiders-to-stable

gh pr create --repo {owner}/{repo} \
  --title "Snap release/insiders into release/stable" \
  --body "Auto-generated by snap skill. Snap merge (take-source strategy)." \
  --head {forkOwner}:snap-insiders-to-stable --base release/stable --draft
```

#### 3.4 Merge main → insiders

Same "take source" approach, but use the chosen **snap commit** (not branch HEAD) as the source:

```bash
SOURCE_COMMIT={snapCommitSha}
SOURCE_TREE=$(git rev-parse "{snapCommitSha}^{tree}")
TARGET_COMMIT=$(git rev-parse {upstreamRemote}/release/insiders)

# Commit 1: Pure merge (take-source strategy)
MERGE_COMMIT=$(git commit-tree "$SOURCE_TREE" \
  -p "$TARGET_COMMIT" -p "$SOURCE_COMMIT" \
  -m "Merge main into release/insiders")

# Commit 2: Update PublishData.json for the insiders branch.
# Use source's PD as base (from the snap commit on main),
# then set the correct branchInfo values for release/insiders:
#   - vsBranch: main (temporary — VS hasn't snapped yet, so rel/insiders still points to the old version)
#   - insertionTitlePrefix: [Insiders]
#   - insertionCreateDraftPR: false
NEW_PD_BLOB=$(...)  # git hash-object -w of the modified JSON
TEMP_INDEX=$(mktemp)
GIT_INDEX_FILE=$TEMP_INDEX git read-tree $SOURCE_TREE
GIT_INDEX_FILE=$TEMP_INDEX git update-index --add --cacheinfo 100644,$NEW_PD_BLOB,eng/config/PublishData.json
CONFIG_TREE=$(GIT_INDEX_FILE=$TEMP_INDEX git write-tree)
rm -f "$TEMP_INDEX"
CONFIG_COMMIT=$(git commit-tree "$CONFIG_TREE" \
  -p "$MERGE_COMMIT" \
  -m "Update PublishData.json for release/insiders")

git branch snap-main-to-insiders "$CONFIG_COMMIT"
git push {forkRemote} snap-main-to-insiders

gh pr create --repo {owner}/{repo} \
  --title "Snap main into release/insiders" \
  --body "Auto-generated by snap skill. Snap merge (take-source strategy)." \
  --head {forkOwner}:snap-main-to-insiders --base release/insiders --draft
```

> **PowerShell note**: On Windows, use `$env:GIT_INDEX_FILE` for environment variables and `[System.IO.Path]::GetTempFileName()` for temp files. Set `$env:GIT_INDEX_FILE` before each git command and restore it afterward.

#### Constructing custom PublishData.json

For each merge PR, construct a `PublishData.json` from the source's content (up-to-date package list) with the correct `branchInfo` / `branches` values for the target branch:

1. Read source's PD content: `git show {sourceCommit}:eng/config/PublishData.json`
2. Parse and modify the JSON:
   - **Roslyn** (uses `branchInfo` key): Replace `vsBranch`, `insertionTitlePrefix`, `insertionCreateDraftPR` values.
3. Write modified JSON to a git blob: `echo "{modifiedJson}" | git hash-object -w --stdin`
4. Override in temp index: `GIT_INDEX_FILE=$TEMP_INDEX git update-index --add --cacheinfo 100644,{newBlobSha},eng/config/PublishData.json`

**Target values for each merge:**

| Merge | `vsBranch` (interim) | `vsBranch` (final) | `insertionTitlePrefix` |
|-------|---------------------|--------------------|----------------------|
| main → insiders | `main` | `rel/insiders` | `[Insiders]` |
| insiders → stable | `rel/insiders` (if redirecting) or `rel/stable` (if not) | `rel/stable` | `[Stable]` |

Set `insertionCreateDraftPR` to `false` for both. Ask the user whether `release/stable` needs temporary redirection — default is typically **no** (unlike insiders, which always needs it).

After both merge PRs are opened, clean up the local branches:

```bash
git branch -D snap-insiders-to-stable snap-main-to-insiders
```

#### 3.5 Update configuration files on main

For config file changes to the source branch (e.g., `Versions.props` bump, SARIF updates, `PublishData.json` draft flag), create a branch in the fork and update files via the GitHub API:

```bash
# Create branch in fork from main HEAD
MAIN_SHA=$(gh api repos/{owner}/{repo}/git/refs/heads/main --jq '.object.sha')
gh api -X POST repos/{forkOwner}/{repo}/git/refs \
  --field ref=refs/heads/{updateBranch} --field sha=$MAIN_SHA

# Get current file SHA and content, modify, then update
FILE_SHA=$(gh api -X GET "repos/{forkOwner}/{repo}/contents/{filePath}" \
  --field ref={updateBranch} --jq '.sha')
gh api -X PUT "repos/{forkOwner}/{repo}/contents/{filePath}" \
  --field message="Update {fileName}" \
  --field branch={updateBranch} \
  --field sha=$FILE_SHA \
  --field content={base64Content}

# Open draft PR for config changes
gh pr create --repo {owner}/{repo} \
  --title "Post-snap configuration updates" \
  --body "Auto-generated by snap skill. Version bump and config updates after snap." \
  --head {forkOwner}:{updateBranch} --base main --draft
```

> **PR convention**: When this skill opens a PR, include a short note like `Auto-generated by snap skill.` in the body/description so reviewers know it was mechanically produced. Keep follow-up/configuration PRs as **drafts** unless the user asks otherwise.

> **Large files (e.g., SARIF)**: When the base64 content is too long for a command-line argument, write a JSON body to a temp file and use `gh api --input {tempFile}` instead of inline `--field content=...`.

**SARIF files**: Located in subdirectories under `src/RoslynAnalyzers/`, e.g.:
- `src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/Microsoft.CodeAnalysis.Analyzers.sarif`
- `src/RoslynAnalyzers/Roslyn.Diagnostics.Analyzers/Roslyn.Diagnostics.Analyzers.sarif`
- etc.

Find them with `git ls-files 'src/RoslynAnalyzers/**/*.sarif'` or search via the GitHub API. Replace `"version": "{oldVersion}"` with `"version": "{newVersion}"` (e.g., `"5.6.0"` → `"5.7.0"`).

These SARIF files are generated and intentionally have no final newline. Perform a byte-preserving replacement: keep the existing encoding, BOM, line endings, and EOF state exactly. Do not deserialize/reserialize the JSON or append a newline. The correctness build regenerates these files and fails if any non-version bytes differ.

**Razor versions in `eng/Versions.props`**: Two independent sets of edits in the Razor PropertyGroups. Read the current file, perform both replacements in memory, then PUT the new content back via the GitHub API.

- **5a. VSIX/Addin version** -- bump the VS version (always done):
  - `<RazorVsixVersionPrefix>{oldMajor.Minor}.1</RazorVsixVersionPrefix>` -> `<RazorVsixVersionPrefix>{newMajor.Minor}.1</RazorVsixVersionPrefix>` (e.g., `18.7.1` -> `18.8.1`)
  - `<RazorAddinMajorVersion>{oldMajor.Minor}</RazorAddinMajorVersion>` -> `<RazorAddinMajorVersion>{newMajor.Minor}</RazorAddinMajorVersion>` (e.g., `18.7` -> `18.8`)
- **5b. SDK RazorMajorVersion/RazorMinorVersion** -- only if the matching SDK channel exists on `main` and differs from the current value (see Phase 1 / Step B.1):
  - `<RazorMajorVersion>{old}</RazorMajorVersion>` -> `<RazorMajorVersion>{newSdkMajor}</RazorMajorVersion>` (only changes when crossing .NET majors, e.g., 10 -> 11)
  - `<RazorMinorVersion>{old}</RazorMinorVersion>` -> `<RazorMinorVersion>{newSdkBand}</RazorMinorVersion>` (e.g., `0` -> `4` when `main` flows to `.NET 10.0.4xx SDK`)
  - Leave `<RazorPatchVersion>0</RazorPatchVersion>` and `<RazorPreReleaseVersionLabel>` untouched.
  - If no matching SDK channel exists on `main` yet (the next SDK band hasn't been created), **omit 5b from this PR** and add it to the post-VS-snap follow-up (Step 3.9).

#### 3.6 Update darc default channels and subscriptions

All channel updates across all repos should be collected into a **single PR** in the `maestro-configuration` repository. Use `--configuration-branch` to target a shared branch and `--no-pr` to avoid creating separate PRs for each command. Then create one PR at the end.

Pick a branch name (e.g., `snap/{repo1}-{repo2}-{newVsVersion}`). For each repo, update both VS channels and SDK channels:

**VS channels**: delete the old channel mapping, then add the new one:

```
# First command creates the branch; all subsequent commands reuse it.
# Use --ci to avoid interactive prompts.

# Delete old mapping (find ID via darc get-default-channels first)
darc delete-default-channel --id {id} --configuration-branch {cfgBranch} --no-pr --ci

# Add new mapping
darc add-default-channel --repo https://github.com/{owner}/{repo} --branch {branch} --channel "{channelName}" --configuration-branch {cfgBranch} --no-pr --ci
```

Repeat for every repo and branch being snapped (e.g., insiders and stable for roslyn).

**SDK channels and flows**: Apply the confirmed after-snap matrix from step 1.3. Do not blindly move a single SDK channel from `main` to `release/insiders`; a source branch may continue feeding multiple SDK destinations while insiders gains one of them.

For each matrix row:
- Keep existing default channels and subscriptions that remain required.
- Add the required source channel to a branch that gains an SDK flow.
- Create or update its forward subscription to the correct VMR target branch and the corresponding backflow.
- For an SDK servicing rollover, add the old stable SDK channel and flow to `release/<sdk-band>`, then remove them from `release/stable`.
- Delete the prior servicing branch's default channel and subscriptions only when the approved plan marks that SDK flow retired.

Example default-channel operations:
```
# Add a retained SDK band to insiders
darc add-default-channel --repo https://github.com/{owner}/{repo} --branch release/insiders --channel ".NET 11.0.1xx SDK" --configuration-branch {cfgBranch} --no-pr --ci

# Move old stable's SDK band to its new servicing branch
darc delete-default-channel --id {stableSdkDefaultChannelId} --configuration-branch {cfgBranch} --no-pr --ci
darc add-default-channel --repo https://github.com/{owner}/{repo} --branch release/{sdkBand} --channel ".NET {sdkBand} SDK" --configuration-branch {cfgBranch} --no-pr --ci

# Retire the previous servicing branch's SDK channel
darc delete-default-channel --id {retiredServicingDefaultChannelId} --configuration-branch {cfgBranch} --no-pr --ci
```

Before adding a default channel, check whether the exact branch/channel mapping already exists. If so, report it as already up to date instead of adding a duplicate. A future SDK channel is a follow-up only when it does not exist yet; never predict or replace a confirmed current flow with it.

If a Darc command fails after earlier commands have committed partial changes to the configuration branch, do not try to reverse them by re-adding production associations: Darc may reject the add because it validates equivalence against production. Reset only the isolated configuration branch to current `production`, verify the SHA, and reapply the approved operations from the beginning.

Subscription changes use the same config repo and must be batched onto the same branch. When creating a forward-flow subscription (repo → dotnet/dotnet), also create the corresponding **backflow** subscription (dotnet/dotnet → repo).

**Preferred approach — clone from an existing subscription**: Use `--subscription <GUID>` to copy all settings (excluded assets, merge policies, source-directory, source-enabled, etc.) from an existing subscription for the same repo, then override only what differs. Always use `-q` (quiet mode) to avoid the interactive editor that `darc add-subscription` opens by default:
```
# Find an existing subscription to use as template
darc get-subscriptions --exact --source-repo https://github.com/dotnet/dotnet --target-repo https://github.com/{owner}/{repo}

# Create new subscription by cloning the template, overriding channel and target branch
darc add-subscription -q --subscription {templateSubscriptionGuid} --target-branch {branch} --channel "{channelName}" --configuration-branch {cfgBranch} --no-pr
```

**Manual approach** (when no template exists):
```
# Forward flow: repo → VMR
darc add-subscription -q --source-repo https://github.com/{owner}/{repo} --target-repo https://github.com/dotnet/dotnet --target-branch {vmrBranch} --channel "{channelName}" --update-frequency EveryDay --source-enabled --target-directory {repoName} --standard-automerge --configuration-branch {cfgBranch} --no-pr

# Backflow: VMR → repo
darc add-subscription -q --source-repo https://github.com/dotnet/dotnet --target-repo https://github.com/{owner}/{repo} --target-branch {branch} --channel "{vmrChannelName}" --update-frequency EveryDay --source-enabled --source-directory {repoName} --standard-automerge --configuration-branch {cfgBranch} --no-pr

# Update existing subscription
darc update-subscription --id {subscriptionId} --channel "{newChannel}" --configuration-branch {cfgBranch} --no-pr --ci
```

> **Note**: `-q` is critical — without it, `darc add-subscription` opens an interactive YAML editor even when all flags are provided. The `--subscription` clone approach is preferred because backflow subscriptions have complex excluded-assets lists that are tedious to specify manually.

> **Stale flow PR cleanup**: When you change a subscription or move it to a new channel/branch, existing open `dotnet-maestro[bot]` flow PRs created from the **old** subscription do not automatically disappear. Do **not** merge those stale PRs. After the configuration PR merges (or earlier if you spot them), search the affected repos/branches and close any outdated flow PRs so Maestro recreates them from the new configuration:
```
# Backflow PRs in the product repo
gh pr list --repo {owner}/{repo} --search "is:open author:dotnet-maestro[bot] base:{branch}" --json number,title,url,createdAt

# Forward-flow PRs in the VMR
gh pr list --repo dotnet/dotnet --search "is:open author:dotnet-maestro[bot] base:{vmrBranch}" --json number,title,url,createdAt

# Close a stale PR after confirming it came from the old subscription/channel mapping
gh pr close {number} --repo {repoToCloseIn} --comment "Closing stale flow PR after subscription/channel update; Maestro will recreate it from the new configuration. This action was performed automatically by the snap skill."
```

After all commands, inspect the branch diff against `production` and verify only the expected configuration files and associations changed. Create one non-draft PR with auto-complete and squash enabled, then print the URL. Use a single-line description when invoking `az repos pr create`; multiline native-command arguments may be truncated by some Azure CLI/PowerShell combinations.
```
$description = "Updates default channels for the {newVsVersion} snap. Auto-generated by snap skill."
az repos pr create --repository maestro-configuration --org https://dev.azure.com/dnceng --project internal --source-branch {cfgBranch} --target-branch production --title "Snap: update default channels for {repos} ({newVsVersion})" --description $description --draft false --auto-complete true --squash true -o none

$prs = az repos pr list --repository maestro-configuration --org https://dev.azure.com/dnceng --project internal --source-branch {cfgBranch} --status active -o json | ConvertFrom-Json
$matches = @($prs | Where-Object { $_.targetRefName -eq "refs/heads/production" })
if ($matches.Count -ne 1) {
    throw "Expected one active Maestro configuration PR, found $($matches.Count)"
}
$prId = $matches[0].pullRequestId

# Verify state. Some Azure CLI versions may ignore PR creation options.
$pr = az repos pr show --id $prId --org https://dev.azure.com/dnceng -o json | ConvertFrom-Json
if ($pr.isDraft) {
    az repos pr update --id $prId --org https://dev.azure.com/dnceng --draft false
    $pr = az repos pr show --id $prId --org https://dev.azure.com/dnceng -o json | ConvertFrom-Json
}
if ($null -eq $pr.autoCompleteSetBy) {
    az repos pr update --id $prId --org https://dev.azure.com/dnceng --auto-complete true --squash true
    $pr = az repos pr show --id $prId --org https://dev.azure.com/dnceng -o json | ConvertFrom-Json
}
if ($pr.description -ne $description) {
    az repos pr update --id $prId --org https://dev.azure.com/dnceng --description $description
}
if ($pr.isDraft -or $null -eq $pr.autoCompleteSetBy) {
    throw "Maestro PR state verification failed"
}

Write-Output "PR: https://dev.azure.com/dnceng/internal/_git/maestro-configuration/pullrequest/$prId"
```

After the Maestro configuration PR merges, manually update the [dnceng Roslyn/Razor InfraSwat dashboard](https://dev.azure.com/dnceng/internal/_dashboards/dashboard/7cd4c2dc-8e75-4cb6-9936-e937c0e496c4). Treat this as a required snap checkpoint. Dashboard widget updates are intentionally manual: the API requires replacing full widget payloads and preserving eTags, layout, and settings, which makes unattended edits unnecessarily risky.

Update the active Roslyn widgets to match the verified post-snap state:
- `Roslyn main`: update the displayed VS version; preserve its SDK label unless the confirmed SDK flow changed.
- `Roslyn insiders`: update the displayed VS version.
- `Roslyn stable`: update the displayed VS version.
- SDK servicing widget: update both its name and configured `fullBranchName` from the old stable branch to the new `release/<sdk-band>` branch.
- Remove or repurpose any widget for a retired servicing branch.

Verify each widget still points to the intended build definition and branch after saving. For example, after an 18.11 snap with a 10.0.4xx servicing branch, the expected Roslyn widget state is:
- `Roslyn main -> main/18.12/11.0.1xx`
- `Roslyn insiders -> insiders/18.11`
- `Roslyn stable -> stable/18.10`
- `Roslyn 10.0.4xx -> 10.0.4xx (2026/08/11)`, with `fullBranchName` = `refs/heads/release/10.0.4xx` (preserve or update the dashboard's date suffix as appropriate)

#### 3.7 Move milestones

Create the target milestone if needed (milestone name is just the version number, e.g., `18.6`):
```
gh api -X POST repos/{owner}/{repo}/milestones --field title="{milestoneName}"
```

Do not rely on the `Next` milestone to identify included PRs; contributors and automation may not assign it consistently. Use the exact Git ancestry range from the previous snap source commit (exclusive) through the current snap commit (inclusive), then match those merge commits to merged PRs:
```powershell
$rangeCommits = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
git rev-list --ancestry-path "{previousSnapCommitSha}..{snapCommitSha}" |
    ForEach-Object { [void]$rangeCommits.Add($_.Trim()) }
if ($LASTEXITCODE -ne 0) {
    throw "Computing snap ancestry failed"
}

$mergedPrs = gh pr list --repo {owner}/{repo} --search "is:merged base:main" --json number,title,mergeCommit,milestone --limit 1000 | ConvertFrom-Json
$prs = @($mergedPrs | Where-Object {
    $null -ne $_.mergeCommit -and $rangeCommits.Contains($_.mergeCommit.oid)
})

if ($prs.Count -eq 0) {
    throw "No merged PRs found in the confirmed snap range"
}
```

Get the milestone number for the target milestone:
```
$msNumber = gh api repos/{owner}/{repo}/milestones --jq '.[] | select(.title == "{milestoneName}") | .number'
```

Review the count and boundary PRs before proceeding. Stop if any included PR already has a different milestone. Assign the milestone using the REST API:
```powershell
$conflicts = @($prs | Where-Object {
    $null -ne $_.milestone -and $_.milestone.number -ne [int]$msNumber
})
if ($conflicts.Count -ne 0) {
    throw "Some included PRs already have another milestone"
}

foreach ($pr in $prs) {
    gh api repos/{owner}/{repo}/issues/$($pr.number) -X PATCH -F milestone=$msNumber --silent
    if ($LASTEXITCODE -ne 0) {
        throw "Updating milestone for PR #$($pr.number) failed"
    }
}
```

Verify each assignment through the issue REST endpoint rather than GitHub search, whose milestone index can lag. Do not move unrelated stale items from `Next`. Handle closed issues only when the user provides or confirms an explicit issue set.

#### 3.8 Reply to the snap announcement email

After all snap steps are completed, draft a reply to the pre-snap announcement email (from step 1.4) confirming the snap is done. Don't include links to created PRs. Summarize what each branch now targets using the verified post-change VS and SDK-flow matrices. Mention any pending follow-ups (e.g., SDK channel not yet created).

Present the draft to the user before they send it.

#### 3.9 Post-VS-snap config updates

After the snapped version's scheduled VS `main` → `rel/insiders` snap, the interim `PublishData.json` overrides need reverting. Verify that the scheduled VS snap occurred before proceeding. For each repo, create a **draft PR** to update:

- **`main`**: Set `insertionCreateDraftPR` back to `false` so insertions are no longer drafts.
- **`release/insiders`**: Change `vsBranch` from `main` to `rel/insiders` (VS has now created the `rel/insiders` branch).
- **`release/stable`** (only if temporarily redirected during snap): Change `vsBranch` to `rel/stable`.

In the PR body/description, include a brief note such as `Auto-generated by snap skill. Post-VS-snap PublishData.json follow-up.` so reviewers know the cleanup was generated by this workflow.

Also handle any pending SDK channel follow-ups (e.g., adding `main` to a newly created `.NET 10.0.Nxx SDK` channel).

**Deferred Razor SDK version bump** (roslyn only): If the Razor SDK version bump (Phase 2 step 5b) was deferred during the initial snap because no matching `.NET <Razor.RazorMajorVersion>.0.<Band>xx SDK` channel existed on `main` at the time, re-check now:
- Re-query darc default-channels for `main` (`darc get-default-channels --source-repo https://github.com/{owner}/{repo} --branch main`).
- If a `.NET <Major>.0.<Band>xx SDK` channel matching the current `<RazorMajorVersion>` is now present on `main` and `<Band>` differs from the current `<RazorMinorVersion>` in `eng/Versions.props`, include the `<RazorMajorVersion>`/`<RazorMinorVersion>` edit in this same post-VS-snap follow-up PR (using the same edit pattern as Step 3.5 / 5b).
- If still no matching SDK channel exists on `main`, leave Razor unchanged -- the bump waits for the next opportunity (i.e., when the channel is added).

Remind the user of the exact schedule-derived follow-up date when finishing the snap, and pick this step up when the user resumes this session.

#### 3.10 Review skill for updates

After completing the snap, review whether any steps needed to be done differently than described in this skill. If so, remind the user to update this skill file so future snaps benefit from the improvements.

## Version / VS Branch Conventions

| Concept | Pattern | Example |
|---------|---------|---------|
| Roslyn version | `Major.Minor.Patch` | `5.6.0` |
| VS version (from roslyn) | `(Major+13).(Minor)` | `18.6` |
| VS version (from razor RazorVsixVersionPrefix) | Same as RazorVsixVersionPrefix major.minor | `18.6` |
| Razor SDK version | `<RazorMajorVersion>.<RazorMinorVersion>` from `eng/Versions.props` | `10.4` |
| Razor SDK <-> .NET SDK channel | `Razor X.Y` <-> `.NET X.0.Yxx SDK` | `Razor 10.4` <-> `.NET 10.0.4xx SDK` |
| Named branches | `main` → `release/insiders` → `release/stable` | cascade order |
| VS insertion (main) | `main` | always `main` |
| VS insertion (insiders) | `rel/insiders` | prefix `[Insiders]` |
| VS insertion (stable) | `rel/stable` | prefix `[Stable]` |
| Darc channel | `VS {VS Major}.{VS Minor}` | `VS 18.6` |
| Target milestone | `{VS Major}.{VS Minor}` | `18.6` |
| Servicing branches | `release/dev{vs-version}` or `release/{sdk-band}` | `release/dev18.3`, `release/10.0.4xx` |

## Error Handling

- If a `gh` or `darc` command fails, stop and report the error. Do not retry automatically.
- If the target branch does not exist and cannot be created, report the issue.
- If a subscription already exists in the expected state, skip it and report "already up to date."
- When moving milestones, if a milestone doesn't exist yet, create it first.
