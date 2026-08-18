---
name: merge-into-branch
description: Create a merge branch from a chosen base, merge a chosen source/upstream branch into it, resolve common Roslyn conflicts (`.xlf`, `.resx`, compiler codegen), and summarize the resolutions.
argument-hint: Which target/base branch should receive the merge, and what source/upstream ref should be merged?
---

# Merge Into Branch

Use this skill when you need to merge one branch into another in `dotnet/roslyn`; for example, merging `main` into a release or servicing branch, or merging any other source branch into a target branch.

> **IMPORTANT**: This workflow changes local git state by creating a branch, performing a merge, resolving conflicts, and producing a merge commit. Confirm the repo clone, base branch, upstream branch, and merge-branch name before making changes. If the working tree is dirty, stop and ask whether to continue in a clean worktree.

> **SKILL MAINTENANCE**: If the actual merge process differs from this skill or a new Roslyn-specific conflict pattern shows up, remind the user to update this skill so future merges benefit from the fix.

## When to Use This Skill

Use this skill when:
- Merging one branch into another branch
- Preparing a release, dev, or servicing branch to receive a merge
- Creating a one-off merge branch to prepare a PR
- Resolving Roslyn-specific localization or compiler-generation conflicts during a merge

Do **not** use this for a full branch snap workflow involving milestone movement, darc subscription changes, or release-train coordination. Use the `snap` skill for that.

## Workflow

### 1. Gather and confirm the merge inputs

Determine the following before changing git state:

1. **Repo path**: default to the current workspace if it matches the intended repo.
2. **Base branch**: the branch that should receive the merge.
3. **Source/upstream branch/ref**: the branch that should be merged in.
4. **Merge branch name**: default to `merge/<source>-into-<base>` with slashes sanitized as needed.
5. **PR title** (if a PR will be opened): use a consistent title of the form `Merge <source> into <target>`.
6. **Remote names**: determine which remote holds the base and upstream refs (`origin`, `upstream`, etc.).

Show the plan briefly and get explicit confirmation before creating the merge branch.

### 2. Create the merge branch and start the merge

Fetch fresh refs, create the merge branch from the desired base, then merge the upstream ref:

```bash
git fetch --all --prune
git switch -c <merge-branch> <base-remote>/<base-branch>
git merge --no-ff --no-commit <upstream-remote>/<upstream-branch>
```

If the merge completes without conflicts, keep the merge uncommitted so the summary can be shown to the user before the final merge commit is created.

### 3. Resolve conflicts using Roslyn-specific rules

Keep a running list of every conflicted file and how it was resolved. The final summary must include:
- file path,
- conflict type,
- resolution used,
- follow-up commands that were run,
- and a diff snippet or summary for the resolved change.

#### 3a. `.xlf` conflicts

For any `.xlf` conflict:

1. **Accept ours**:

   ```bash
   git checkout --ours -- <path-to-file>.xlf
   ```

2. Record which project the conflicted `.xlf` belongs to so it can be included in the final summary.
3. Run `dotnet msbuild /t:UpdateXlf <path-to-project>` for each affected project.
4. Re-stage the `.xlf` file and any other localization files updated by that target.

#### 3b. `.resx` conflicts

For any `.resx` conflict:

1. Manually merge the file so that **all strings from both branches are preserved**.
2. Do not drop keys that exist on only one side.
3. If both sides updated the same string, inspect the values and produce the merged result deliberately rather than picking one side blindly.

After the `.resx` merge, run:

```bash
dotnet msbuild /t:UpdateXlf <path-to-project>
```

Then re-stage the resulting resource and `.xlf` updates.

#### 3c. Changes under `src/Compilers`

If **any files under `src/Compilers` change** as part of the merge, regenerate compiler-generated code before finishing the merge:

```bash
dotnet run --file eng/generate-compiler-code.cs
```

Review and stage any generated changes before finishing the merge.

#### 3d. Other conflicts

Resolve any remaining conflicts carefully using the repo's existing code patterns. Avoid unrelated cleanup or opportunistic edits during the merge.

#### 3e. Validate the merge after conflict resolution

Before presenting the summary or creating the merge commit, run a validating build with analyzers enabled to make sure the merge did not introduce new issues:

```bash
./build.sh --runAnalyzers
```

On Windows, use:

```powershell
.\Build.cmd -runAnalyzers
```

If this build reports new issues caused by the merge, resolve them before continuing.

### 4. Present the summary and ask for confirmation

Once all conflicts are resolved and staged, present a concise but specific summary that includes:

1. Whether the merge had conflicts.
2. The list of files or projects involved.
3. The resolution for each conflict:
   - `.xlf` → accepted ours and updated XLFs
   - `.resx` → manually merged to preserve all strings and updated XLFs
   - `src/Compilers` → reran compiler code generation
   - other files → briefly describe the manual resolution
4. The result of the post-merge validation build/analyzer run.
5. A diff summary using commands such as:

```bash
git status --short
git --no-pager diff --cached --stat
git --no-pager diff --cached -- <path>
```

If there were **no conflicts**, say that plainly and note that the merge is ready to commit cleanly.

If this skill is being used from **CCA** and it creates a PR, add the same summary to a comment on that PR so reviewers can see exactly what conflicts were encountered and how they were resolved.

Ask explicitly whether to continue and create the merge commit.

### 5. Complete the merge after confirmation

Only after the user confirms:

```bash
git commit --no-edit
```

If a PR is then created from the merge branch:

1. Use the consistent title `Merge <source> into <target>`.
2. Include a short note in the PR body such as `Auto-generated by merge-into-branch skill.` so reviewers know the merge was mechanically produced.
3. If this workflow is running in CCA, post the summary as a PR comment as part of the handoff.

If the merge cannot be completed cleanly, stop and tell the user exactly what remains blocked.
