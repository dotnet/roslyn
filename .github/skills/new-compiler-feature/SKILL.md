---
name: new-compiler-feature
description: "Set up tracking for a new C# compiler feature: create test plan issue, feature label, update Language Feature Status page, and link related PRs. Use when: setting up a new language feature, creating a test plan, tracking a new csharplang proposal in roslyn, or asked to 'set up feature tracking'."
argument-hint: "Roslyn PR URL implementing the feature (e.g., https://github.com/dotnet/roslyn/pull/83288)"
---

# New Compiler Feature Setup

Set up tracking for a new C# language feature in dotnet/roslyn, starting from an implementation PR.

> **IMPORTANT**: Present the full plan and get explicit user confirmation before making any changes.

> **IDEMPOTENCY**: Safe to re-run. Checks for existing artifacts before creating. Use it to fix missing pieces.

## Prerequisites

- `gh` CLI authenticated with write access to `dotnet/roslyn` (verify with `gh auth status`)

Do not proceed if prerequisites are not met. Instead, guide the user through the necessary actions (installing `gh`, authenticating, etc.).

## Phase 1: Gather

### 1.1 Parse the PR

```powershell
gh pr view <NUMBER> --repo dotnet/roslyn --json title,body,author,baseRefName,labels,number
```

Extract from PR body (ask user if missing):
- **csharplang issue URL**: `https://github.com/dotnet/csharplang/issues/NNNN`
- **Spec URL**: `https://github.com/dotnet/csharplang/blob/main/proposals/*.md`

Extract from metadata:
- **Developer**: PR author
- **Branch**: PR base ref (`features/*` or `main`)

### 1.2 Validate csharplang issue

```powershell
gh issue view <NUMBER> --repo dotnet/csharplang --json title,labels,assignees
```

**Abort** if the issue lacks the `Proposal champion` label.

Extract **LDM champ** from assignees (ask user to pick if multiple).

### 1.3 Feature name

Offer two candidates, ask user to pick or edit:
1. From spec filename (kebab-case → readable text)
2. From csharplang issue title (strip `[Proposal]: ` prefix)

This name is used for the label (`Feature - <Name>`) and test plan title.

### 1.4 Reviewer and IDE buddy

Ask user (default: "TBD" for both).

### 1.5 Verify feature branch

If base ref ≠ `main`, verify it exists (URL-encode the branch name for the API call). Warn if missing.

### 1.6 Collect related PRs

1. Always include the input PR
2. Parse `#NNNN` references from stacked-PR lists in the body; validate each is a PR via `gh pr view`
3. Search for open PRs targeting the same feature branch (if not `main`)
4. Deduplicate; present list to user for confirmation

### 1.7 Check existing artifacts

- **Label**: `gh label list --repo dotnet/roslyn --search "Feature - <Name>"`
- **Test plan issue**: `gh issue list --repo dotnet/roslyn --search "<csharplang_url>" --label "Area-Compilers"` — match by csharplang URL in body
- **Feature status row**: grep for csharplang issue URL in `docs/Language Feature Status.md`
- **Status PR**: `gh pr list --repo dotnet/roslyn --head "feature-status/<spec-filename>" --state open`

## Phase 2: Plan

Present all gathered info and planned actions. Mark existing artifacts with ✓. Ask for explicit confirmation.

## Phase 3: Execute

### 3.1 Create feature label

Skip if exists. Color: `2ad0ad`.

```powershell
gh label create "Feature - <Name>" --repo dotnet/roslyn --color "2ad0ad"
```

### 3.2 Create test plan issue

Skip if exists. Assign to the developer and reviewers (assignment may silently fail for external contributors — that's OK).

```powershell
gh issue create --repo dotnet/roslyn `
  --title 'Test plan for "<Name>" feature' `
  --body "Championed proposal: <csharplang_url>`nSpec: <spec_url>`n`n(created by new-compiler-feature skill)" `
  --label "Area-Compilers" --label "Feature - <Name>" `
  --assignee "<developer>" --assignee "<reviewer1>" --assignee "<reviewer2>"
```

Set issue type to "Feature" via GraphQL:
```powershell
# Get issue type ID for "Feature"
gh api graphql -f query='{ repository(owner: "dotnet", name: "roslyn") { issueTypes(first: 20) { nodes { id name } } } }'
# Get issue node ID, then update
gh api graphql -f query='mutation { updateIssue(input: {id: "<issueId>", issueTypeId: "<featureTypeId>"}) { issue { issueType { name } } } }'
```

### 3.3 Label related PRs

For each PR, add `Feature - <Name>` label if not already applied.

### 3.4 Comment on related PRs

For each PR, check if a comment with the exact test plan URL exists. If not:

```powershell
gh pr comment <NUMBER> --repo dotnet/roslyn --body "Test plan: <test_plan_url>`n(created by new-compiler-feature skill)"
```

### 3.5 Update Language Feature Status page

Skip if the csharplang issue URL already appears in the file.

**Row format:**
```
| [<Title>](<csharplang_url>) | <branch_col> | [In progress](<test_plan_url>) | [<dev>](https://github.com/<dev>) | <reviewers> | <ide_buddy> | [<ldm>](https://github.com/<ldm>) |
```

- Branch column: `[<short-name>](https://github.com/dotnet/roslyn/tree/<full-ref>)` for feature branches; plain `main` otherwise
- Reviewers/IDE buddy: `TBD` or `[<user>](https://github.com/<user>)` (comma-separated if multiple)
- Insert after the last "In progress" row, before the first "Merged" row in the Working Set C# table. If no merged rows, append after the last row.

### 3.6 Create PR for feature status update

Skip if an open PR already exists.

Use the GitHub API to create the branch and commit **without touching the local working tree**:

```powershell
# Get latest main SHA
$mainSha = gh api repos/dotnet/roslyn/git/refs/heads/main --jq '.object.sha'

# Create branch
gh api -X POST repos/dotnet/roslyn/git/refs -f ref="refs/heads/feature-status/<spec-filename>" -f sha="$mainSha"

# Get current file content and SHA from main
$fileMeta = gh api "repos/dotnet/roslyn/contents/docs/Language Feature Status.md" --jq '{sha: .sha, content: .content}' | ConvertFrom-Json

# Decode content, insert the new row, re-encode to base64, then update:
gh api -X PUT "repos/dotnet/roslyn/contents/docs/Language Feature Status.md" `
  -f message='Track status for feature "<Name>"' `
  -f branch="feature-status/<spec-filename>" `
  -f sha="$($fileMeta.sha)" `
  -f content="$newContentBase64"

# Create PR
gh pr create --repo dotnet/roslyn `
  --title 'Track status for feature "<Name>"' `
  --body "Adds feature status tracking row.`n`n- csharplang issue: <csharplang_url>`n- Test plan: <test_plan_url>`n`nFYI @<developer> @<reviewer(s)> @<ldm_champ>`n`n(created by new-compiler-feature skill)" `
  --head "feature-status/<spec-filename>" --base main `
  --label "Area-Compilers" --label "Documentation"
```

**Present the PR URL to the user.**

### Summary

```
✅ Feature setup complete for "<Name>"
- Label: Feature - <Name>
- Test plan: <url>
- Feature status PR: <url>
- PRs labeled and linked: #NNN, #NNN
```

## Error Handling

- `gh` command failure → stop and report
- Missing "Proposal champion" label → abort
- Missing feature branch (non-main) → warn, let user decide
- Always check for existing artifacts before creating
