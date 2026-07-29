# Load Projects On Demand For Roslyn LSP

Status: Implemented

## Summary

Roslyn LSP can defer project loading until a document lookup misses all currently loaded workspaces.

At startup, the server builds an in-memory index of `.csproj` files under each workspace folder. Later, when a file-backed request needs a project document and none is currently available, Roslyn resolves candidate projects from that index, opens those projects, and retries document resolution once.

## Scope

This behavior is implemented in the language server project system path.

Key components:

- `WorkspaceProjectDiscoveryService`
- `WorkspaceFolderWalker`
- `OnDemandProjectLoader`
- `LspWorkspaceManager`

## Configuration

On-demand loading is controlled by `LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand` (`dotnet_load_on_demand`).

- Default: `true`
- If disabled, no on-demand project load is attempted.

## Startup Discovery

During initialization, `WorkspaceProjectDiscoveryService`:

1. Reads workspace folders from `IInitializeManager`.
2. Indexes `.csproj` files per workspace folder.
3. Starts file watchers for `.csproj` changes in each workspace folder.
4. Subscribes to workspace-folder add/remove events and updates discovery state accordingly.

Discovery uses `WorkspaceFolderWalker.Walk(...)`.

Current walker behavior:

- ignores directories beginning with `.`
- ignores well-known directories: `artifacts`, `bin`, `obj`, `node_modules`
- records directories that contain one or more `.csproj`
- does not descend below a directory once that directory contains a `.csproj`

## Discovery Index Model

For each workspace folder, the index stores:

- project directory path
- list of `.csproj` files in that directory

The index is held in memory and updated by `.csproj` file-change notifications.

Current implementation does not persist this index to disk.

## Candidate Resolution

When resolving candidates for a file path:

1. Consider only workspace folders that contain the file path.
2. Prefer the deepest matching workspace folder.
3. Starting at the file's directory, walk up toward the workspace root.
4. Return the first directory that exists in the index.
5. Return all `.csproj` files from that directory.

This means ambiguity is handled by returning multiple sibling projects when they share the nearest indexed directory.

## Load Trigger And Retry

`LspWorkspaceManager.GetLspDocumentInfoAsync(...)` performs this sequence:

1. Try to find the document in all currently loaded LSP workspaces.
2. If not found, call `IOnDemandProjectLoader.TryLoadProjectsForDocumentAsync(...)`.
3. If that call initiated a load, refresh workspace solutions and retry document resolution once.
4. If still unresolved, continue normal fallback behavior (including miscellaneous-files handling for tracked open documents).

This keeps on-demand loading on the document-resolution path instead of per-feature request handlers.

## Project Materialization

`OnDemandProjectLoader`:

- checks `LoadProjectsOnDemand`
- requires a local `file:` URI
- asks `WorkspaceProjectDiscoveryService` for candidate projects
- deduplicates projects already loaded by this loader instance
- calls `LanguageServerProjectSystem.OpenProjectsAsync(...)` for new candidates

If no candidates are found (or all candidates were already loaded), no load is initiated.

## Interaction With Eager Auto-Load

`AutoLoadProjectsInitializer` remains a separate startup path driven by server configuration (`--autoLoadProjects`) and solution settings.

When eager auto-load is used, those projects are opened at startup. On-demand loading still applies to later document misses outside the already loaded set.

## Current Behavioral Boundaries

- On-demand loading is only attempted for local file URIs.
- Candidate selection is directory-based; it does not evaluate MSBuild include/exclude item membership before loading.
- The first resolution attempt can still miss; Roslyn retries after loading once and then falls back.
- Workspace-wide operations continue to operate on the set of currently loaded projects.
