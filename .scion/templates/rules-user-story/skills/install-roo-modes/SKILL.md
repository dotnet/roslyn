---
name: install-roo-modes
description: >-
  provides step-by-step instructions for installing or updating Roo mode
  definitions (.roomodes), Roo rules (.roo/rules-*), and GitHub/Copilot agent
  configurations (.github/agents/) from a configurable source repository into
  the local project. Supports overwrite and interactive merge modes. Does NOT
  modify skills.
---

# Install Roo Modes Skill

This skill synchronises **Roo mode definitions**, **Roo rules**, and **GitHub/Copilot agent configurations** from a central source repository into the current project. It depends on the **`gitlab-tools` skill** for all remote GitLab API access and uses the agent's built-in file tools to write locally.

**What it syncs:**

| Component | Source Path | Description |
|-----------|------------|-------------|
| Mode definitions | `.roomodes` | JSON file registering custom Roo modes (slug, name, role, groups) |
| Mode rules | `.roo/rules-{slug}/*.md` | Markdown instruction files for each mode |
| Global rules | `.roo/rules/*.md` | Shared rules loaded by all modes |
| Agent configs | `.github/agents/*.md` | GitHub Copilot agent definition files |

**What it does NOT sync:** Skills (`.agents/skills/`), `.claude/` directory, or any other files.

## Prerequisites

- **`gitlab-tools` skill** must be available in the project (`.agents/skills/gitlab-tools/`) with all its prerequisites met (`glab` CLI installed and authenticated)
- The source repository must be accessible via the GitLab API

## Environment Configuration

Read [`env.config`](env.config) before starting. It contains:

| Variable | Description | Default |
|----------|-------------|---------|
| `SOURCE_REPO` | Full URL of the source GitLab repository | `https://code.siemens.com/genai-google-gcp-motion-poc/tools-and-agents/agent-skills-hub` |
| `SOURCE_BRANCH` | Branch to fetch mode definitions from | `main` |

**Parsing the URL:** Extract `GITLAB_HOST` and `PROJECT_PATH` from `SOURCE_REPO`:
- Given `SOURCE_REPO=https://code.siemens.com/genai-google-gcp-motion-poc/tools-and-agents/agent-skills-hub`
- `GITLAB_HOST` = `https://code.siemens.com/`
- `PROJECT_PATH` = `genai-google-gcp-motion-poc/tools-and-agents/agent-skills-hub`

These are used to construct `--repo` flags for `glab` commands.

---

## Workflow

Follow these steps **in order**. Do NOT skip steps.

### Step 1: Read Configuration

1. Read `.agents/skills/install-roo-modes/env.config`
2. Parse `SOURCE_REPO` and `SOURCE_BRANCH`
3. Extract `GITLAB_HOST` and `PROJECT_PATH` from `SOURCE_REPO`

### Step 2: Load the gitlab-tools Skill (MANDATORY — DO NOT SKIP)

Before proceeding, you MUST load the `gitlab-tools` skill by reading `.agents/skills/gitlab-tools/SKILL.md` into context. All subsequent `glab` commands in this workflow follow that skill's command syntax and safety rules.

Also read `.agents/skills/gitlab-tools/env.config` if it exists, for GitLab connection details.

If the `gitlab-tools` skill is not available (`.agents/skills/gitlab-tools/SKILL.md` does not exist), tell the user to install it and **stop**.

### Step 3: Verify Source Repository Access

Run this command to verify the source repo is accessible:

```
glab repo view --repo <GITLAB_HOST>/<PROJECT_PATH> --output json
```

- **If this fails with an authentication error:** Tell the user to run `glab auth login --hostname <GITLAB_HOST>` and stop.
- **If this fails with a "not found" error:** Tell the user to verify `SOURCE_REPO` in `env.config` and stop.
- **If successful:** Proceed to Step 4.

### Step 4: Ask Sync Mode

Ask the user which sync mode to use:

> **How should I apply changes from the source repository?**
>
> 1. **Overwrite** — Replace all mode definitions, rules, and agent configs with the source versions. Any local customisations will be lost.
> 2. **Merge** — Add new items from the source, and for items that exist in both source and target, show you a diff so you can decide per item whether to accept the source version or keep yours. Local-only items are preserved.

Wait for the user's answer before proceeding.

### Step 5: Fetch Source File Listings

Fetch the file tree for each component from the source repository. Use `SOURCE_BRANCH` as the `ref` parameter.

#### 5a. Fetch .roomodes

```
glab api "projects/:id/repository/files/.roomodes/raw?ref=<SOURCE_BRANCH>" --repo <GITLAB_HOST>/<PROJECT_PATH>
```

This returns the raw JSON content of the source `.roomodes` file.

#### 5b. Fetch .roo/ file tree

```
glab api "projects/:id/repository/tree?path=.roo&ref=<SOURCE_BRANCH>&recursive=true&per_page=100" --repo <GITLAB_HOST>/<PROJECT_PATH>
```

This returns a JSON array of file/directory entries. Filter to keep only entries where `"type": "blob"` (files, not directories). These are the rule files to sync.

#### 5c. Fetch .github/agents/ file tree

```
glab api "projects/:id/repository/tree?path=.github/agents&ref=<SOURCE_BRANCH>&recursive=true&per_page=100" --repo <GITLAB_HOST>/<PROJECT_PATH>
```

Filter to keep only `"type": "blob"` entries.

#### Error Handling

- If any API call returns an error (e.g., 404), report which component could not be fetched and ask the user whether to continue with the remaining components or abort.
- A 404 on `.github/agents` is acceptable — the source repo may not have Copilot agent definitions.

### Step 6: Sync .roomodes

#### Overwrite Mode

1. Write the fetched source `.roomodes` content directly to the local `.roomodes` file using `write_to_file`.
2. Report: "✅ .roomodes overwritten with source version."

#### Merge Mode

1. Read the local `.roomodes` file (if it exists). If it doesn't exist, treat it as `{"customModes": []}`.
2. Parse both source and local `.roomodes` as JSON.
3. Compare modes by `slug`:

   **For each mode in the source:**
   - **If the slug does NOT exist in the local file:** Add it. Report: "➕ Added new mode: `<slug>`"
   - **If the slug exists in the local file AND the content is identical:** Skip it. Report: "⏭️ Mode unchanged: `<slug>`"
   - **If the slug exists in the local file AND the content differs:** Show the user both versions side by side and ask:
     > Mode `<slug>` differs between source and target.
     >
     > **Source version:**
     > ```json
     > <source mode JSON>
     > ```
     >
     > **Local version:**
     > ```json
     > <local mode JSON>
     > ```
     >
     > Accept source version? (yes/no)

     If yes: replace the local mode with the source version. Report: "✅ Updated mode: `<slug>`"
     If no: keep the local version. Report: "⏭️ Kept local mode: `<slug>`"

   **For each mode in the local file that is NOT in the source:**
   - Preserve it. Report: "🔒 Preserved local-only mode: `<slug>`"

4. Write the merged `.roomodes` JSON to the local file using `write_to_file`. Format the JSON with 2-space indentation.

### Step 7: Sync .roo/ Rules

> **⚠️ CRITICAL — Process files ONE AT A TIME.** Do NOT fetch all files before writing. For each file: fetch its content, write it (or compare it), report the result, then move on to the next file. This prevents context overflow when many files exist.

Iterate over the file list from Step 5b. For **each individual file**, perform steps 7a–7c before moving to the next file:

#### 7a. Fetch one file's content

```
glab api "projects/:id/repository/files/<URL_ENCODED_PATH>/raw?ref=<SOURCE_BRANCH>" --repo <GITLAB_HOST>/<PROJECT_PATH>
```

> **IMPORTANT:** URL-encode the file path. Replace `/` with `%2F` and `.` with `%2E` in the path parameter. For example:
> - `.roo/rules-task-agent/01-instructions.md` → `.roo%2Frules-task-agent%2F01-instructions%2Emd`

#### 7b. Write or compare (depending on sync mode)

**Overwrite Mode:**

1. Write the fetched content to the corresponding local path using `write_to_file`.
2. Report: "✅ Written: `<file_path>`"

**Merge Mode:**

1. Read the local file using `read_file`. If it does not exist, treat it as missing.
2. **If the local file does NOT exist:** Write the source file. Report: "➕ Added: `<file_path>`"
3. **If the local file exists AND content is identical:** Skip. Report: "⏭️ Unchanged: `<file_path>`"
4. **If the local file exists AND content differs:** Show the user both versions and ask:
   > File `<file_path>` differs between source and target.
   >
   > **Source version** (first 50 lines):
   > ```
   > <source content preview>
   > ```
   >
   > **Local version** (first 50 lines):
   > ```
   > <local content preview>
   > ```
   >
   > Accept source version? (yes/no)

   If yes: write the source version. Report: "✅ Updated: `<file_path>`"
   If no: keep local. Report: "⏭️ Kept local: `<file_path>`"

#### 7c. Move to next file

Repeat steps 7a–7b for the next file in the list. Do NOT batch multiple file fetches into a single step.

**Local-only files** (files in `.roo/` that exist locally but not in the source) are **always preserved** — never delete them.

### Step 8: Sync .github/agents/

Follow the same **one-file-at-a-time** procedure as Step 7, but for the `.github/agents/` directory.

> **⚠️ CRITICAL — Process files ONE AT A TIME**, exactly as in Step 7. Fetch → write/compare → report → next file.

For each file found in the source `.github/agents/` tree (from Step 5c):

1. Fetch the raw content of **one file** using the same API pattern (with URL-encoded path).
2. Write or compare it using the same overwrite or merge logic as Step 7.
3. Report the result, then move to the next file.

**Local-only agent files** are always preserved.

If Step 5c returned a 404 (source has no `.github/agents/`), skip this step entirely and report: "⏭️ Source repository has no .github/agents/ — skipping."

### Step 9: Summary Report

After all components are synced, present a summary to the user:

```
## Sync Complete

**Source:** <SOURCE_REPO> (branch: <SOURCE_BRANCH>)
**Mode:** Overwrite | Merge

### .roomodes
- Added: N new modes
- Updated: N modes
- Unchanged: N modes
- Preserved (local-only): N modes

### .roo/ rules
- Added: N files
- Updated: N files
- Unchanged: N files
- Preserved (local-only): not tracked

### .github/agents/
- Added: N files
- Updated: N files
- Unchanged: N files
- Preserved (local-only): not tracked

All changes are local only. Review the changes and commit when ready.
```

---

## Important Notes

- **All changes are local only.** The skill never commits or pushes. The user reviews changes before deciding to commit.
- **Skills are never touched.** The `.agents/skills/` directory is completely out of scope.
- **The `.claude/` directory is out of scope.** Do not modify it.
- **File-level merging for rules.** Each file in `.roo/rules-*/` is compared individually. Target-only files within a rules directory are preserved.
- **URL encoding is critical.** File paths in `glab api` calls must be URL-encoded. Use `%2F` for `/` and `%2E` for `.` in the `files/:file_path/raw` endpoint.
- **JSON formatting.** When writing `.roomodes`, always use 2-space indentation and a trailing newline to match the existing project convention.

## Error Handling

| Error | Action |
|-------|--------|
| `gitlab-tools` skill not found | Tell user to install the `gitlab-tools` skill into `.agents/skills/gitlab-tools/` |
| `glab` not found | Tell user to install it — see the `gitlab-tools` skill prerequisites |
| Authentication failed | Tell user to run `glab auth login --hostname <host>` |
| Source repo not found | Tell user to verify `SOURCE_REPO` in `env.config` |
| Individual file fetch fails | Report the specific file and error; continue with remaining files |
| Local `.roomodes` is invalid JSON | Report the parse error; ask user to fix it manually before retrying |
| `env.config` missing or unreadable | Tell user to copy `env.config.example` to `env.config` and configure it |
