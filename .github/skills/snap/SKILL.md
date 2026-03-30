---
name: snap
description: "Perform a branch snap (release branch cut) for dotnet repos like dotnet/roslyn and dotnet/razor. Use when: snapping a branch, cutting a release branch, creating a release branch, merging main into release, updating VS insertion config, updating darc subscriptions for a snap, moving milestones, or asked about snap workflow."
argument-hint: Which repo and branches to snap (e.g., snap main to release/dev18.6)?
---

# Branch Snap

Perform a branch snap (release branch cut) for dotnet repositories. A snap shifts the content of named branches forward by one VS minor version in a cascade.

> **IMPORTANT**: This skill makes destructive changes (creates branches, opens PRs, updates subscriptions, moves milestones). Always gather info first, present the full plan, and get explicit user confirmation before executing any modifications.

> **NOTE**: This skill works for multiple dotnet repos (e.g., `dotnet/roslyn`, `dotnet/razor`). Do not assume `dotnet/roslyn` — always confirm the repo.

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

After the snap, the old stable VS version (18.4 in this example) is retired. In some cases, a long-lived servicing branch like `release/sdk10.0.3xx` may be created from the old stable content to continue SDK-only flows (not VS insertion).

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

Snap PRs are typically opened from a user's fork. Ask the user for their fork (e.g., `jjonescz/roslyn`). Branches are created in the fork via the GitHub API, and PRs are opened cross-fork with `--head {forkOwner}:{branchName}`. Verify the `gh` account has push access to the fork (`gh api repos/{forkOwner}/{repo} --jq '.permissions'`).

## Workflow

The snap process has three phases: **Gather**, **Plan**, **Execute**.

### Phase 1: Gather Information

Collect all relevant state before proposing any changes:

#### 1.1 Determine repo

- Ask which repo (default: the current repo via `gh repo set-default --view`).

#### 1.2 Discover branches and versions automatically

Use darc default channels and version files to infer the branch structure. Do **not** ask the user for branches or versions — infer them and present for confirmation.

**Step A — List darc default channels** to discover active branches and their channel mappings:
```
darc get-default-channels --source-repo https://github.com/{owner}/{repo}
```
Identify the three named branches (`main`, `release/insiders`, `release/stable`) and their current VS channels.

**Step B — Read versions and configs** from all three branches:
- Fetch `eng/Versions.props` from each branch to get the current version.
  - For roslyn: VS version = `Major + 13`.`Minor` (e.g., Roslyn 5.6 → VS 18.6).
  - For razor: use `<VsixVersionPrefix>` directly.
- Fetch `eng/config/PublishData.json` from each branch to get insertion config (`vsBranch`, `insertionCreateDraftPR`, `insertionTitlePrefix`).
  - The JSON key is `branchInfo` (roslyn) or `branches` (razor).

**Step C — Infer the snap cascade** from the discovered state:
- The snap version is whatever `main` currently targets (e.g., 18.6).
- After snap: main bumps +1 minor, insiders gets main's current version, stable gets insiders' current version.
- The old stable version is retired (unless the user says otherwise, e.g., creating a `release/sdk*` branch).

Present a summary like:
```
Snap for VS 18.6 on dotnet/roslyn:
  main:              18.6 (VS main)     → 18.7 (VS main, version bump)
  release/insiders:  18.5 (rel/insiders) → 18.6 (rel/insiders, merge from main)
  release/stable:    18.4 (rel/stable)   → 18.5 (rel/stable, merge from insiders)
  Old 18.4:          retired
```
Confirm with the user before proceeding.

#### 1.3 Draft pre-snap announcement email

A few days before the snap, a notification email should be sent to the team. Using the info from steps 1.1–1.2, draft the email. Ask the user for the snap date and time.

The email should follow this format:

> **Subject:** Snap for {VS version} on {day of week}, {date}
>
> Hi all,
>
> We will **snap for {VS version} on {day of week}, {date} ~ {time} PST.** main will point to {new VS version} after the snap.
>
> Following is important information about branches and dates.
>
> | Roslyn/Razor Branch | Current Targeting | Notes |
> |---|---|---|
> | main | {current VS version} -> inserts into VS main | Snap for {VS version} on {day}, {date}. main will point to {new VS version} after the snap |
> | release/insiders | {current insiders VS version} | release/insiders will point to {VS version} (rel/insiders) after the snap. {Schedule link}. First QB period is {dates}. |
> | release/stable | NA | release/stable will point to {previous VS version} (rel/stable) after the snap |
> | release/dev{old version} and below | Respective release branches | Only servicing changes |
>
> If there's anything that needs to be checked in for QB mode, please contact the InfraSwat team.

**Adapt the table** to the actual branch structure:
- Include all active branches (main, release/insiders, release/stable, older release branches).
- Fill "Current Targeting" from the `PublishData.json` `vsBranch` of each branch.
- Fill "Notes" with what changes after the snap for each branch.
- If the user provides a QB schedule or schedule link, include it.

Present the draft to the user for review and editing before they send it.

#### 1.4 Check darc subscriptions and flows

List existing subscriptions and backflows:
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

- Ask the user which PR should be the last one included in the target branch.
- Get the merge commit SHA for that PR:
  ```
  gh api repos/{owner}/{repo}/commits/{mergeCommitOid}
  ```

### Phase 2: Present the Plan

After gathering, present **all** planned actions in a numbered list for the user to review. The plan typically includes:

1. **Merge `release/insiders` → `release/stable`**: Open a draft snap PR to bring insiders' content (e.g., 18.5) into stable.

2. **Merge `main` → `release/insiders`**: Open a draft snap PR to bring main's content (e.g., 18.6) into insiders, up to the chosen snap commit.

3. **Update `PublishData.json` on `main`**: Set `insertionCreateDraftPR` to `true`. VS snaps about a week after Roslyn snaps, so during that interim period main's insertions should be drafts to avoid merging into the wrong VS branch. This change goes in the same PR as the version bump.

4. **Update `Versions.props` on `main`**: Bump the minor version (e.g., 5.6.0 → 5.7.0).

5. **Update SARIF files** (roslyn only): Replace old version string with new version in all `.sarif` files under `src/RoslynAnalyzers/`.

6. **Darc subscription changes**: Update default channels to reflect the new version each branch carries:
   - `release/insiders` → channel for the snapped version (e.g., `VS 18.6`)
   - `release/stable` → channel for what was previously insiders (e.g., `VS 18.5`)
   - Also update corresponding VMR flows and backflows for each branch.
   - Remove/retire default channels for the old stable version if no longer needed.

7. **Move milestones**: Move merged PRs and closed issues from `Next` milestone to the target milestone (e.g., `18.6`). Create the milestone if it doesn't exist.

8. **Retire old stable** (if applicable): If the old stable version (e.g., 18.4) is fully retired, remove its darc default channels. If a servicing branch is needed (e.g., `release/sdk10.0.3xx`), note that for the user to handle separately.

Other than the draft flag on `main`, `PublishData.json` typically does **not** need changes during a snap — the named branches keep their fixed insertion targets (`main`, `rel/insiders`, `rel/stable`).

### Post-snap follow-up

After VS snaps (~1 week later), `PublishData.json` on `main` should be updated to set `insertionCreateDraftPR` back to `false` so insertions resume as normal PRs. Remind the user about this follow-up.

Present the plan clearly and ask: **"Shall I proceed with these changes?"**

### Phase 3: Execute (after confirmation)

Only proceed after explicit user confirmation. Execute changes in this order:

#### Merge strategy: "take source" (conflict-free)

Snap merges use a **"take source"** strategy (inspired by the VS repo's `Merge-ToMoreStableBranch`). Instead of letting Git three-way merge source and target (which conflicts on config files), we create a merge commit whose **tree is taken entirely from the source branch** using `git commit-tree` plumbing. The commit has two parents (target branch tip + source commit) so Git records it as a proper merge, but the content comes exclusively from the source — no merge conflicts are possible.

Files that must differ per named branch (specifically `eng/config/PublishData.json`, which controls VS insertion target) are **preserved from the target branch** by modifying the tree via a temporary index before creating the merge commit. This mirrors how `snap.cs` pushes the correct `PublishData.json` to the snap branch.

This approach does NOT touch the working tree — all operations use git plumbing.

#### 3.1 Set up remotes and fetch

```bash
# Add fork remote if not present
git remote add fork https://github.com/{forkOwner}/{repo}.git 2>/dev/null || true

# Fetch latest branch refs
git fetch origin release/insiders release/stable main
```

#### 3.2 Merge insiders → stable

```bash
# Resolve refs
SOURCE_COMMIT=$(git rev-parse origin/release/insiders)
SOURCE_TREE=$(git rev-parse "origin/release/insiders^{tree}")
TARGET_COMMIT=$(git rev-parse origin/release/stable)

# Build a modified tree: take source tree entirely, but preserve PublishData.json from target
TEMP_INDEX=$(mktemp)
GIT_INDEX_FILE=$TEMP_INDEX git read-tree $SOURCE_TREE

TARGET_PD_BLOB=$(git rev-parse "origin/release/stable:eng/config/PublishData.json")
GIT_INDEX_FILE=$TEMP_INDEX git update-index --add --cacheinfo 100644,$TARGET_PD_BLOB,eng/config/PublishData.json

MODIFIED_TREE=$(GIT_INDEX_FILE=$TEMP_INDEX git write-tree)
rm -f "$TEMP_INDEX"

# Create merge commit (two parents, source tree with target's PublishData.json)
MERGE_COMMIT=$(git commit-tree "$MODIFIED_TREE" \
  -p "$TARGET_COMMIT" -p "$SOURCE_COMMIT" \
  -m "Merge release/insiders into release/stable")

# Create branch, push to fork, open draft PR
git branch snap-insiders-to-stable "$MERGE_COMMIT"
git push fork snap-insiders-to-stable

gh pr create --repo {owner}/{repo} \
  --title "Snap release/insiders into release/stable" \
  --body "Auto-generated by snap skill. Snap merge (take-source strategy). PublishData.json preserved from release/stable." \
  --head {forkOwner}:snap-insiders-to-stable --base release/stable --draft
```

> **Note on PublishData.json**: The target's `PublishData.json` is preserved verbatim in the merge tree so that the named branch keeps its fixed insertion config (e.g., `rel/stable` with `[Stable]` prefix). If the source added new fields to the JSON, they'll be lost — in that rare case, manually merge the JSON after the PR is created.

#### 3.3 Merge main → insiders

Same "take source" approach, but use the chosen **snap commit** (not branch HEAD) as the source:

```bash
SOURCE_COMMIT={snapCommitSha}
SOURCE_TREE=$(git rev-parse "{snapCommitSha}^{tree}")
TARGET_COMMIT=$(git rev-parse origin/release/insiders)

# Build modified tree preserving target's PublishData.json
TEMP_INDEX=$(mktemp)
GIT_INDEX_FILE=$TEMP_INDEX git read-tree $SOURCE_TREE

TARGET_PD_BLOB=$(git rev-parse "origin/release/insiders:eng/config/PublishData.json")
GIT_INDEX_FILE=$TEMP_INDEX git update-index --add --cacheinfo 100644,$TARGET_PD_BLOB,eng/config/PublishData.json

MODIFIED_TREE=$(GIT_INDEX_FILE=$TEMP_INDEX git write-tree)
rm -f "$TEMP_INDEX"

MERGE_COMMIT=$(git commit-tree "$MODIFIED_TREE" \
  -p "$TARGET_COMMIT" -p "$SOURCE_COMMIT" \
  -m "Merge main into release/insiders")

git branch snap-main-to-insiders "$MERGE_COMMIT"
git push fork snap-main-to-insiders

gh pr create --repo {owner}/{repo} \
  --title "Snap main into release/insiders" \
  --body "Auto-generated by snap skill. Snap merge (take-source strategy). PublishData.json preserved from release/insiders." \
  --head {forkOwner}:snap-main-to-insiders --base release/insiders --draft
```

> **PowerShell note**: On Windows, adapt the commands for PowerShell. Use `$env:GIT_INDEX_FILE` for environment variables, and `New-TemporaryFile` or `[System.IO.Path]::GetTempFileName()` for temp files. Set `$env:GIT_INDEX_FILE` before each git command and restore it afterward.

#### 3.4 Update configuration files on main

For config file changes to the source branch (e.g., `Versions.props` bump, SARIF updates, `PublishData.json` draft flag), create a branch in the fork and update files via the GitHub API:

```bash
# Create branch in fork from main HEAD
MAIN_SHA=$(gh api repos/{owner}/{repo}/git/refs/heads/main --jq '.object.sha')
gh api -X POST repos/{forkOwner}/{repo}/git/refs \
  --field ref=refs/heads/{updateBranch} --field sha=$MAIN_SHA

# Get current file SHA and update
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

#### 3.5 Update darc subscriptions

For new flows:
```
darc add-default-channel --repo https://github.com/{owner}/{repo} --branch {branch} --channel "{channelName}"
darc add-subscription --source-repo https://github.com/{owner}/{repo} --target-repo https://github.com/dotnet/dotnet --target-branch {vmrBranch} --channel "{channelName}" --update-frequency EveryDay --source-enabled --target-directory {repoName}
```

Before adding a default channel, delete existing associations for the same channel (only one branch should publish to a given channel):
```
darc get-default-channels --source-repo https://github.com/{owner}/{repo} --channel "{channelName}"
darc delete-default-channel --id {id}
```

For updating existing subscriptions:
```
darc get-subscriptions --exact --source-repo {sourceUrl} --target-repo {targetUrl}
darc update-subscription --id {subscriptionId} --channel "{newChannel}"
```

#### 3.6 Move milestones

Create the target milestone if needed (milestone name is just the version number, e.g., `18.6`):
```
gh api -X POST repos/{owner}/{repo}/milestones --field title="{milestoneName}"
```

Move PRs and issues:
```
gh pr edit {prNumber} --repo {owner}/{repo} --milestone "{targetMilestone}"
gh issue edit {issueNumber} --repo {owner}/{repo} --milestone "{targetMilestone}"
```

## Version / VS Branch Conventions

| Concept | Pattern | Example |
|---------|---------|---------|
| Roslyn version | `Major.Minor.Patch` | `5.6.0` |
| VS version (from roslyn) | `(Major+13).(Minor)` | `18.6` |
| VS version (from razor VsixVersionPrefix) | Same as VsixVersionPrefix major.minor | `18.6` |
| Named branches | `main` → `release/insiders` → `release/stable` | cascade order |
| VS insertion (main) | `main` | always `main` |
| VS insertion (insiders) | `rel/insiders` | prefix `[Insiders]` |
| VS insertion (stable) | `rel/stable` | prefix `[Stable]` |
| Darc channel | `VS {VS Major}.{VS Minor}` | `VS 18.6` |
| Target milestone | `{VS Major}.{VS Minor}` | `18.6` |
| Servicing branches | `release/dev{version}` or `release/sdk{version}` | `release/dev18.3`, `release/sdk10.0.3xx` |

## Error Handling

- If a `gh` or `darc` command fails, stop and report the error. Do not retry automatically.
- If the target branch does not exist and cannot be created, report the issue.
- If a subscription already exists in the expected state, skip it and report "already up to date."
- When moving milestones, if a milestone doesn't exist yet, create it first.
