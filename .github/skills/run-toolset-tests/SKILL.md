---
name: run-toolset-tests
description: >-
  Run the razor-toolset-ci pipeline to validate the current branch against large
  third-party repositories (MudBlazor, OrchardCore, ASP.NET Core, etc.).
  Use when asked to run toolset tests, ecosystem tests, or third-party validation.
---

# Run Razor Toolset Tests

Pushes the current branch to `dotnet/roslyn` on GitHub and triggers the
[`razor-toolset-ci`](https://dev.azure.com/dnceng/internal/_build?definitionId=1270)
Azure DevOps pipeline (definition 1270, project `internal`) to build the Razor
compiler toolset and validate it against large third-party projects.

## Background

The pipeline lives in the [`razor-toolset-tests`](https://dev.azure.com/dnceng/internal/_git/razor-toolset-tests)
repo in `dnceng/internal`. It:

1. Builds `Microsoft.Net.Compilers.Razor.Toolset` from the merged Roslyn repo
2. Injects it (plus the matching Roslyn toolset) into 11 third-party projects
3. Builds each project to verify compatibility

Projects tested: MudBlazor, Havit.Blazor, Telerik Blazor UI, OrchardCore,
GrandNode2, Damselfly, Hawaso, dotnet-podcasts, and ASP.NET Core
(MVC, Razor Pages, Blazor components).

**Important:** When manually triggered, the pipeline checks out from the
`RazorGitHubRepo` resource (`github.com/dotnet/roslyn`). The commit must be
pushed to `dotnet/roslyn` — commits on forks or the internal ADO mirror are
not accessible for manual runs.

## Steps

### 1. Verify prerequisites

- Confirm we're in the merged Roslyn repository (look for `src/Razor/src/Compiler` or check git remotes).
- Get the current commit SHA: `git rev-parse HEAD`
- Get the current branch name: `git branch --show-current`
  (if HEAD is detached, use `git rev-parse --short HEAD` as the name)

### 2. Find or configure a remote for `dotnet/roslyn`

- Run `git remote -v` and look for a remote whose URL contains `dotnet/roslyn`.
- Common remote names: `upstream`, `dotnet`.
- If no such remote exists, add one:
  ```
  git remote add dotnet https://github.com/dotnet/roslyn.git
  ```
- Store the remote name for subsequent steps.

### 3. Push to GitHub

Push current HEAD to a test branch on `dotnet/roslyn`:

```
git push <remote> HEAD:refs/heads/toolset-test/<branch-name> --force
```

If the branch name is empty (detached HEAD), use the short SHA instead.

If the push fails with a permission error, inform the user they need push access
to `dotnet/roslyn`.

### 4. Trigger the pipeline

Use the `ado-pipelines_run_pipeline` MCP tool:

- `project`: `"internal"`
- `pipelineId`: `1270`
- `resources`:
  ```json
  {
    "pipelines": {},
    "repositories": {
      "RazorGitHubRepo": {
        "refName": "refs/heads/toolset-test/<branch-name>",
        "version": "<commit-sha>"
      }
    }
  }
  ```

If the MCP tool fails, fall back to the REST API via PowerShell:

```powershell
$body = @{
    resources = @{
        repositories = @{
            RazorGitHubRepo = @{
                refName = "refs/heads/toolset-test/<branch-name>"
                version = "<commit-sha>"
            }
        }
    }
} | ConvertTo-Json -Depth 5

az devops configure --defaults organization=https://dev.azure.com/dnceng project=internal
$result = az rest --method post `
    --uri "https://dev.azure.com/dnceng/internal/_apis/pipelines/1270/runs?api-version=7.1" `
    --body "$body" `
    --resource "499b84ac-1321-427f-aa17-267ca6975798" | ConvertFrom-Json
Write-Output "Run URL: https://dev.azure.com/dnceng/internal/_build/results?buildId=$($result.id)"
```

### 5. Report results

Show the user:
- The pipeline run URL (from the response, or construct from the build ID)
- The commit SHA and branch that was pushed
- A reminder to clean up the remote branch when done:
  ```
  git push <remote> --delete toolset-test/<branch-name>
  ```

## Notes

- All 11 projects run as parallel jobs — individual jobs can fail independently.
- The pipeline uses the `windows.vs2026.amd64` agent image.
- Pipeline definition: https://dev.azure.com/dnceng/internal/_build?definitionId=1270
- Pipeline source repo: https://dev.azure.com/dnceng/internal/_git/razor-toolset-tests
