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

## LSP document lookup can return transient documents

**Affected area:** Language server miscellaneous-files tests
**Description:** `LspWorkspaceManager.GetLspDocumentInfoAsync` can delegate an
unregistered file URI to the miscellaneous-files provider, which may return a
document from a forked solution without adding it to a workspace.
**Workaround:** Tests that need to verify whether a document is persisted or
unloaded must inspect the relevant workspace's `CurrentSolution` instead of
using a null manager lookup as a proxy.
