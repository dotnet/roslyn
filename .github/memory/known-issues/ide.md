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

## Command-handler unit tests can bypass host paste transformations

**Affected area:** Editor paste command handlers
**Description:** A unit test that invokes a command handler with
`ReplaceSelection` as its next handler exercises the Roslyn handler but not the
terminal Visual Studio paste operation. The host can transform documentation
comment text before Roslyn observes the post-paste snapshot, such as escaping a
literal `<` as `&lt;`.
**Guidance:** Validate behavior that depends on the original clipboard text in
an experimental Visual Studio instance or an integration test that includes the
host paste pipeline.
