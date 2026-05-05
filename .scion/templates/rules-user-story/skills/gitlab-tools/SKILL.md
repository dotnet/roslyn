---
name: gitlab-tools
description: >-
  provides instructions for interacting with the GitLab API via the glab CLI
  to list, view, create, edit, and manage GitLab issues, merge requests,
  branches, and labels. Requires user approval for all mutating operations.
---

# GitLab Tools Skill

This skill wraps the **[glab CLI](https://gitlab.com/gitlab-org/cli)** to interact with GitLab projects — listing and creating issues, managing merge requests, working with branches, and more. The agent constructs `glab` commands and executes them via `execute_command`.

## ⚠️ Safety: User Confirmation Required

All commands are classified as either **read-only** (safe) or **mutating** (dangerous).

| Category  | Action                                                                                                                                  | Requires User Approval        |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| 🟢 Read   | List issues, view issue, list MRs, view MR, list branches, list labels, view project info, view pipelines                               | **No** — execute directly     |
| 🔴 Mutate | Create issue, edit issue, close issue, reopen issue, create MR, edit MR, merge MR, create branch, delete branch, add labels, push files | **Yes** — MUST ask user first |

**CRITICAL RULE:** Before executing ANY mutating command, you MUST:

1. Show the user the exact `glab` command you intend to run
2. Explain what the command will do
3. Wait for explicit user approval via `ask_followup_question`
4. Only execute after the user confirms

Read-only commands may be executed directly without asking.

## Prerequisites

- **glab CLI** installed and available on the system PATH
  - **Windows:** `choco install glab` (via [Chocolatey](https://chocolatey.org/))
  - **Linux:** `apt install glab` or see [glab installation docs](https://gitlab.com/gitlab-org/cli#installation)
  - **macOS:** `brew install glab`
- **Authentication** configured via one of:
  - `glab auth login --hostname <host>` (interactive, recommended)
  - `GITLAB_TOKEN` environment variable set with a Personal Access Token (PAT)

If `glab` is not found, instruct the user to install it using the appropriate method above. **Do NOT attempt to install it automatically.**

## Environment Configuration

Connection parameters are pre-configured in [`env.config`](env.config). Read them to construct commands — **do not ask the user for project details** unless overriding.

| Variable         | Description                      | Default         |
| ---------------- | -------------------------------- | --------------- |
| `GITLAB_HOST`    | GitLab instance hostname         | from env.config |
| `GITLAB_PROJECT` | Default `namespace/project` path | from env.config |

## How to Run Commands

All commands use the `glab` CLI via `execute_command`. Always include the `--repo` flag with the project from `env.config`:

```
glab <resource> <action> --repo <GITLAB_HOST>/<GITLAB_PROJECT> [options]
```

> **Note:** Output is plain text by default. Use `--output json` where supported for structured output that's easier to parse.

---

## Available Commands

### 1. Issues

#### List Issues (🟢 Read)

```
glab issue list --repo <GITLAB_HOST>/<GITLAB_PROJECT> --output json
```

**Common filters:**

```
glab issue list --repo <host>/<project> --state opened --output json
glab issue list --repo <host>/<project> --label "bug" --output json
glab issue list --repo <host>/<project> --assignee "@me" --output json
glab issue list --repo <host>/<project> --search "telegram" --output json
glab issue list --repo <host>/<project> --per-page 50 --output json
```

#### View Issue (🟢 Read)

```
glab issue view <issue_id> --repo <host>/<project> --output json
```

View with comments:

```
glab issue view <issue_id> --repo <host>/<project> --comments --output json
```

#### Create Issue (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab issue create --repo <host>/<project> --title "Issue title" --description "Description" --label "label1,label2"
```

**Available options:**

- `--title` (required): Issue title
- `--description`: Issue body (supports Markdown)
- `--label`: Comma-separated labels
- `--assignee`: Username to assign
- `--milestone`: Milestone title or ID
- `--confidential`: Mark as confidential
- `--weight`: Issue weight (integer)

#### Edit Issue (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab issue update <issue_id> --repo <host>/<project> --title "New title" --description "Updated description"
```

#### Close Issue (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab issue close <issue_id> --repo <host>/<project>
```

#### Reopen Issue (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab issue reopen <issue_id> --repo <host>/<project>
```

#### Add Note/Comment to Issue (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab issue note <issue_id> --repo <host>/<project> --message "Comment text"
```

### 2. Merge Requests

#### List Merge Requests (🟢 Read)

```
glab mr list --repo <host>/<project> --output json
```

**Common filters:**

```
glab mr list --repo <host>/<project> --state opened --output json
glab mr list --repo <host>/<project> --label "ai-assist" --output json
glab mr list --repo <host>/<project> --assignee "@me" --output json
```

#### View Merge Request (🟢 Read)

```
glab mr view <mr_id> --repo <host>/<project> --output json
```

#### Create Merge Request (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab mr create --repo <host>/<project> --source-branch "feature/my-feature" --target-branch "main" --title "MR Title" --description "Description" --label "ai-assist"
```

**Available options:**

- `--source-branch` (required): Source branch name
- `--target-branch` (required): Target branch name
- `--title` (required): MR title
- `--description`: MR body (supports Markdown)
- `--label`: Comma-separated labels
- `--assignee`: Username to assign
- `--reviewer`: Username for reviewer
- `--milestone`: Milestone title or ID
- `--squash-before-merge`: Squash commits on merge
- `--remove-source-branch`: Delete source branch after merge
- `--allow-collaboration`: Allow commits from upstream members
- `--draft`: Create as draft MR

#### Merge a Merge Request (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab mr merge <mr_id> --repo <host>/<project> --squash --remove-source-branch
```

#### Add Labels to Merge Request (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab mr update <mr_id> --repo <host>/<project> --label "label1,label2"
```

### 3. Branches

#### List Branches (🟢 Read)

```
glab api "projects/:id/repository/branches?per_page=100" --repo <host>/<project> | python -c "import sys,json;[print(b['name']) for b in json.load(sys.stdin)]"
```

Or search for a specific branch pattern:

```
glab api "projects/:id/repository/branches?search=feature" --repo <host>/<project>
```

#### Create Branch (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab api "projects/:id/repository/branches" --repo <host>/<project> --method POST --field "branch=feature/my-feature" --field "ref=main"
```

#### Delete Branch (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab api "projects/:id/repository/branches/feature%2Fmy-feature" --repo <host>/<project> --method DELETE
```

### 4. Labels

#### List Labels (🟢 Read)

```
glab label list --repo <host>/<project> --output json
```

#### Create Label (🔴 Mutate — REQUIRES USER APPROVAL)

```
glab label create "label-name" --repo <host>/<project> --color "#428BCA" --description "Label description"
```

### 5. Project Info

#### View Project (🟢 Read)

```
glab repo view --repo <host>/<project> --output json
```

### 6. Pipelines

#### List Pipelines (🟢 Read)

```
glab ci list --repo <host>/<project> --output json
```

#### View Pipeline (🟢 Read)

```
glab ci view <pipeline_id> --repo <host>/<project>
```

### 7. Push Files to a Branch (🔴 Mutate — REQUIRES USER APPROVAL)

Use the GitLab API to commit files directly (useful for task context files):

```
glab api "projects/:id/repository/commits" --repo <host>/<project> --method POST --raw-field '{
  "branch": "feature/my-feature",
  "commit_message": "Add task context file",
  "actions": [
    {
      "action": "create",
      "file_path": "docs/task-context.md",
      "content": "# Task Context\n\nContent here..."
    }
  ]
}'
```

---

## Authentication Setup

### Recommended: Interactive Login

```
glab auth login --hostname <GITLAB_HOST>
```

Replace `<GITLAB_HOST>` with the hostname from your `env.config`. This opens a browser for OAuth or prompts for a token. The credential is stored securely by `glab`.

### Alternative: Environment Variable

Set `GITLAB_TOKEN` with a [Personal Access Token](https://docs.gitlab.com/ee/user/profile/personal_access_tokens.html) that has `api` scope:

```
set GITLAB_TOKEN=glpat-xxxxxxxxxxxxxxxxxxxx   (Windows cmd)
$env:GITLAB_TOKEN = "glpat-xxxxxxxxxxxxxxxxxxxx"   (PowerShell)
export GITLAB_TOKEN=glpat-xxxxxxxxxxxxxxxxxxxx   (Linux/macOS)
```

### Verify Authentication

```
glab auth status --hostname <GITLAB_HOST>
```

## Error Handling

- If `glab` is not found: instruct the user to install it (see Prerequisites)
- If authentication fails: instruct the user to run `glab auth login --hostname <host>`
- If project not found: verify `GITLAB_PROJECT` in `env.config` is correct
- If permission denied: the user's token may lack required scopes (`api` scope recommended)
- If rate limited: wait and retry, or reduce request frequency

## Example Workflow: Create an Issue from a User Story

```
1. User asks: "Create a GitLab issue for the parser refactoring"
2. You:
   a. Read env.config to get GITLAB_HOST and GITLAB_PROJECT
   b. (Optional) List existing issues to check for duplicates:
      glab issue list --repo <GITLAB_HOST>/<GITLAB_PROJECT> --search "parser refactoring" --output json
   c. Compose the issue content
   d. Show the user the exact create command:
      glab issue create --repo <GITLAB_HOST>/<GITLAB_PROJECT> \
        --title "Refactor parser module" \
        --description "## Description\n\nRefactor the parser to..." \
        --label "refactor"
   e. Ask user for approval via ask_followup_question
   f. On approval: execute the command
   g. Report the created issue URL back to the user
```

## Best Practices

- **Always read `env.config` first** to get the correct host and project path
- **Use `--output json`** for list/view commands to get parseable output
- **Check for duplicates** before creating issues or MRs
- **Add the `ai-assist` label** to MRs created by this agent
- **Never skip user approval** for mutating operations — even if the user previously said "go ahead"
