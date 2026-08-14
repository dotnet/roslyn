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

## A request with `"params": null` crashes the language server

**Affected area:** `src/LanguageServer` (StreamJsonRpc `SystemTextJsonFormatter`)
**Description:** A JSON-RPC request whose `params` member is explicitly `null`
(rather than omitted) makes `SystemTextJsonFormatter` throw
`InvalidOperationException("Unexpected value kind: Null")` while deserializing.
The exception faults the JSON-RPC read loop, so the server terminates with an
unhandled exception instead of replying `MethodNotFound` (-32601) for an unknown
method. Tracked by https://github.com/dotnet/roslyn/issues/84890; repro test:
`ServerDisconnectTests.ServerSurvivesUnknownRequestWithNullParams` (skipped).
**Workaround:** None on the server today. Clients should omit `params` instead of
sending `null`; unknown methods with omitted/object params correctly return -32601.
