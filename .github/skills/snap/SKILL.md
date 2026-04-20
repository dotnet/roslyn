---
name: snap
description: "Perform a branch snap (release branch cut) for dotnet repos like dotnet/roslyn and dotnet/razor. Use when: snapping a branch, cutting a release branch, creating a release branch, merging main into release, updating VS insertion config, updating darc subscriptions for a snap, moving milestones, or asked about snap workflow."
argument-hint: Which repo and branches to snap (e.g., snap main to release/dev18.6)?
---

# Branch Snap

Perform a branch snap (release branch cut) for dotnet repositories. A snap shifts the content of named branches forward by one VS minor version in a cascade.

> **IMPORTANT**: This skill makes destructive changes (creates branches, opens PRs, updates subscriptions, moves milestones). Always gather info first, present the full plan, and get explicit user confirmation before executing any modifications.

> **NOTE**: This skill works for multiple dotnet repos (e.g., `dotnet/roslyn`, `dotnet/razor`). Do not assume `dotnet/roslyn` — always confirm the repo.

> **SKILL MAINTENANCE**: If you deviate from this skill during execution (e.g., a step doesn't work as described, a new step is needed, or the process has changed), remind the user to update this skill file so future snaps benefit from the fix.

> **SESSION**: A snap spans multiple days (initial snap, then post-VS-snap follow-up ~1 week later). Recommend the user reuse the same chat session throughout one snap cycle so context (PR numbers, branch names, channel IDs, etc.) is preserved. If starting a new session, review session memory for prior snap state.

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

Snap PRs are typically opened from a user's fork. Ask the user for their fork (e.g., `{username}/roslyn`). Branches are created in the fork via the GitHub API, and PRs are opened cross-fork with `--head {forkOwner}:{branchName}`. Verify the `gh` account has push access to the fork (`gh api repos/{forkOwner}/{repo} --jq '.permissions'`).

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
Identify the three named branches (`main`, `release/insiders`, `release/stable`) and their current VS channels. Also note any SDK channels (e.g., `.NET 10.0.3xx SDK`, `.NET 11.0.1xx SDK`) assigned to each branch — these will need to shift during the snap.

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
- SDK channels shift to follow the content: any SDK channel on `main` that corresponds to the current VS version moves to `release/insiders` (since insiders now carries that content). SDK channels for the next major (e.g., `.NET 11.0.1xx SDK`) stay on `main`. If the next SDK band channel doesn't exist yet, note it as a follow-up.

Present a summary like:
```
Snap for VS 18.6 on dotnet/roslyn:
  main:              18.6 (VS main, .NET 10.0.3xx SDK)  → 18.7 (VS main, .NET 10.0.4xx SDK*)
  release/insiders:  18.5 (VS 18.5)                     → 18.6 (VS 18.6, .NET 10.0.3xx SDK)
  release/stable:    18.4 (VS 18.4)                     → 18.5 (VS 18.5)
  Old 18.4:          retired
  * .NET 10.0.4xx SDK channel may not exist yet — follow up when created
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
> | Branch | Current VS | Current SDK | After-snap VS | After-snap SDK | Notes |
> |---|---|---|---|---|---|

Fill in each row using the VS channels and SDK channels discovered in step 1.2. For example, if `main` currently flows to `.NET 10.0.3xx SDK` and `.NET 11.0.1xx SDK`, show those in "Current SDK" and show the after-snap state (SDK channel moves to insiders, next-major stays on main). Mark channels that don't exist yet with `*` and add a footnote.

> \* {channel name} will be added when that channel is created.
>
> If there's anything that needs to be checked in for QB mode, please contact the InfraSwat team.

**Adapt the table** to the actual branch structure:
- Include all active named branches and relevant servicing branches.
- Fill current/after-snap columns from darc default channels and `PublishData.json`.
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

1. **Merge `release/insiders` → `release/stable`**: Open a draft snap PR to bring insiders' content (e.g., 18.5) into stable. Construct a custom `PublishData.json` with `insertionTitlePrefix` = `[Stable]`. Ask the user whether `vsBranch` should temporarily be `rel/insiders` (interim redirect before VS snaps) or `rel/stable` (no redirect). Default is typically **no redirect**.

2. **Merge `main` → `release/insiders`**: Open a draft snap PR to bring main's content (e.g., 18.6) into insiders, up to the chosen snap commit. Construct a custom `PublishData.json` with `insertionTitlePrefix` = `[Insiders]` and `vsBranch` = `main` (temporary — VS hasn't snapped yet, so `rel/insiders` still points to the old version).

3. **Update `PublishData.json` on `main`**: Set `insertionCreateDraftPR` to `true`. VS snaps about a week after Roslyn snaps, so during that interim period main's insertions should be drafts to avoid merging into the wrong VS branch. This change goes in the same PR as the version bump.

4. **Update `Versions.props` on `main`**: Bump the minor version (e.g., 5.6.0 → 5.7.0) and reset `PreReleaseVersionLabel` to `1`.

5. **Update SARIF files** (roslyn only): Replace old version string with new version in all `.sarif` files under `src/RoslynAnalyzers/` (search recursively).

6. **Darc channel changes**: Update default channels to reflect the new version each branch carries:
   - `release/insiders` → VS channel for the snapped version (e.g., `VS 18.6`)
   - `release/stable` → VS channel for what was previously insiders (e.g., `VS 18.5`)
   - SDK channels: move `.NET 10.0.Nxx SDK` from `main` to `release/insiders` (insiders now carries that SDK band). `main` will be added to the next SDK band (e.g., `.NET 10.0.(N+1)xx SDK`) when that channel is created.
   - Also update corresponding VMR flows and backflows for each branch if needed.
   - Remove/retire default channels for the old stable version if no longer needed.

7. **Move milestones**: Move merged PRs and closed issues from `Next` milestone to the target milestone (e.g., `18.6`). Create the milestone if it doesn't exist.

8. **Retire old stable** (if applicable): If the old stable version (e.g., 18.4) is fully retired, remove its darc default channels. If a servicing branch is needed (e.g., `release/sdk10.0.3xx`), note that for the user to handle separately.

**`PublishData.json` interim handling**: During the ~1 week gap between Roslyn snap and VS snap, named branches need temporary insertion target overrides because VS branch names haven't shifted yet. These temporary changes are included directly in the snap merge PRs (for non-main branches) and reverted after VS snaps (see step 3.8).

Present the plan clearly and ask: **"Shall I proceed with these changes?"**

### Phase 3: Execute (after confirmation)

Only proceed after explicit user confirmation. Execute changes in this order:

#### Merge strategy: "take source" (conflict-free)

Snap merges use a **"take source"** strategy (inspired by the VS repo's `Merge-ToMoreStableBranch`). Instead of letting Git three-way merge source and target (which conflicts on config files), we create a merge commit whose **tree is taken entirely from the source branch** using `git commit-tree` plumbing. The commit has two parents (target branch tip + source commit) so Git records it as a proper merge, but the content comes exclusively from the source — no merge conflicts are possible.

Files that must differ per named branch (specifically `eng/config/PublishData.json`, which controls VS insertion target) are **replaced with a custom version** constructed from the source's package list and the correct `branchInfo` values for the target branch. This mirrors how `snap.cs` pushes the correct `PublishData.json` to the snap branch.

This approach does NOT touch the user's working tree — all operations use git plumbing (temp index, `commit-tree`, `write-tree`) which do not read or modify the working tree or the repo's main index.

#### 3.1 Set up remotes and fetch

Ask the user for the **path to their local clone** of the repo (e.g., `D:\roslyn`). Default to the current workspace folder if it matches the repo being snapped.

**Find the fork remote name** — the user likely already has a remote for their fork. List remotes (`git remote -v`) and find the one pointing to `{forkOwner}/{repo}`. Use that name as `{forkRemote}` throughout. Do **not** create a new remote if one already exists.

Also identify the upstream remote (typically `origin` or `dotnet`) — the one pointing to `{owner}/{repo}`. Use that as `{upstreamRemote}`.

```bash
cd {repoPath}
git fetch {upstreamRemote} release/insiders release/stable main
```

#### 3.2 Merge insiders → stable

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

#### 3.3 Merge main → insiders

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
   - **Razor** (uses `branches` key with branch name as sub-key, e.g., `"branches": { "main": { ... } }`): Rename the sub-key from the source branch name to the target branch name (e.g., `main` → `release/insiders`), and replace `vsBranch`, `insertionTitlePrefix`, `insertionCreateDraftPR`.
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

#### 3.4 Update configuration files on main

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

#### 3.5 Update darc default channels

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

Repeat for every repo and branch being snapped (e.g., insiders and stable for both roslyn and razor).

**SDK channels**: Move the current SDK band from `main` to `release/insiders`:
```
# Delete main → .NET 10.0.Nxx SDK
darc delete-default-channel --id {id} --configuration-branch {cfgBranch} --no-pr --ci

# Add insiders → .NET 10.0.Nxx SDK
darc add-default-channel --repo https://github.com/{owner}/{repo} --branch release/insiders --channel ".NET 10.0.Nxx SDK" --configuration-branch {cfgBranch} --no-pr --ci
```

Note: Adding `main` to the next SDK band (e.g., `.NET 10.0.(N+1)xx SDK`) is a **follow-up** — that channel may not exist yet at snap time.

If subscription changes are also needed (e.g., VMR flows), they use the same config repo and can be batched onto the same branch. When creating a forward-flow subscription (repo → dotnet/dotnet), also create the corresponding **backflow** subscription (dotnet/dotnet → repo).

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

After all commands, create one PR with auto-complete enabled and print the URL:
```
$prId = az repos pr create --repository maestro-configuration --org https://dev.azure.com/dnceng --project internal --source-branch {cfgBranch} --target-branch production --title "Snap: update default channels for {repos} ({newVsVersion})" --description "Updates default channels for the {newVsVersion} snap.`n`nAuto-generated by snap skill." --query pullRequestId -o tsv
az repos pr update --id $prId --org https://dev.azure.com/dnceng --auto-complete true --squash true --query pullRequestId -o tsv
Write-Output "PR: https://dev.azure.com/dnceng/internal/_git/maestro-configuration/pullrequest/$prId"
```

#### 3.6 Move milestones

Create the target milestone if needed (milestone name is just the version number, e.g., `18.6`):
```
gh api -X POST repos/{owner}/{repo}/milestones --field title="{milestoneName}"
```

List merged PRs and closed issues in the `Next` milestone:
```
$prs = gh pr list --repo {owner}/{repo} --search "is:merged milestone:Next base:main" --json number --limit 200 | ConvertFrom-Json
$issues = gh issue list --repo {owner}/{repo} --search "is:closed milestone:Next" --json number --limit 200 | ConvertFrom-Json
```

Get the milestone number for the target milestone:
```
$msNumber = gh api repos/{owner}/{repo}/milestones --jq '.[] | select(.title == "{milestoneName}") | .number'
```

Move them using the REST API (more reliable than `gh pr edit` / `gh issue edit` for bulk operations):
```
foreach ($pr in $prs) { gh api repos/{owner}/{repo}/issues/$($pr.number) -X PATCH -f milestone=$msNumber }
foreach ($iss in $issues) { gh api repos/{owner}/{repo}/issues/$($iss.number) -X PATCH -f milestone=$msNumber }
```

#### 3.7 Reply to the snap announcement email

After all snap steps are completed, draft a reply to the pre-snap announcement email (from step 1.3) confirming the snap is done. Don't include links to created PRs. Summarize what each branch now targets (VS channel and SDK channels, using the values discovered in step 1.2). Mention any pending follow-ups (e.g., SDK channel not yet created).

Present the draft to the user before they send it.

#### 3.8 Post-VS-snap config updates

After VS snaps (~1 week later), the interim `PublishData.json` overrides need reverting. For each repo, create a **draft PR** to update:

- **`main`**: Set `insertionCreateDraftPR` back to `false` so insertions are no longer drafts.
- **`release/insiders`**: Change `vsBranch` from `main` to `rel/insiders` (VS has now created the `rel/insiders` branch).
- **`release/stable`** (only if temporarily redirected during snap): Change `vsBranch` to `rel/stable`.

In the PR body/description, include a brief note such as `Auto-generated by snap skill. Post-VS-snap PublishData.json follow-up.` so reviewers know the cleanup was generated by this workflow.

Also handle any pending SDK channel follow-ups (e.g., adding `main` to a newly created `.NET 10.0.Nxx SDK` channel).

This step happens ~1 week after the initial snap — remind the user about it when finishing the snap, and pick it up when the user resumes this session.

#### 3.9 Review skill for updates

After completing the snap, review whether any steps needed to be done differently than described in this skill. If so, remind the user to update this skill file so future snaps benefit from the improvements.

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
