# Load Projects On Demand For Roslyn LSP

Status: Draft

## Summary

The standalone Roslyn LSP server can defer project loading until a file is opened or a document request prefers project-backed semantics. The design has four key properties:

- project discovery is demand-driven and walks only the requested file's ancestors,
- opening a document starts loading without waiting for a design-time build,
- requests can wait for a preferred level of project context without blocking the global LSP request queue, and
- explicit, eager, and on-demand callers share one canonical load operation per project path.

The feature is enabled by default through `dotnet_load_on_demand`. It is disabled when Dev Kit owns the project system.

## Motivation

The standalone server can currently load a solution or recursively discover and load projects at startup. That scales poorly for large repositories when a session uses only a small subset of their projects.

Deferring design-time builds reduces startup work and memory. The first request that needs an unloaded project may take longer, but unrelated LSP requests must remain responsive while that load is in progress.

## Goals

- Avoid recursively scanning workspace folders at startup.
- Avoid loading unrelated projects before they are needed.
- Start likely project loads when documents are opened.
- Give document handlers a declarative way to request project-context completeness.
- Preserve request ordering and request-time tracked text.
- Avoid blocking unrelated requests while project loading is in progress.
- Deduplicate project loading across every loading entry point.
- Load transitive project references when a request prefers project-and-dependency context.
- Preserve miscellaneous-file fallback when discovery or loading cannot provide the preferred context.
- Organize document resolution in `LspWorkspaceManager` as a small provider pipeline.

## Non-Goals

- Replacing the Dev Kit project system.
- Discovering projects outside configured workspace folders.
- Proving exact source-file membership before evaluating a project.
- Discovering reverse project dependencies.
- Loading every project under a workspace folder for ordinary workspace requests.
- Adding project-load prioritization in this change. The work queue is being enhanced separately.
- Adding a persistent project-discovery cache.
- Changing file-based-program discovery.
- Adding new user-facing project-load failure notifications.

## Configuration And Applicability

`LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand` uses the `dotnet_load_on_demand` option and defaults to `true`.

The option is checked centrally when an on-demand operation is initiated:

- when enabled, document open and preferred-context requests may discover and load projects,
- when disabled, callers immediately continue with the currently available context,
- explicit project and solution loading are unaffected, and
- Dev Kit disables the feature even when the option is enabled.

Automatic or explicit loading does not disable on-demand behavior. On-demand loading joins projects already being loaded and fills gaps outside the explicitly loaded set.

## Context Preference Model

`ISolutionRequiredHandler` exposes an `LspSolutionContextPreference` property. The initial enum contains:

- `NoPreference`
- `Project`
- `ProjectAndDependencies`
- `Workspace`

Reverse-dependant completeness is not represented until it can be implemented correctly.

The interface default is `ProjectAndDependencies`. Context creation normalizes the preference to `NoPreference` when the handler does not require an LSP solution or the request has no document identifier.

Mutating handlers must explicitly declare `NoPreference`. Context creation validates this requirement because mutating handlers cannot wait outside serialized execution safely. `didOpen` is one such handler.

Individual handlers can later use less or more expensive preferences based on correctness and latency data. The initial implementation intentionally establishes one default rather than tuning every handler in the same change.

### Preference Semantics

`NoPreference` uses the context immediately available when the request is ordered.

`Project` waits for at least one evaluated target for the containing project to be committed to the host workspace. A primordial project does not satisfy this preference.

`ProjectAndDependencies` waits for the selected project and its transitive project-reference closure to settle. Supported absolute project references can be loaded even when they are outside the workspace folder that bounded initial discovery.

`Workspace` waits for a snapshot of projects associated with explicit solution and project open operations known when the request is ordered. It does not discover every project under workspace folders. A document-scoped workspace request still selects its requested document after waiting.

Preferences mean "wait, then fall back," not "wait or fail." Project-load failures use existing logging and toast policy, after which the request receives the best context available.

### Achieved Completeness

`RequestContext` exposes a separate `LspSolutionContextCompleteness` property with these values:

- `NotEvaluated`
- `None`
- `Miscellaneous`
- `Project`
- `ProjectAndDependencies`
- `Workspace`

`NoPreference` produces `NotEvaluated`; the framework does not inspect or guarantee the actual context in that case.

For preferred requests, `None` means no document context was available and `Miscellaneous` means a valid loose-file context was available. Project completeness is relative to the project selected for the requested document. Failure to load an unselected ambiguous candidate does not lower the selected document's achieved completeness.

## Demand-Driven Project Discovery

`WorkspaceProjectDiscoveryService` remains a per-LSP-server service, but it no longer performs startup discovery. It owns:

- configured workspace-folder boundaries,
- subscriptions to workspace-folder changes,
- nearest-ancestor project lookup,
- positive per-directory candidate caches, and
- logical file-watcher contexts for positive project subtrees.

`OnDemandProjectLoader` consumes the concrete discovery service. Protocol-layer callers see only `IOnDemandProjectLoader`.

### Initialization And Paths

During initialization, the discovery service records workspace-folder paths and subscribes to `IInitializeManager.WorkspaceFoldersChanged`. It performs no filesystem enumeration.

When a workspace folder is removed, the service removes its cached candidates and disposes logical watcher contexts beneath that root. Already loaded projects remain owned by the project-system lifecycle.

Path keys use ordinal case-insensitive identity on every platform, independent of the underlying filesystem's case sensitivity. Discovery uses normalized lexical full paths and does not resolve symbolic links.

### Ancestor Lookup

For a local file URI:

1. Find the deepest configured workspace folder containing the file.
2. Start at the file's containing directory.
3. Enumerate project files directly in that directory.
4. If supported project files are found, return all of them in ordinal path order.
5. Otherwise continue with the parent directory.
6. Stop after inspecting the selected workspace root.

Files outside every configured workspace folder are not eligible for discovery.

The first project-bearing directory is authoritative. If its projects evaluate successfully but do not contain the requested file, the operation falls back to miscellaneous behavior rather than loading projects from higher ancestors.

Project-file recognition is delegated to `LanguageServerProjectSystem`, which owns `ProjectFileExtensionRegistry`, and is filtered to languages supported by the current LSP server.

Directory enumeration is synchronous because each operation examines only one directory at a time. The shared discovery operation itself is scheduled outside serialized LSP queue execution, so filesystem I/O does not block request ordering.

Enumeration failures are logged and treated as an empty directory for that lookup. Discovery continues with the parent directory.

### Cache And File Watching

The service retains positive per-directory results for the server lifetime. Empty directories are not retained.

Each positive directory gets a logical recursive project-file watch. The underlying `IFileChangeWatcher` implementations consolidate watcher resources where possible, so the discovery service does not add custom watcher consolidation.

Watcher behavior is:

- newly created supported project files populate the matching cached directory,
- deleted project files are removed from the cached set,
- an empty positive entry is removed,
- modified project files remain candidates and are handled by existing project reload infrastructure, and
- existing descendants are not scanned when a positive directory is first discovered.

Watcher events that race enumeration are merged with the enumerated result, and project-file existence is validated before candidates are returned.

Concurrent lookups coalesce enumeration per directory. Different directories can be inspected concurrently.

## Document Open

`didOpen` remains a mutating handler with `NoPreference`.

`RequestContext.StartTrackingAsync` is the single production document-open path. It:

1. awaits normal document tracking so the client text is recorded,
2. asks the optional `IOnDemandProjectLoader` to start a shared load operation, and
3. returns without waiting for discovery or project evaluation.

The operation is tracked through Roslyn's asynchronous-operation infrastructure. Once initiated, it uses server-lifetime cancellation rather than the `didOpen` request token. Canceling one notification or request does not cancel work shared with later requests.

A preferred document request can independently initiate the same operation when `didOpen` did not occur.

Shared pre-discovery operations are keyed by normalized lexical document path plus the selected workspace root. Requests share discovery and project loading, but each request constructs its own final `RequestContext` because tracked text, version, and project-context selection are request-specific.

## Canonical Project Loading

`LanguageServerProjectLoader` owns canonical per-project in-flight state. A normalized lexical project path identifies one load operation across on-demand, automatic, explicit project, and explicit solution loading.

The existing `_gate` protects this state. Queue-level deduplication is not the sole correctness mechanism because it only deduplicates pending items within a batch.

### Project Load Handle

A begin-or-join operation returns an internal `ProjectLoadHandle`. The handle is internal to HostWorkspace and exposes structured completion for one project.

An individual result contains a final status such as loaded, failed, unsupported, or unloaded, plus the `ProjectId`s of loaded target-framework projects committed to the workspace.

Expected evaluation failures are caught at the per-project reload boundary, logged through existing mechanisms, and converted to a structured result. One failed project must not fail the entire batch or strand unrelated handles.

A project is complete only after its evaluated target projects have been applied to the workspace. Already loaded projects return completed handles. Primordial projects remain pending until canonical targets are committed.

Outstanding handles complete with an unloaded status when their project is unloaded. Server shutdown cancels outstanding handles with the project-loader lifetime token.

Reload freshness is outside this design. A handle requested for an already committed project completes from current state even if a reload is queued or active.

### Joining Metadata And Progress

If on-demand loading queues a project without a solution GUID and an explicit solution load joins before evaluation starts, the pending operation adopts the GUID. If different GUIDs are supplied for the same normalized path, the first wins and the conflict is logged.

Progress belongs to each explicit bulk operation, not to a single `ProjectToLoad`. Every caller advances its own progress when the shared handle settles.

`OpenProjectsAsync` awaits only handles for its requested projects. `OpenSolutionAsync` awaits only handles for projects from that solution. Neither operation waits for unrelated projects that happen to enter the same batching snapshot.

Project-initialization-complete notifications remain associated with explicit bulk project and solution operations. On-demand operations do not send global initialization-complete notifications.

### Dependency Closure

`ProjectAndDependencies` expands the project-reference graph after each project evaluation:

1. Await every candidate root project.
2. Read normalized supported absolute project-reference paths from the loader's private completed state.
3. Begin or join canonical handles for unseen references.
4. Repeat until the transitive closure settles.

Each document operation maintains a visited set and shares canonical handles, which makes cycles and overlapping candidate closures safe.

All nearest candidate roots and their closures are allowed to settle before final document selection. Completeness is then evaluated relative to the selected document project:

- successful selected root and closure produces `ProjectAndDependencies`,
- successful selected root with a failed dependency produces `Project`, and
- failure to obtain a project-backed selected document falls back to `Miscellaneous` or `None`.

### Prioritization

This change preserves the existing `AsyncBatchingWorkQueue` scheduling and project-load parallelism. It does not preempt, promote, or reserve capacity for on-demand work.

A preferred-context request can therefore wait behind a large existing project batch. A separate work-queue enhancement can add prioritization later without changing the handle or context-preference contracts.

## Non-Blocking Request Context Preparation

Request-context creation must preserve LSP ordering without awaiting long-running project work in the serialized queue.

`AbstractRequestContextFactory` gains an optional generic deferred-preparation capability. Serialized creation returns the immediate context built using current behavior plus an optional async callback that produces the context used for dispatch. Other language-server consumers return no callback and retain existing behavior.

For a preferred Roslyn document request, serialized creation:

1. captures tracked document text as of the request's position in the queue,
2. captures client capabilities, method, and requested project-context identifier,
3. builds the immediate fallback context as today,
4. atomically gets or creates the shared on-demand operation, and
5. returns without performing ancestor I/O or awaiting project work.

For non-mutating requests, dispatch awaits deferred preparation outside serialized queue processing. Later mutating and non-mutating requests can therefore start while project loading remains blocked.

After the shared operation settles, preparation always re-resolves the document against the latest workspace. The resulting solution is forked with the request-time tracked text. This exposes newly loaded project structure without exposing document edits ordered after the request.

The original project-context identifier is passed through normal `FindDocumentInProjectContext` selection. When multiple candidates contain the file, existing project-context behavior remains authoritative.

If preparation fails unexpectedly, Roslyn reports and logs the exception and dispatches with the immediate context. If the request is canceled while waiting, dispatch is canceled and the handler is not invoked; shared project loading continues.

## Document Context Provider Pipeline

`LspWorkspaceManager.GetLspDocumentInfoAsync` resolves existing document contexts through an explicitly ordered, closed provider pipeline. On-demand loading is not a provider because it occurs during deferred context preparation.

The internal Protocol abstraction is `ILspDocumentContextProvider`. A provider receives a lookup context and returns either `(Workspace, Solution, TextDocument)` or no result so the next provider can run.

The factory constructs an immutable provider array in this order:

1. registered workspaces,
2. miscellaneous fallback.

The registered-workspaces provider searches snapshots supplied by the manager's existing registration and fork cache. It is read-only.

The miscellaneous provider may add an open tracked document to the existing miscellaneous-files provider. It receives the captured tracked-document map through the lookup context rather than reading manager state directly.

The manager owns final success/failure telemetry and cross-provider cleanup. When a registered non-miscellaneous document wins, the manager removes any stale miscellaneous copy.

Unexpected non-cancellation provider exceptions are logged and resolution continues with the next provider. Workspace-level `GetLspSolutionInfoAsync` remains a direct host-workspace operation and does not use this pipeline.

The provider pipeline is internal and explicitly constructed. It does not add MEF ordering metadata until a real external extension requirement exists.

## Interaction With File-Based Programs

This design does not change file-based-program discovery or extract a shared recursive workspace walker. File-based-program behavior continues through the existing miscellaneous-files infrastructure.

Removing recursive project discovery also removes the reason for the current `WorkspaceFolderWalker` extraction. That refactor should not be part of this feature change.

## Error Handling

- Initialization performs no discovery I/O.
- Ancestor enumeration failures are logged and lookup continues upward.
- Project evaluation failures use existing logging, telemetry, and toast behavior.
- Preferred requests fall back to their immediate context when the preference cannot be achieved.
- No new LSP request errors or feature-specific user warnings are introduced.
- Request cancellation cancels only that waiter.
- Server shutdown cancels shared work through loader lifetime cancellation.

## Telemetry And Logging

The initial implementation reuses existing project-load telemetry. It adds diagnostic logs for discovery outcome, requested and achieved completeness, fallback reason, and wait duration.

No paths are added to telemetry. A dedicated telemetry schema for discovery and preference fallback is deferred until the behavior is validated.

## Testing Plan

### Discovery And Watcher Tests

- Initialization records workspace roots without enumerating the filesystem.
- Lookup selects the deepest containing workspace root and nearest project-bearing ancestor.
- Multiple supported projects are returned in ordinal order.
- Files outside workspace roots do not trigger discovery.
- Empty directories are not retained in the cache.
- Concurrent lookups coalesce per-directory enumeration.
- Enumeration errors log and continue upward.
- Case-insensitive path identity applies to workspace add/remove and duplicate handling on every platform.
- Workspace-folder removal drops matching cache and watcher state.
- Created and deleted project files update positive subtree caches.
- Enumeration racing a watcher event returns a validated merged result.

### Project Loading Tests

- On-demand, project-open, and solution-open callers share one evaluation and one canonical handle.
- Per-project completion occurs after workspace commit without waiting for unrelated batch work.
- Already loaded projects return completed handles.
- Transitive references, cycles, and overlapping closures settle correctly.
- Dependency failure lowers selected-document completeness from `ProjectAndDependencies` to `Project`.
- Unload completes outstanding handles as unloaded.
- Shutdown cancels outstanding handles.
- Dev Kit does not initiate standalone on-demand loading when the option is enabled.

### Request Queue And Context Tests

- A blocked preferred request does not prevent later mutating or non-mutating requests from starting.
- Prepared context uses request-time tracked text after a later `didChange`.
- Client project-context selection survives deferred preparation.
- Request cancellation prevents handler dispatch without canceling shared loading.
- Unexpected preparation failure falls back to the immediate context.
- Every achieved completeness value is reported correctly.

### Provider Tests

- Registered workspaces run before miscellaneous fallback.
- Project-context selection for linked documents is preserved.
- A project-backed result removes a stale miscellaneous document.

### Integration Validation

Integration validation is split across deterministic synthetic tests at the service boundaries:

- `WorkspaceProjectDiscoveryServiceTests` covers deep source paths, multiple sibling candidates, unsupported extensions, and dynamic workspace-folder changes.
- `OnDemandProjectLoaderTests` covers transitive and cyclic references, overlapping dependency graphs, and partial load failure.
- `LanguageServerProjectLoaderTests` covers unrelated blocked loads plus unsupported and failing project evaluations.
- `HandlerTests.DeferredContextDoesNotBlockLaterRequestsAndUsesRequestTimeText` holds preferred loading behind a gate, applies a later `didChange`, and runs later mutating and non-mutating requests before releasing the load.

The primary release criterion is no global LSP queue stall. The tests use explicit gates rather than fixed wall-clock performance assertions.

Required validation includes focused Protocol unit tests, LanguageServer unit tests, and builds for the touched language-server and protocol projects.

## Implementation And Review Plan

The existing PR is rewritten in place and kept as one PR with phased commits:

1. Add generic deferred request-context preparation and queue responsiveness tests.
2. Add canonical per-project handles and targeted explicit-operation waiting.
3. Replace startup discovery with demand-driven ancestor resolution and positive-subtree watching.
4. Add context preference, document-open initiation, dependency closure, and achieved completeness.
5. Refactor document lookup into the minimal provider pipeline, update tests, and finalize documentation.

The option remains enabled by default throughout the completed implementation. The design accepts first-touch latency from existing work-queue ordering until the separate prioritization enhancement is available.

## Deferred Work

- Tune preferences for individual handlers.
- Add project-load prioritization using the separately enhanced work queue.
- Design reverse-dependant discovery before adding a dependant preference value.
- Consider dedicated telemetry after initial validation.
- Consider broader provider extensibility only when another document-context source requires it.
- Evaluate first-touch latency before considering primordial host-project context as a preference level.