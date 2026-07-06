# Load Projects On Demand For Roslyn LSP

Status: Draft

## Summary

This document proposes a Roslyn LSP feature that defers full project loading until a request needs project-backed semantics, while still performing a cheap upfront scan of workspace folders for `.csproj` files. This should be the default behavior for the standalone language server.

The design is intentionally different from the current standalone LSP behavior, which either:

- eagerly auto-loads a solution or all discovered projects at startup, or
- leaves unopened projects invisible and serves files through the miscellaneous-files workspace.

The proposal is also intentionally different from a pure OmniSharp clone. Instead of starting from a fully empty project model and discovering projects only when a file is touched, Roslyn will build a lightweight project index up front, then use that index to materialize projects on demand.

This is a hybrid approach:

- startup work is bounded to file-system enumeration and cache updates,
- first-touch latency is reduced because candidate projects are already known,
- Roslyn can preserve richer project-context behavior than a pure directory walk,
- the implementation can reuse existing Roslyn LSP building blocks such as primordial projects, miscellaneous-file fallback, and explicit project loading,
- eager project loading remains available as an opt-in path for `AutoLoadProjects` and explicit client-open scenarios.

## Motivation

Large repositories pay a meaningful startup cost when Roslyn LSP discovers and fully loads every `.csproj` under the workspace or every project in a solution. Users who open a large monorepo often need only one or two projects during a session.

OmniSharp's `LoadProjectsOnDemand` feature addresses this by delaying project loads until a file is actually used. That improves startup time and reduces initial memory use, but its discovery model is tightly coupled to OmniSharp's workspace and request pipeline.

Roslyn LSP already has several primitives that make a different, more Roslyn-native design attractive:

- explicit `solution/open` and `project/open` loading paths,
- a host workspace and a miscellaneous-files workspace,
- a primordial-to-fully-loaded project transition in `LanguageServerProjectLoader`,
- an existing file-system traversal and caching implementation for file-based-app discovery.

The goal of this proposal is to combine those primitives into an implementation that improves startup scalability without regressing correctness for project-backed features.

## Goals

- Avoid full design-time loading of all projects at server startup.
- Perform an upfront scan of workspace folders for `.csproj` files.
- Reuse as much of the existing file-based-app discovery infrastructure as is practical.
- Load projects when a document or feature first requires project-backed semantics.
- Preserve current behavior for transitive project references once a root project is loaded.
- Keep loose-file and miscellaneous-file behavior working for files that do not belong to any discovered project.
- Fit the existing Roslyn LSP architecture for standalone LSP without requiring immediate Dev Kit changes.

## Non-Goals

- Replacing the Dev Kit project system.
- Fully solving multi-solution and multi-project ambiguity in the first iteration.
- Eagerly determining exact document membership for every source file at startup.
- Loading analyzers, generators, or full compilation state during the startup scan.
- Changing the file-based-app classification algorithm.

## Current State

### Standalone Roslyn LSP

At startup, `AutoLoadProjectsInitializer` may:

- load a configured solution,
- load a single solution found at the workspace root, or
- recursively enumerate `.csproj` files in workspace folders and fully load them.

This is simple, but it scales poorly in large repositories because project enumeration is followed by design-time loading of every discovered project.

### File-Based Apps

`FileBasedProgramsEntryPointDiscovery` already walks workspace folders using a custom file-system visitor. Important characteristics of that implementation:

- it skips well-known ignored directories,
- it maintains a persistent discovery cache,
- it records directories containing `.csproj`,
- it avoids descending into a subtree once a `.csproj` is found,
- it is optimized for repeated startup scans.

This is the closest existing Roslyn LSP infrastructure to the desired upfront `.csproj` scan.

### Document Resolution

When an LSP request needs a document, `LspWorkspaceManager` searches the registered workspaces. If no project-backed document is found, Roslyn may serve the file from the miscellaneous-files workspace instead.

That fallback is useful today, but it means a file that really belongs to a project may initially be treated as loose until the project is loaded through some other path.

## Proposed User Experience

When the feature is enabled:

1. On initialization, Roslyn scans workspace folders for `.csproj` files and builds a lightweight discovery index.
2. Roslyn does not run design-time builds for those projects at startup.
3. If the user opens or requests a file that belongs to one of the indexed projects, Roslyn loads that project on demand.
4. Roslyn also loads transitive project references using the existing project-loading pipeline.
5. Once a project is loaded, project-backed features behave as they do today.
6. If no indexed project plausibly owns the file, Roslyn continues to use miscellaneous-file behavior.

The expected tradeoff is:

- much lower startup cost,
- slightly higher latency on the first project-backed request for a file in an unloaded project,
- significantly better first-touch behavior than pure reactive discovery because the project candidates are already indexed.

## Configuration

Add a new client-side setting, conceptually:

- `dotnet.loadProjectsOnDemand`

Behavior:

- `true` by default.
- When enabled, standalone LSP uses the hybrid design described here.
- `AutoLoadProjects` remains an explicit eager-loading override for scenarios that want a preloaded project set at startup.

Compatibility:

- If `AutoLoadProjects` is enabled, the projects loaded eagerly at startup stay loaded, but on-demand loading remains active for files outside that eagerly loaded set.
- If the client explicitly opens a solution or project, those projects become part of the loaded set, but files outside that set should still be eligible for on-demand loading.

Open question:

- none; this setting is client-driven and defaults to enabled.

## Design

### Overview

Introduce a new startup-time discovery component and a new first-touch load trigger:

- `WorkspaceProjectDiscoveryService`
- `OnDemandProjectLoader`

The design separates three stages:

1. Discovery: find `.csproj` files cheaply at startup.
2. Resolution: map a requested file to one or more candidate projects.
3. Materialization: load the chosen project through the existing `LanguageServerProjectSystem` pipeline.

### 1. Discovery Stage

#### Responsibilities

The discovery service will:

- read workspace-folder paths during LSP initialization,
- walk each workspace folder,
- collect discovered `.csproj` files,
- persist a cache to accelerate future startups,
- expose lookup APIs for later resolution.

#### Reuse of File-Based-App Discovery

The preferred implementation is to piggy-back on the traversal pattern from `FileBasedProgramsEntryPointDiscovery` rather than duplicate a separate recursive enumerator.

The strongest reuse opportunities are:

- directory ignore rules,
- timestamp-based cache invalidation,
- the `FileSystemEnumerator`-based visitor,
- persistent per-workspace-folder cache files,
- the existing concept of `DirectoriesContainingCsproj`.

There are two ways to do this.

Option A: Refactor the common traversal into shared infrastructure.

- Extract a shared workspace-folder scanner used by both file-based-app discovery and project discovery.
- Allow each consumer to plug in file-type-specific behavior.

Option B: Leave the current file-based-app implementation mostly intact and build a second discovery service that copies the traversal pattern.

- Faster to prototype.
- Higher long-term maintenance cost.

Recommendation:

- Start with a small shared utility if the extraction is local and mechanical.
- Do not force a large abstraction if it delays the feature substantially.

#### Discovery Output

The cache should store at least:

- workspace folder path,
- last successful walk time,
- sorted list of discovered `.csproj` paths,
- sorted list of directories containing `.csproj`.

Optional future additions:

- solution paths,
- solution-to-project membership,
- last known project GUID from solution parsing,
- lightweight ownership hints for files previously resolved to a project.

### 2. Resolution Stage

When a request references a file, Roslyn needs to decide whether to stay in miscellaneous-files mode or load a project.

#### Trigger Points

The first version should trigger resolution when a request needs a project-backed document and no loaded workspace currently contains that file.

The most natural hook is the document lookup path in `LspWorkspaceManager`:

- if a requested document is not found in loaded workspaces,
- and the URI is a local file path,
- ask the discovery service for candidate projects,
- synchronously or asynchronously initiate project load,
- retry document resolution before falling back to miscellaneous files.

This keeps the behavior centered around the existing document-resolution path rather than spreading feature-specific checks across many request handlers.

#### Candidate Selection

The first iteration should use simple, explainable heuristics:

1. Walk upward from the requested file's directory.
2. At each level, check whether the directory is known to contain one or more indexed `.csproj` files.
3. Prefer the nearest containing directory.
4. If multiple projects exist in that directory, queue them all or apply a deterministic tie-breaker.

Why this works:

- it matches common SDK-style repository layouts,
- it mirrors OmniSharp's intuition without requiring runtime directory enumeration,
- it uses the prebuilt index instead of scanning the file system again.

Later improvements may incorporate:

- solution membership,
- already-open project context,
- source include/exclude evaluation,
- project name or path affinity from recent resolutions.

#### Ambiguity Strategy

In repositories with linked files or multiple projects per directory, file ownership may be ambiguous.

First iteration behavior:

- if one candidate project is found, load it,
- if multiple candidate projects are found in the nearest directory, load all of them,
- let normal Roslyn project-context selection resolve the document after load,
- if none of the loaded projects contains the file, fall back to miscellaneous files.

This is intentionally conservative. Loading a small set of sibling projects is acceptable because it is still much cheaper than loading the entire repository.

### 3. Materialization Stage

Once candidate projects are selected, Roslyn should load them through the existing `LanguageServerProjectSystem` pipeline.

#### Loading Flow

The on-demand feature should not invent a second project loader. Instead it should:

- call into `LanguageServerProjectSystem.OpenProjectsAsync(...)` or a narrower equivalent,
- rely on existing batching and telemetry in `LanguageServerProjectLoader`,
- reuse existing transitive project-reference loading performed during project load.

This keeps the expensive part of the feature in the same code path used by explicit project loading today.

#### Primordial Experience

Roslyn already supports a primordial-project model in `LanguageServerProjectLoader`, though the standard host project system currently goes straight to tracked loaded targets for explicit project loads.

There are two viable materialization strategies.

Strategy A: Load-on-demand waits for the target project load to complete before retrying document resolution.

- simpler,
- less new workspace-state complexity,
- higher first-touch latency.

Strategy B: Introduce a host-workspace primordial project for on-demand resolution misses.

- immediate document availability,
- lower perceived latency,
- more implementation complexity,
- requires careful transition from miscellaneous or unresolved state to real host project state.

Recommendation:

- implement Strategy A first,
- reserve Strategy B for a later optimization if first-touch latency proves unacceptable.

The main value of this proposal comes from the startup-time savings and indexed resolution, not from immediate primordial host projects.

## Interaction With File-Based Apps

This feature must coexist cleanly with file-based-app discovery.

Desired behavior:

- the `.csproj` discovery scan should not regress file-based-app discovery,
- file-based-app logic should continue to avoid treating files inside a normal project cone as loose file-based apps,
- both features should use the same ignored-directory policy and, if practical, a compatible cache layout.

Recommended implementation direction:

- keep file-based-app discovery as the owner of `.cs` entry-point classification,
- let the new project discovery service own `.csproj` indexing,
- share traversal helpers and cache conventions where it is easy and low-risk.

We should avoid coupling these features so tightly that changes to file-based-app heuristics block project-discovery evolution.

## Detailed Behavior

### Initialization

When `dotnet.loadProjectsOnDemand` is enabled:

1. Do not run the eager auto-load path.
2. Start background discovery of `.csproj` files for each workspace folder.
3. Allow LSP initialization to complete without waiting for project loads.
4. If a request arrives before discovery finishes, either:
   - await discovery for the relevant workspace folder, or
   - fall back to a targeted synchronous probe for that file's directory chain.

Recommendation:

- discovery should start immediately at initialization,
- requests should be allowed to await per-folder discovery completion when needed.

### Document Open

`didOpen` should continue to track LSP text immediately.

On-demand project loading should not be triggered merely because a document was opened, unless we explicitly decide to prewarm project load on open.

Recommendation for first iteration:

- keep `didOpen` cheap,
- trigger project loading when the first project-backed request arrives.

Possible future optimization:

- opportunistically begin loading the owning project in the background after `didOpen` for a local file that has a single strong candidate project.

### Project-Backed Requests

For requests such as completion, hover, go to definition, find references, rename, code actions, or diagnostics:

1. Attempt normal document lookup.
2. If the file is already in a loaded project, proceed normally.
3. If not found and the file is eligible for on-demand loading, resolve candidate projects from the discovery index.
4. Load the candidate project set.
5. Retry document lookup.
6. If still unresolved, continue with miscellaneous-file behavior or a no-document result, as appropriate.

### Workspace-Wide Requests

Workspace-wide requests such as workspace symbols or whole-workspace diagnostics should not automatically load every indexed project just because the feature is enabled.

First iteration behavior:

- operate only on the currently loaded projects.

Rationale:

- loading all indexed projects on the first workspace-wide request would defeat the feature's primary goal.

## Caching

The discovery cache should follow the same general model as the file-based-app cache:

- one cache directory per workspace folder,
- stable sorted output for ease of inspection and binary search,
- timestamp-based incremental invalidation,
- best-effort writes with graceful fallback when cache I/O fails.

The cache does not need to be perfectly precise to be useful. False positives are acceptable if they only cause Roslyn to consider or load a nearby project. False negatives should be minimized because they lead to unnecessary miscellaneous-file fallback.

## Telemetry

Add telemetry for:

- discovery duration per workspace folder,
- number of `.csproj` files indexed,
- number of directories containing `.csproj`,
- number of on-demand load attempts,
- number of successful document resolutions after on-demand load,
- first-touch load latency,
- count of ambiguity cases,
- count of fallbacks to miscellaneous files after attempted on-demand load.

This telemetry is important because the feature's success criteria are about startup cost, first-touch latency, and fallback correctness.

## Error Handling

- Discovery failures should be non-fatal and should log diagnostics.
- Corrupt cache files should be discarded.
- Project-load failures should use existing project-load logging and user messaging.
- If on-demand loading fails for a given file, Roslyn should still try to provide best-effort miscellaneous-file behavior.

## Testing Plan

### Unit Tests

- Discovery finds `.csproj` files under workspace folders.
- Discovery skips ignored directories.
- Discovery cache is reused across repeated scans.
- Resolution selects the nearest indexed project directory.
- Resolution loads multiple sibling projects when ownership is ambiguous.
- Resolution falls back to miscellaneous files when no project candidate is found.

### Integration Tests

- Opening a workspace with many projects does not eagerly load them when the feature is enabled.
- First project-backed request for a file loads the owning project.
- Loading one project does not load unrelated projects.
- Project references are loaded transitively.
- Workspace-wide requests operate over only loaded projects.
- File-based-app discovery still works in workspaces that contain both `.csproj` projects and loose file-based apps.

### Performance Tests

- compare startup time against eager auto-load in large repositories,
- compare memory after initialization,
- measure first-touch latency for unloaded projects,
- measure repeated startup scans with a warm discovery cache.

## Rollout Plan

Phase 1:

- standalone LSP only,
- discovery index plus on-demand host project loading,
- conservative ambiguity behavior,
- no Dev Kit integration changes.
- `didOpen` prewarm,

Phase 2:

- better candidate selection heuristics,
- optional lightweight host primordial projects if latency warrants it.

Phase 3:

- evaluate whether similar indexing concepts should be surfaced to the Dev Kit project system through a separate contract.

## Recommendation

Implement this feature as a hybrid index-and-materialize design for standalone Roslyn LSP.

- Build a lightweight `.csproj` index at startup by reusing the file-based-app discovery traversal model.
- Trigger project loads from `didOpen` and from project-backed document requests when a file is not already covered by a loaded project.
- Load the selected project set through the existing `LanguageServerProjectSystem` pipeline.
- If multiple sibling projects exist in the nearest indexed directory, load them all.
- Keep workspace-wide features scoped to loaded projects.
- Do not index `.sln` or `.slnx` in v1.
- Use a client-side `dotnet.loadProjectsOnDemand` setting that defaults to enabled.
- Defer host-workspace primordial projects and Dev Kit integration to later iterations.

This gives Roslyn most of the startup and memory wins that motivated OmniSharp's feature, while staying aligned with Roslyn LSP's current architecture and existing discovery infrastructure.