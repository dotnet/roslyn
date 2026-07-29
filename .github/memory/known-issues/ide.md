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
**Description:** Both the default `MetadataReference.CreateFromFile` path and
the shared-cache path load backing metadata with
`PEStreamOptions.PrefetchEntireImage`. Concurrent cold requests for the same key
may both perform that work; insertion selects one metadata instance for both
callers and disposes the duplicate, but does not single-flight the file read.
The shared cache holds metadata through weak references, allowing all metadata
still owned by another active workspace to be reused without extending its
lifetime. The existing `MetadataReferenceCache` still avoids duplicate
reference and metadata creation within each individual workspace, but it is not
shared between daemon clients.
**Workaround:** Do not interpret project-load timing or BenchmarkDotNet's
allocation-traffic column as retained-metadata savings. Use sequential loading
and enable the benchmark's shared-cache statistics to distinguish useful reuse
from concurrent misses. In the sequential two-Roslyn-solution benchmark, the
second solution reused all 1,488 cacheable metadata values and performed no
successful metadata loads, but elapsed time was unchanged (2.175 minutes
without sharing versus 2.174 minutes with sharing). Metadata loading is not a
dominant cost in this end-to-end project-loading workload. An external
two-client daemon measurement showed the retained-memory benefit that
BenchmarkDotNet's allocation column misses: median daemon private bytes after
two Roslyn solutions fell from 1,847.6 MiB without sharing to 1,342.1 MiB with
sharing (505.4 MiB, or 27.4%). The second solution's private-byte increment fell
from 820.3 MiB to 257.3 MiB. The smaller concurrent two-console-application
benchmark reports process-memory growth around project loading. Across five
measured iterations, sharing reduced median private-byte growth from 11.25 MiB
to 6.43 MiB and median working-set growth from 13.96 MiB to 9.19 MiB. Treat
these as per-workload deltas rather than absolute retained-process sizes.
