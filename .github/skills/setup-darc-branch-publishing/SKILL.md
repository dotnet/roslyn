---
name: setup-darc-branch-publishing
description: "Configure a dotnet repository branch to publish build artifacts through a Maestro channel. Use when: publishing branch artifacts to General Testing or another NuGet feed, adding a Darc default channel, enabling official builds for a branch, or setting up branch publishing flow."
argument-hint: "Repository, branch, and Maestro channel (for example: dotnet/roslyn demos/my-experiment General Testing)"
---

# Set Up Darc Branch Publishing

Configure a branch so official builds register their assets in BAR and publish
them through a Maestro channel such as `General Testing`.

> **IMPORTANT**: This workflow creates a PR in the Maestro configuration
> repository and changes a repository's official pipeline triggers. Present the
> planned changes and get explicit user confirmation before making external
> changes.

> **PUSHES**: Never push a repository branch without explicit user permission.

## Prerequisites

- `darc` is installed and authenticated with Maestro.
- `gh` is installed and authenticated for the source repository.
- Azure CLI is authenticated to the `dnceng` Azure DevOps organization.
- The user has permission to update the target branch and create a PR against
  the repository's default branch.

## Phase 1: Gather

### 1. Confirm the exact repository and branch

Do not assume a similarly named branch is the intended target. Check both the
upstream repository and the user's fork:

```bash
git remote -v
git ls-remote --heads upstream "refs/heads/<branch>"
gh api "repos/<owner>/<repo>/branches/<url-encoded-branch>"
```

If the requested branch does not exist but a similarly named branch does, ask
the user which branch to configure.

### 2. Find the channel and prior art

List channels and existing default-channel associations:

```bash
darc get-channels --output-format json
darc get-default-channels \
  --source-repo "https://github.com/<owner>/<repo>" \
  --output-format json
```

Confirm the requested channel exists. Look for another experimental branch
already using that channel to validate the intended pattern.

Before adding anything, check whether the exact repository, branch, and channel
association already exists. Do not create a duplicate.

### 3. Inspect repository publishing configuration

Find the official build pipeline and its post-build publishing setup:

```bash
rg -n "trigger:|publish-build-assets|post-build.yml|PromoteToChannel" \
  azure-pipelines*.yml eng
```

For Roslyn:

- `azure-pipelines-official.yml` controls which branches trigger official
  builds.
- The existing Arcade `publish-build-assets.yml` and `post-build.yml` hooks
  register assets in BAR and promote builds to their default channels.
- `eng/Publishing.props` controls package metadata, but normally needs no
  branch-specific change.
- Do not edit `eng/common`; it is Arcade-managed.

The branch must be included in the official pipeline trigger. Azure Pipelines
evaluates the YAML from the branch being built, so make the trigger change on
the target branch for immediate effect and submit the same change to the
default branch as the durable repository configuration.

## Phase 2: Plan

Present these planned actions:

1. Add the branch-to-channel default association through Darc.
2. Add the branch to the official pipeline trigger on the target branch.
3. Push the target-branch change only after explicit permission.
4. Submit the same pipeline change, plus any workflow documentation updates,
   in a PR against the repository's default branch.
5. Report the Maestro configuration PR, which must merge before the association
   takes effect.

Get explicit user confirmation before proceeding.

## Phase 3: Execute

### 1. Add the Darc default channel

The basic command is:

```bash
darc add-default-channel \
  --channel "<channel>" \
  --branch "<branch>" \
  --repo "https://github.com/<owner>/<repo>" \
  --quiet
```

If Darc cannot read the GitHub branch, pass the authenticated GitHub token
without printing it:

```bash
darc add-default-channel \
  --channel "<channel>" \
  --branch "<branch>" \
  --repo "https://github.com/<owner>/<repo>" \
  --quiet \
  --github-pat "$(gh auth token)"
```

If the Maestro configuration repository returns HTTP 401, obtain an Azure
DevOps access token for the Azure DevOps resource and pass it directly:

```bash
AZDO_TOKEN="$(
  az account get-access-token \
    --resource 499b84ac-1321-427f-aa17-267ca6975798 \
    --query accessToken \
    --output tsv
)"

darc add-default-channel \
  --channel "<channel>" \
  --branch "<branch>" \
  --repo "https://github.com/<owner>/<repo>" \
  --quiet \
  --github-pat "$(gh auth token)" \
  --azdev-pat "$AZDO_TOKEN"
```

Do not add `--output-format json` to `add-default-channel`; that operation does
not support JSON output.

Record the generated Maestro configuration PR URL. The channel association is
not active until that PR merges.

### 2. Enable official builds on the target branch

Add the exact branch under `trigger.branches.include` in the official pipeline
YAML. Make the smallest possible change and verify it:

```bash
git diff --check
git diff -- azure-pipelines-official.yml
```

Commit and push only after explicit user permission.

### 3. Create the durable default-branch PR

Create a new branch from the latest upstream default branch. Apply the same
official-pipeline trigger change and include any workflow documentation or
skill updates requested by the user.

Validate the diff, commit with the repository's required commit trailers, push
to the user's fork after explicit permission, and open a PR against the
upstream default branch.

Do not post PR comments, reviews, or replies. Provide the PR URL to the user.

## Verification

After the Maestro configuration PR merges, verify the association:

```bash
darc get-default-channels \
  --source-repo "https://github.com/<owner>/<repo>" \
  --branch "<branch>" \
  --channel "<channel>" \
  --output-format json
```

Verify that:

- The association is present and enabled.
- The target branch contains the official pipeline trigger.
- The default-branch PR contains the durable trigger configuration.
- A subsequent official build is registered in BAR and promoted to the
  requested channel.

## Error Handling

- Branch name mismatch: stop and ask the user to choose the exact branch.
- Existing association: do not add a duplicate.
- GitHub HTTP 401: retry with `--github-pat "$(gh auth token)"`.
- Azure DevOps HTTP 401: retry with an Azure DevOps resource access token.
- Maestro configuration PR is unmerged: report that publishing is not active
  yet.
- Missing official publishing hooks: stop and investigate the repository's
  Arcade publishing setup before changing Darc.
