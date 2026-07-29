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

## Shared LSP metadata cache eagerly reads and does not coalesce misses

**Affected area:** Language Server project loading and metadata benchmarks
**Description:** With sharing disabled, the Language Server metadata service
creates lazy file references. With sharing enabled, it immediately loads the
backing metadata with `PEStreamOptions.PrefetchEntireImage`. Concurrent cold
requests for the same key may both perform that work; insertion selects one
metadata instance for both callers and disposes the duplicate, but does not
single-flight the file read.
**Workaround:** Do not interpret project-load timing or BenchmarkDotNet's
allocation-traffic column as retained-metadata savings. Use sequential loading
to isolate warm-cache behavior, and instrument cache hits, duplicate misses, and
evictions when validating cache effectiveness.
