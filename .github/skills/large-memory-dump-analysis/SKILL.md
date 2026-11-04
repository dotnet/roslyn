---
name: large-memory-dump-analysis
description: Analyze existing memory dumps of Visual Studio (devenv.exe), its background process DevHub, or Roslyn language server processes for Roslyn memory retention. Use when investigating memory leaks, excessive memory usage, counting Solution or compilation objects, tracing GC roots, or investigating uncollected dead objects.
---

# Visual Studio, DevHub, and Language Server Memory Dump Analysis

Analyze an existing dump using the shared procedure, then the investigations below. Dump collection and product changes are out of scope.

## General instructions

Obtain the local dump path and identify Visual Studio (`devenv.exe`), DevHub, or a Roslyn language server process; ask if unclear. Record the runtime, architecture, Roslyn version, and whether the process was active or idle. For language server processes, identify the active server instances, since daemon mode can host multiple servers in one process. Use matching managed dump tooling and confirm heap data is readable. Report missing memory, runtime support, or symbols as limitations, not zero counts. Do not upload dumps to external services.

Reuse the [modern .NET SOS reference](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/sos-debugging-extension), [.NET Framework SOS reference](https://learn.microsoft.com/en-us/dotnet/framework/tools/sos-dll-sos-debugging-extension), or [dotnet-dump analysis guide](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump#dotnet-dump-analyze), according to runtime support. Commands below use WinDbg's `!` prefix; omit it in dotnet-dump. Consult installed command help for version-specific options.

### Count exact types

```text
!dumpheap -live -stat -type <type-name>
!dumpheap -live -mt <method-table>
```

Use `-live` for all dumpheap calls in Investigations 1 and 2 to exclude unreachable objects awaiting collection. If unsupported, report the limitation rather than treating unfiltered counts as live counts.

The `-type` filter is a substring search. Select only exact concrete type names, excluding arrays, containers, and related types, then enumerate each matching method table with `-mt`. Record addresses, counts, and shallow bytes, keeping runtime/type copies distinguishable. Count distinct addresses, not references; ownership categories can overlap.

### Trace retention

```text
!dumpobj <object-address>
!gcroot -all <object-address>
```

Inspect all incoming root paths and intermediate objects to identify retaining types and fields. An object's reference to a workspace does not mean the workspace retains it. Weak references alone do not establish strong retention; distinguish active stack/task references from retention after completion.

Separate rooted objects, apparently unreachable objects awaiting collection, and unresolved inspection. Only classify an object as apparently unreachable after successful heap/root inspection. Counts alone do not prove a leak, and shallow bytes do not measure the retained graph.

Use source matching the dump's Roslyn version for field names and cache policies.

## Investigation 1: Solution objects

Count `Microsoft.CodeAnalysis.Solution`.

| Process | Expected retention |
|---------|--------------------|
| Visual Studio | One current Solution per workspace. Workspace upper bounds: at most one `VisualStudioWorkspace` implementation, one `MiscellaneousFilesWorkspace`, and two `InteractiveWorkspace` instances; fewer may be present. |
| DevHub | Solutions accounted for by `RemoteWorkspace` and its actual cache contents; inspect cache objects as described below. |
| Language server | One current Solution per workspace: one host and one miscellaneous-files `LanguageServerWorkspace` per server instance, plus the shared metadata-as-source workspace when present. Also account for each server's `LspWorkspaceManager` cache as described below; do not assume a single server per process. |

Trace every Solution's roots, grouping by workspace and retaining owner. Additional solutions held by other owners are suspicious even below the baseline. Explain excess retention rather than raising the baseline.

| Owner | Fields to inspect in this checkout |
|-------|------------------------------------|
| Workspace current solution | `Workspace._latestSolution` |
| RemoteWorkspace caches | `_lastRequestedPrimaryBranchSolution`, `_lastRequestedAnyBranchSolutions` |
| LSP solution cache | `LspWorkspaceManager._cachedLspSolutions` |
| RemoteWorkspace in-flight requests | `_solutionChecksumToSolution`, `InFlightSolution` tasks; verify the request is active |

For each RemoteWorkspace, inspect `_lastRequestedAnyBranchSolutions` with `!dumpobj`: read the cache's `_maxCapacity` and traverse `_cacheNodes` for additional solutions. Check that this count does not exceed capacity; `_totalHistory` includes checksum-only entries and is not a Solution limit. Combine the actual cached Solution addresses with `_latestSolution` and `_lastRequestedPrimaryBranchSolution.solution`, deduplicating shared instances.

Account separately for `_solutionChecksumToSolution` entries backed by active requests. Report current/cache-owned, in-flight, and unexpected retention; do not infer a process-wide limit from one cache's capacity.

For each LspWorkspaceManager, inspect `_cachedLspSolutions`: each workspace entry holds the last Solution returned for LSP, which may share the workspace's current Solution or be a fork with LSP text applied. Deduplicate shared Solution addresses and account for these cached snapshots separately from active requests. Apply this cache check to language servers hosted in Visual Studio as well as standalone language server processes.

Source: `src\Workspaces\Core\Portable\Workspace\Workspace.cs` for current ownership; `src\Workspaces\Remote\ServiceHub\Host` for RemoteWorkspace caching and request lifetime; `src\LanguageServer\Microsoft.CodeAnalysis.LanguageServer\HostWorkspace` for language server workspace ownership; `src\LanguageServer\Protocol\Workspaces\LspWorkspaceManager.cs` for LSP solution caching.

## Investigation 2: Compilation objects

`Microsoft.CodeAnalysis.Compilation` is abstract. Count `Microsoft.CodeAnalysis.CSharp.CSharpCompilation` and `Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation`, reporting the combined total with per-language counts.

### Compare with the project-based upper bound

For each distinct Solution from Investigation 1, inspect its project collection. Expect at most two compilations per project per Solution:

```text
compilation upper bound = 2 * sum(project count for each distinct Solution object)
```

Do not deduplicate projects across snapshots. Shared or not-yet-created compilations can make the actual count lower. If project counts are unavailable, mark the bound incomplete.

A total much higher than the bound indicates a suspected leak; trace roots to identify retaining owners.

### Check analyzer and source generator retention

Analyzers and source generators should not retain compilations after execution completes. Check their instance/static fields, caches, and captured callbacks, including indirect references through semantic models or symbols. Flag persistent retention even below the upper bound, recording the analyzer/generator type, field, and root path.

## Investigation 3: Dead objects and GC behavior

If Investigations 1 and 2 reveal no retention problems, expand to unreachable objects awaiting collection. Replace `-live` with `-dead`; never combine the two flags.

```text
!dumpheap -live -stat
!dumpheap -dead -stat
!dumpheap -dead -mt <method-table>
!eeheap -gc
!finalizequeue
```

Compare live and dead counts and bytes, starting with Solutions and compilations, then the largest dead-object totals across the heap. Select exact method tables from the dead statistics, including types absent from the live results. Inspect heap generations/segments and finalization backlog; examine GC/finalizer thread stacks if work appears stalled.

Dead objects alone do not indicate a leak or failed GC: they may have become unreachable since the last collection, and older generations need not be collected by every GC. Correlate with available GC events, collection counts, allocation activity, or timestamped dumps to determine whether relevant collections are occurring and reclaiming memory. A single dump cannot establish GC frequency; report missing evidence and recommend a GC trace or comparison dumps when needed.

## Report the findings

Include process/runtime context, measured live counts and bounds, per-Solution project counts, ownership classifications, and inspection limitations (including sampling). For suspicious objects, tabulate addresses, retaining types/fields, and root paths. If Investigation 3 was needed, report dead-object totals and evidence about collection behavior separately.

If active work prevents a retention conclusion, recommend comparing an idle dump.
