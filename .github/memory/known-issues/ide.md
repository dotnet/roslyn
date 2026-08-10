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

## Workspace project discovery watches leak in LanguageServer host tests

**Affected area:** LanguageServer tests with workspace folders
**Description:** `WorkspaceProjectDiscoveryService` owns file-watch contexts for
workspace folders but does not currently participate in server disposal. Tests
using `FileWatcherReleaseTracker` can therefore fail at teardown with leaked
workspace-folder watches even when project loading and assertions succeeded.
**Workaround:** Treat the workspace-folder watch path as this known issue when
classifying broader project-system test runs; use focused loader tests for load
lifetime validation until the discovery service releases its watchers on shutdown.
