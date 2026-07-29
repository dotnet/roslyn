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
single-flight the file read. The shared cache holds metadata through weak
references, allowing all metadata still owned by another active workspace to be
reused without extending its lifetime. The existing `MetadataReferenceCache`
still avoids duplicate reference and metadata creation within each individual
workspace, but it is not shared between daemon clients.
**Workaround:** Do not interpret project-load timing or BenchmarkDotNet's
allocation-traffic column as retained-metadata savings. Use sequential loading
and enable the benchmark's shared-cache statistics to distinguish useful reuse
from concurrent misses. In the sequential two-Roslyn-solution benchmark, the
second solution reused all 1,488 cacheable metadata values and performed no
successful metadata loads, but elapsed time was unchanged (2.175 minutes
without sharing versus 2.174 minutes with sharing). Metadata loading is not a
dominant cost in this end-to-end project-loading workload.
