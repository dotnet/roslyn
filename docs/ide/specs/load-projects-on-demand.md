# Load Projects On Demand For Roslyn LSP

Status: Draft

## Summary

The standalone Roslyn LSP server can defer project loading until a file is opened or a request accesses its Roslyn document or solution context. The design has four key properties:

- project discovery is demand-driven and walks only the requested file's ancestors,
- every demand observes the current filesystem instead of retaining discovery results or file watchers,
- `RequestContext` starts loading without blocking request ordering and waits only when a handler accesses document or solution state, and
- explicit, eager, and on-demand callers share canonical project loads, while on-demand callers additionally share active root-and-dependency closure operations.

The feature is enabled by default through `dotnet_load_on_demand`. It is disabled when Dev Kit owns the project system.

## Motivation

The standalone server can load a solution or recursively discover and load projects at startup. That scales poorly for large repositories when a session uses only a small subset of their projects.

Deferring design-time builds reduces startup work and memory. The first request that accesses an unloaded project's context may take longer, but unrelated LSP requests remain responsive while loading is in progress.

## Goals

- Avoid recursively scanning workspace folders at startup.
- Avoid loading unrelated projects before they are needed.
- Start likely project loads for document-scoped messages, including document open.
- Give handlers one consistent async API for obtaining up-to-date document and solution context.
- Preserve request ordering and request-time tracked text.
- Avoid blocking unrelated requests while project loading is in progress.
- Deduplicate active project loading across every loading entry point.
- Load transitive supported project references for an on-demand root.
- Preserve miscellaneous-file fallback when discovery or loading cannot provide project context.
- Observe project-file creation and deletion on the next demand without maintaining discovery file watchers.
- Preserve registered-workspace lookup before miscellaneous-file fallback in `LspWorkspaceManager`.

## Non-Goals

- Replacing the Dev Kit project system.
- Discovering projects outside configured workspace folders.
- Proving exact source-file membership before evaluating a project.
- Discovering reverse project dependencies.
- Loading every project under a workspace folder for ordinary document requests.
- Adding project-load prioritization in this change. The work queue is being enhanced separately.
- Changing file-based-program discovery.
- Adding new user-facing project-load failure notifications.
- Removing canonical loaded-project and active-evaluation state from `LanguageServerProjectLoader`.

## Configuration And Applicability

`LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand` uses the `dotnet_load_on_demand` option and defaults to `true`.

The option is checked centrally when a document on-demand operation is initiated:

- when enabled, eligible local file messages whose documents are not already part of the host workspace may discover and load projects,
- when disabled, context access immediately uses the currently available state,
- explicit project and solution loading are unaffected, and
- Dev Kit disables standalone on-demand behavior even when the option is enabled.

Automatic or explicit loading does not disable on-demand behavior. On-demand loading joins projects already being loaded and fills gaps outside the explicitly loaded set.

## Demand-Driven Project Discovery

`WorkspaceProjectDiscoveryService` is a per-LSP-server service. It owns only nearest-ancestor project lookup. It does not perform startup discovery, retain candidate results, coalesce directory enumerations, or create project-discovery file watchers.

`WorkspaceFolderTracker` is the authoritative synchronized store for configured workspace-folder boundaries. Discovery reads one immutable snapshot from the tracker for each search.

`OnDemandProjectLoader` consumes the concrete discovery service. Protocol-layer callers see only `IOnDemandProjectLoader`.

### Initialization And Paths

During initialization, `InitializeHandler` adds the initial workspace folders to `WorkspaceFolderTracker`. The `workspace/didChangeWorkspaceFolders` handler removes and adds later changes directly on the tracker. Neither operation performs filesystem enumeration.

Adding or removing a local file workspace folder updates only the normalized folder set. Non-file workspace-folder URIs are ignored because ancestor project discovery requires a local filesystem path. Already loaded projects remain owned by the project-system lifecycle.

Path keys use Roslyn's platform path semantics: case-sensitive on Unix-like systems and case-insensitive on Windows. Discovery uses normalized lexical full paths and does not resolve symbolic links.

### Ancestor Lookup

For a local file URI:

1. Find the deepest configured workspace folder containing the file.
2. Start at the file's containing directory.
3. Enumerate supported project files directly in that directory.
4. If any are found, return all of them in ordinal path order.
5. Otherwise continue with the parent directory.
6. Stop after inspecting the selected workspace root.

Files outside every configured workspace folder are not eligible for discovery.

The first project-bearing directory is authoritative. If its projects evaluate successfully but do not contain the requested file, context resolution falls back to miscellaneous behavior rather than searching higher project-bearing ancestors.

Project-file recognition is delegated to `LanguageServerProjectSystem`, which owns `ProjectFileExtensionRegistry`, and is filtered to languages supported by the current LSP server.

Directory enumeration is synchronous. `OnDemandProjectLoader` schedules each complete ancestor walk on a background task, so filesystem I/O does not block serialized request ordering. Concurrent demands perform independent walks and therefore independently observe the current filesystem.

Normal enumeration failures are logged and treated as an empty directory. Discovery continues with the parent directory.

## Active Root-Closure Operations

After fresh discovery identifies candidate root projects, `OnDemandProjectLoader` creates or joins one active operation for each normalized root project path. Workspace folders bound discovery but do not affect closure loading once a project is found.

An operation:

1. Begins or joins the canonical load handle for its root project.
2. Waits until evaluated target projects are committed to the workspace.
3. Reads supported absolute project-reference paths from the completed project state.
4. Begins or joins canonical handles for unseen references.
5. Repeats until the transitive dependency closure settles.

A visited set makes cycles safe. Canonical project handles deduplicate overlapping roots and dependencies across on-demand, automatic, explicit project, and explicit solution loading.

All roots found in the nearest project-bearing directory and all their dependency closures settle before the document operation completes. Expected project failures settle through existing logging, telemetry, and toast policy. One failed project does not strand unrelated handles or fault the LSP request.

The root-closure entry exists only while work is active and is removed when it settles. A later demand performs a new filesystem walk and creates a new closure operation. Canonical project-loader state remains responsible for loaded projects and may complete the new operation immediately.

Request cancellation cancels only that request's wait. Discovery and project loading use server-lifetime cancellation because their work may be shared with later requests. Server shutdown cancels outstanding operations.

## Document Messages

`RequestContextFactory` requests an on-demand operation for every eligible local file message, including `didOpen`, even when the handler does not require a Roslyn solution. `OnDemandProjectLoader` first checks the host workspace's current solution and returns a completed operation when the document path is already represented there. Otherwise it schedules ancestor discovery without performing filesystem I/O on the serialized request queue.

`RequestContext.StartTrackingAsync` remains responsible only for recording client text. Loading is initiated centrally during context creation rather than by individual handlers.

Messages for documents already in the host workspace, non-file URIs, files outside workspace folders, disabled on-demand loading, and Dev Kit sessions receive a completed no-op operation.

## Async Request Context

Serialized `RequestContext` creation captures:

- the current workspace, solution, and text document when requested,
- tracked document text as of the request's position in the queue,
- the original text-document and project-context identifier,
- the aggregate on-demand operation for document requests, or a snapshot of explicit loads for workspace requests, and
- whether the handler mutates solution state.

Context creation returns without waiting for discovery or project evaluation. The handler is dispatched normally, outside serialized context creation.

### Accessors

Core handlers obtain solution-backed state through asynchronous accessors:

- `GetDocumentAsync`
- `GetRequiredDocumentAsync`
- `GetTextDocumentAsync`
- `GetRequiredTextDocumentAsync`
- `GetWorkspaceAsync`
- `GetRequiredWorkspaceAsync`
- `GetSolutionAsync`
- `GetRequiredSolutionAsync`

The first accessor on a non-mutating document context:

1. waits for all discovered roots and dependencies,
2. re-resolves the requested document against the latest workspace,
3. reapplies request-time tracked text, and
4. stores one resolved `(Workspace, Solution, TextDocument?)` tuple shared by every copy of that `RequestContext`.

This exposes newly loaded project structure without exposing document edits ordered after the request. Existing project-context selection remains authoritative when multiple projects contain the same file.

`GetSolutionAsync` uses the same resolved state as document access. If document lookup still returns no document, it returns the latest solution available after loading.

For workspace-scoped requests without a document identifier, context creation snapshots the active project-load operations represented by the canonical project-loader state. `GetSolutionAsync` waits only for that fixed snapshot and returns the latest solution; it does not discover every project in workspace folders or continually drain later loads.

Callers obtain the workspace from the asynchronously resolved document or solution so workspace identity remains paired with the resolved state. Workspace identity can change when document re-resolution moves a document from the miscellaneous workspace to the host workspace after project loading.

### Mutating Contexts

Mutating handlers run in serialized execution and must not wait for project loading. Their context may initiate loading, but async document and solution accessors return the state captured during context construction immediately.

Document synchronization handlers do not require solution context. This rule also prevents a future mutating handler from stalling subsequent text synchronization by awaiting a design-time build.

### Cancellation, Failure, And Clearing

All context copies share the underlying load and resolution task. Canceling one accessor wait does not cancel loading or shared resolution; another waiter can still receive the result.

Expected project-load failure still triggers latest-state re-resolution. If re-resolution itself fails unexpectedly, Roslyn reports and logs the exception and returns the construction-time state.

`ClearSolutionContext` clears the shared state for memory-sensitive handlers. Access after clearing throws, matching the previous synchronous contract.

## Compatibility Wrappers

Core `RequestContext` no longer exposes synchronous `Document`, `TextDocument`, or `Solution` properties.

ExternalAccess wrappers used by XAML, Hot Reload, Copilot, Compiler Developer SDK, and other partner layers expose async context methods. Existing synchronous wrapper properties remain temporarily for compatibility, are marked obsolete with warning severity, and always return their construction-time snapshots. They never switch to post-load state. Repository consumers use the async APIs.

The corresponding `InternalAPI.Unshipped.txt` baselines track the new methods while retaining the compatibility properties.

## Canonical Project Loading

`LanguageServerProjectLoader` owns canonical per-project state. A normalized lexical project path identifies one `LoadedProject` across on-demand, automatic, explicit project, and explicit solution loading.

The existing gate protects this state. Queue-level batching is not the sole correctness mechanism because it only deduplicates pending work within a batch.

### Project Load Completion

A begin-or-join operation returns the canonical `LoadedProject`. The project owns a shared completion task whose final status is loaded, failed, unsupported, or unloaded. A failed initial load may be retried; loaded and unsupported projects return their completed status without reevaluation.

Expected evaluation failures are caught at the per-project reload boundary, logged through existing mechanisms, and converted to failed status. Primordial projects remain pending until canonical targets are committed. Successful reloads update a previous failed or unsupported status to loaded, while failed reloads preserve an existing successful status.

Outstanding completion tasks finish with unloaded status when their project is unloaded or the project loader is disposed.

Reload freshness is outside this design. A completion requested for an already committed project returns the current status even if a reload is queued or active. Consumers that need project details query the current workspace state after successful completion; completion results do not retain target `ProjectId` snapshots.

### Explicit Loads And Progress

If on-demand loading creates a project without a solution GUID and an explicit solution load later joins it, the existing `LoadedProject` receives the GUID for telemetry.

Progress belongs to each explicit bulk operation, not to a single queued project. Every caller advances its own progress when shared completion tasks settle.

`OpenProjectsAsync` awaits only completion tasks for its requested projects. `OpenSolutionAsync` awaits only completion tasks for projects from that solution. Neither operation waits for unrelated projects in the same batching snapshot.

Project-initialization-complete notifications remain associated with explicit project and solution operations. On-demand operations do not send global initialization-complete notifications.

## Document Context Lookup

`LspWorkspaceManager.GetLspDocumentInfoAsync` first searches registered workspace snapshots using the manager's fork cache. If no registered document is found, it may add an open tracked document to the existing miscellaneous-files provider using the request's captured tracked-document map. On-demand loading is separate from lookup; `RequestContext` waits and then invokes normal resolution again.

The manager owns final telemetry and cleanup. When a registered non-miscellaneous document wins, the manager removes any stale miscellaneous copy. Unexpected non-cancellation lookup exceptions are logged; a registered-workspace failure does not prevent miscellaneous fallback. Workspace-level solution lookup remains a direct host-workspace operation.

## Interaction With File-Based Programs

This design does not change file-based-program discovery or extract a shared recursive workspace walker. File-based-program behavior continues through the existing miscellaneous-files infrastructure.

## Error Handling

- Initialization performs no discovery I/O.
- Ancestor enumeration failures are logged and lookup continues upward.
- Project evaluation failures use existing logging, telemetry, and toast behavior.
- Context access returns the best state available after loading settles.
- Unexpected context refresh failure falls back to construction-time state.
- No new LSP request errors or feature-specific user warnings are introduced.
- Request cancellation cancels only that waiter.
- Server shutdown cancels shared work through loader lifetime cancellation.

## Telemetry And Logging

The implementation reuses existing project-load telemetry and logging. On-demand loading logs discovery/load initiation and failures. No paths are added to telemetry.

Handler preferences and requested or achieved completeness are not part of the design, so there is no completeness telemetry. Dedicated on-demand discovery or wait-duration telemetry can be considered after the behavior is validated.

## Testing

### Discovery

- Initialization records workspace roots without enumeration.
- Lookup selects the deepest containing workspace root and nearest project-bearing ancestor.
- Multiple supported projects are returned in ordinal order.
- Files outside workspace roots do not trigger discovery.
- Project creation and deletion are observed on the next demand without watcher notification.
- Concurrent demands enumerate independently.
- Enumeration errors log and continue upward.
- Workspace-folder changes follow platform path identity.

### Project Loading

- Concurrent demands share active root-closure operations by normalized root project path.
- Settled closure entries are evicted and later demands walk again.
- On-demand, project-open, and solution-open callers share canonical handles.
- Already loaded projects return completed handles.
- Multiple roots, transitive references, cycles, and overlapping closures settle correctly.
- Partial project failure does not strand unrelated loads.
- Workspace operations wait for the explicit-load snapshot captured at context creation.
- Dev Kit and disabled on-demand loading do not initiate discovery.

### Request Context

- A handler begins without waiting and later requests remain responsive while its accessor is blocked.
- Post-load context uses request-time tracked text after a later `didChange`.
- Canceling one accessor wait does not cancel shared loading or later access.
- Mutating contexts return construction-time state without waiting.
- Missing-document access can still return the latest solution.
- Both source `Document` and non-source `TextDocument` paths use async access.
- External compatibility wrappers compile with obsolete construction-snapshot properties and async replacements.

### Integration Validation

Deterministic synthetic tests cover the service boundaries:

- `WorkspaceProjectDiscoveryServiceTests` covers fresh filesystem behavior, nested roots, multiple candidates, unsupported extensions, concurrency, and workspace-folder changes.
- `OnDemandProjectLoaderTests` covers active operation sharing and eviction, transitive and cyclic references, overlapping graphs, partial failure, cancellation, and workspace snapshots.
- `LanguageServerProjectLoaderTests` covers canonical handles, unrelated blocked loads, and unsupported or failing evaluations.
- `HandlerTests.AsyncContextDoesNotBlockLaterRequestsAndUsesRequestTimeText` gates loading, applies a later document change, and proves later mutating and non-mutating requests run before context access completes.
- Handler tests separately cover accessor cancellation isolation and mutating no-wait behavior.

Required validation includes focused Protocol, LanguageServer, framework, and process-host tests plus builds for affected Language Server, Protocol, ExternalAccess, and Razor projects.

## Prioritization And Deferred Work

This change preserves existing `AsyncBatchingWorkQueue` scheduling and project-load parallelism. It does not preempt, promote, or reserve capacity for on-demand work. A context accessor can therefore wait behind a large existing project batch.

Deferred work includes:

- project-load prioritization using the separately enhanced work queue,
- dedicated on-demand telemetry if operational data justifies it,
- reverse-dependant discovery if a future feature requires it, and
- broader document-provider extensibility only when another context source requires it.
