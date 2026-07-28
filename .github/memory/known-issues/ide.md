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

## LSP relay completions identify endpoints, not connections

**Affected area:** `src/LanguageServer/roslyn-language-server/LspRelay.cs`
**Description:** The relay has one copy task per traffic direction. Each task
reports the endpoint that ended that copy, not which connection closed overall.
A clean shutdown can report the editor endpoint from both tasks because the
editor closes its bidirectional transport after sending LSP `exit`, before the
daemon closes its side.
**Workaround:** Treat two server-endpoint completions as definitive daemon loss,
but do not require the two tasks to report opposite endpoints for a clean
shutdown.
