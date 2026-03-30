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

#### 3.1 Merge insiders → stable

Create a draft snap PR to merge `release/insiders` into `release/stable`. Create the branch in the user's fork and open a cross-fork PR:
```
gh api repos/{owner}/{repo}/git/refs/heads/release/insiders --jq '.object.sha'  # get head SHA
gh api -X POST repos/{forkOwner}/{repo}/git/refs --field ref=refs/heads/snap-insiders-to-stable --field sha={insidersHeadSha}
gh pr create --repo {owner}/{repo} --title "Snap release/insiders into release/stable" --body "Auto-generated by snap skill." --head {forkOwner}:snap-insiders-to-stable --base release/stable --draft
```

#### 3.2 Merge main → insiders

Create a draft snap PR to merge `main` into `release/insiders` up to the chosen snap commit:
```
gh api -X POST repos/{forkOwner}/{repo}/git/refs --field ref=refs/heads/snap-main-to-insiders --field sha={snapCommitSha}
gh pr create --repo {owner}/{repo} --title "Snap main into release/insiders" --body "Auto-generated by snap skill." --head {forkOwner}:snap-main-to-insiders --base release/insiders --draft
```

#### 3.3 Update configuration files

For each config file change (e.g., `Versions.props`, SARIF files), create a branch in the fork and update files via the GitHub API:
```
# Get current file SHA
gh api -X GET repos/{owner}/{repo}/contents/{filePath} --field ref=refs/heads/{branch}

# Create branch in fork from the target branch head
gh api repos/{owner}/{repo}/git/refs/heads/{branch} --jq '.object.sha'
gh api -X POST repos/{forkOwner}/{repo}/git/refs --field ref=refs/heads/{updateBranch} --field sha={branchHeadSha}

# Update file
gh api -X PUT repos/{forkOwner}/{repo}/contents/{filePath} \
  --field message="Update {fileName}" \
  --field branch={updateBranch} \
  --field sha={fileSha} \
  --field content={base64Content}
```

Then open a draft PR for the config changes:
```
gh pr create --repo {owner}/{repo} --title "Update snap configuration files" --body "Auto-generated by snap skill." --head {forkOwner}:{updateBranch} --base {branch} --draft
```

#### 3.4 Update darc subscriptions

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

#### 3.5 Move milestones

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
