---
coverage: IDE-layer (src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}) known issues, quirks & workarounds
---

# IDE — Known Issues

Layer-specific quirks for the IDE/Workspaces stack. Load when working under
`src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}`.
Cross-cutting issues live in `.github/memory/KNOWN_ISSUES.md`.

## MEF composition failures surface as test failures

**Affected area:** MEF-dependent IDE/Workspaces tests
**Description:** A missing/incorrect MEF export attribute often manifests as an
unrelated-looking test failure rather than a clear composition error.
**Workaround:** When IDE tests fail unexpectedly, check the export attributes
first (`[ExportLanguageService]`/`[ExportWorkspaceService]`, `[Shared]`,
`[ImportingConstructor]` + `[Obsolete(MefConstruction.ImportingConstructorMessage)]`).

## The language-server daemon mutex tracks the accepting lifetime

**Affected area:** `src/LanguageServer` daemon shutdown

**Description:** The daemon server mutex signals that the daemon is accepting
connections, not merely that its process is alive. On an initial-connection or
idle timeout, `NamedPipeDaemonConnectionSource` disposes the pending listener,
commits shutdown with no active connections, and then releases the mutex before
the connection manager and `Program` finish their broader teardown. This order
allows a replacement daemon to start without advertising it while the old
listener can still accept clients.
