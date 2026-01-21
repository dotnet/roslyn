# Diagnostic Analysis System

## Overview

The diagnostic analysis system computes diagnostics (errors, warnings, and informational messages) from analyzers for
documents and projects within a solution. The system evolved from a complex "solution crawler" architecture to a
snapshot-based model where callers request diagnostics for a specific immutable solution snapshot and receive accurate
results for that snapshot.

**Core principle:** Given an immutable snapshot of a document or project, compute and return the correct diagnostics for
that snapshot. The system handles caching and optimization internally as implementation details that preserve
correctness guarantees.

### Integration with Higher-Level Systems

Two distinct higher-level systems consume this diagnostic analysis service:

1. **Live Diagnostics System**: Drives squiggles, error list, and real-time feedback during editing. The LSP pull
   diagnostics model serves as the primary mechanism for delivering diagnostics to VS features. The LSP layer maintains
   its own cache of diagnostic results and invokes `IDiagnosticAnalyzerService` only when changes would invalidate its
   cache. The LSP layer's diagnostics drive most diagnostics features.

2. **Explicit "Run Code Analysis"**: A user-invoked feature that computes all diagnostics for a project and displays the
   cached results until cleared or another analysis is initiated. This resembles executing a Build (retrieving only
   analysis results) from within VS itself.

3. **Other Diagnostic Analysis Clients**: Beyond the LSP-based and explicit analysis systems, several other features
   directly consume `IDiagnosticAnalyzerService`:
   - **Inline/Inlay Hints**: These visual enhancements are not yet powered by LSP and directly query the diagnostic
     service for analysis results.
   - **CodeFixService**: This underlying system powers features like lightbulbs to determine which diagnostics are
     present that could be fixed. Code fixes query the diagnostic service to obtain diagnostics for a specific span,
     then match available fixes to those diagnostics.
   - **Copilot**: AI-assisted code generation uses the diagnostic service to check if generated code would produce
     diagnostics and whether those diagnostics can be automatically fixed before presenting suggestions to users.

   This list is not exhaustive—other features and systems may also directly consume diagnostic analysis as needed.
   Ideally, all of these scenarios would flow through LSP in the future for consistency, but the system is not yet at
   that stage.

For features requiring lower-level access, this system provides a consistent API with well-defined behavior.

## Architecture

### Entry Point: IDiagnosticAnalyzerService

The primary interface `IDiagnosticAnalyzerService` provides methods for requesting diagnostics at different scopes:

```csharp
Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
    TextDocument document, TextSpan? range, ...);

Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
    Project project, ImmutableArray<DocumentId> documentIds, ...);

Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
    Project project, ...);
```

These methods constitute the exclusive entry points features should utilize. Upon invocation:

1. Attempts execution out-of-process (OOP) if a remote host is available
2. Falls back to in-process execution if OOP is unavailable or already executing in OOP

The OOP transition occurs once at the interface boundary.

### Analyzer Types

The system handles two fundamentally different analyzer types:

#### 1. Compilation-Based Analyzers

Standard Roslyn `DiagnosticAnalyzer`s that participate in compilation analysis. Execution occurs through the compiler's
`CompilationWithAnalyzers` infrastructure, providing:
- Full compilation access
- Semantic model information
- Symbol information
- Incremental execution and caching

This path serves C# and VB analyzers that analyze code using semantic information.

#### 2. DocumentDiagnosticAnalyzers

Specialized analyzers operating outside the compilation model, used by languages lacking Roslyn compilations (F#, XAML,
TypeScript). These analyzers implement `DocumentDiagnosticAnalyzer` and execute directly without
`CompilationWithAnalyzers`, receiving a `TextDocument` for independent analysis.

### Host vs. Project Analyzers

The system distinguishes between two analyzer sources:

#### Host Analyzers
- Installed as VS extensions (VSIX) or built into the IDE
- Apply globally to all workspace projects
- Loaded once per language and shared across projects
- Utilize host analyzer options (IDE settings)

#### Project Analyzers
- Referenced via NuGet packages (`<PackageReference>`)
- Scoped to individual projects
- Each project may reference different analyzers or versions
- Utilize project-specific analyzer options (`.editorconfig` settings without host fallback)

This distinction enables isolation and versioning. Project analyzers from different projects must load simultaneously,
even when representing different versions of the same assemblies.

### Analyzer Isolation and Assembly Load Contexts

Due to out-of-process execution in a .NET Core environment, the system loads project analyzers into isolated Assembly
Load Contexts (ALCs) enabling:
1. Simultaneous loading of different versions of same-named analyzer assemblies across projects
2. Unloading and reloading analyzers upon disk changes. This accommodates developers actively building their own
   analyzers and generators, allowing reload within a running Visual Studio instance without restarting. This
   substantially improves development efficiency and iteration speed.
3. Side-by-side execution without assembly conflicts

The `IsolatedAnalyzerReferenceSet` class manages this isolation:

```
1. Compute checksum for analyzer reference set
2. Verify if isolated set exists for this checksum
3. If absent, instantiate new ALC with shadow-copy loader
4. Load all analyzer assemblies into this ALC
5. Cache isolated references by checksum
6. Return IsolatedAnalyzerFileReference wrappers
```

The system maintains a "current" isolated set, adding new analyzers provided no MVID conflicts exist. Upon conflict
detection (analyzer DLL modified on disk), a new isolated set is instantiated. Previous sets persist while any analyzer,
generator, or diagnostic from them remains referenced. Once all references are released, the ALC cleanly unloads itself,
removing its code and associated burden from the .NET runtime.

**Key insight:** The checksum encompasses the complete analyzer assembly closure and their MVIDs. Projects with
identical analyzer references (same packages, same versions) share the identical isolated set and ALC, eliminating
redundant loading.

## CompilationWithAnalyzers Caching

For compilation-based analyzers, the system maintains a cache of `CompilationWithAnalyzers` instances:

```csharp
ConditionalWeakTable<
    Project,
    SmallDictionary<
        ImmutableArray<DiagnosticAnalyzer>,
        AsyncLazy<CompilationWithAnalyzers?>>>
```

This structure provides:

- **Outer key (Project):** Cache lifetime bound to a specific `Project` instance.

- **Inner key (Analyzer Array):** Multiple analyzer sets may be cached per project, handling scenarios such as:
  - Complete project analyzer set (most common, ~99% of queries)
  - Single analyzer for targeted lightbulb actions
  - Filtered set for specific diagnostic IDs

`SmallDictionary` is employed because typically only 1-2 entries exist. The overwhelming majority of cases contain
exactly one entry: the complete analyzer set for the project. In a small number of cases, this may expand. For example,
when a user invokes Fix All for a specific analyzer across a solution, causing only that analyzer to execute on all
projects. This scenario justifies the map's existence: when executing Fix All for a particular analyzer, re-running all
analyzers across all projects would be prohibitively expensive.

**Lifetime:** The `ConditionalWeakTable` ensures cache lifetime matches the `Project` instance. Upon release of all
`Project` references, the cache is collected. No explicit invalidation occurs.

**Creation:** When instantiating `CompilationWithAnalyzers`, the system:
1. Filters out `DocumentDiagnosticAnalyzer`s (excluded from compilation analysis)
2. Deduplicates the analyzer list
3. Creates analyzer options distinguishing host from project analyzers
4. Configures compilation analysis options (non-concurrent execution, log execution time, report suppressed diagnostics)

## Diagnostic Computation Flow

### Document-Level Diagnostics

When features request diagnostics for a document or span (e.g., lightbulbs, error squiggles):

```
GetDiagnosticsForSpanAsync
  ↓
  Attempt OOP execution (if available)
  ↓
  GetDiagnosticsForSpanInProcessAsync
    ↓
    Collect all project analyzers
    Filter analyzers by:
      - Priority (High/Normal/Low for lightbulbs)
      - Diagnostic kind (Syntax/Semantic/Compiler/Analyzer)
      - Analysis kind support (Syntax/Semantic)
      - Span-based analysis capability
    ↓
    Partition into three sets:
      - syntaxAnalyzers
      - semanticSpanAnalyzers (support span-based semantic analysis)
      - semanticDocumentAnalyzers (require full document)
    ↓
    ComputeDiagnosticsInProcessAsync
      ↓
      For each analyzer set:
        - Locate or instantiate CompilationWithAnalyzers
        - Create DocumentAnalysisExecutor
        - Compute diagnostics via executor
        - Optionally employ incremental member edit analysis
      ↓
      Merge and filter results by requested span
```

### Explicit "Run Code Analysis"

For the user-invoked "Run Code Analysis" feature:

```
ForceRunCodeAnalysisDiagnosticsAsync
  ↓
  Attempt OOP execution (if available)
  ↓
  ForceRunCodeAnalysisDiagnosticsInProcessAsync
    ↓
    Retrieve all project analyzers
    Filter by effective severity (exclude if all descriptors hidden)
    Include compiler analyzer, suppressors, built-ins
    ↓
    Parallel execution:
      - Document diagnostics for all documents
      - Project diagnostics (compilation-level)
    ↓
    Merge results
```

This feature computes all diagnostics and displays cached results until cleared or another "Run Code Analysis" phase is
initiated.

## DocumentAnalysisExecutor

The `DocumentAnalysisExecutor` class orchestrates analysis for a specific document and analyzer set:

```csharp
public sealed partial class DocumentAnalysisExecutor
{
    private readonly DocumentAnalysisScope _analysisScope;
    private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;
    
    // Cached results preventing recomputation
    private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? _lazySyntaxDiagnostics;
    private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? _lazySemanticDiagnostics;
}
```

The executor handles special treatment of the compiler analyzer versus other analyzers:

**Compiler Analyzer:**
- Receives immediate, span-based execution (independent of other analyzers)
- Critical for rapid error squiggle feedback during typing

**Other Analyzers:**
- Initial invocation computes diagnostics for **all** analyzers within scope
- Results cached in `_lazySyntaxDiagnostics` or `_lazySemanticDiagnostics`
- Subsequent requests for individual analyzers return cached results
- Batching improves performance through shared compilation work

For span-based requests, the executor adjusts the span for compiler diagnostics to encompass complete member
declarations, accommodating historical compiler API limitations.

## Incremental Member Edit Analysis

When analyzing complete documents (no span), the system optimizes for the common case of typing within a method body:

```csharp
internal sealed partial class IncrementalMemberEditAnalyzer
{
    private WeakReference<Document?> _lastDocumentWithCachedDiagnostics;
    private MemberSpans _savedMemberSpans;  // Document ID + version + member spans
}
```

**Conditions for incremental analysis:**
1. Complete document analysis (no span)
2. Semantic analysis kind
3. Analyzer supports span-based semantic analysis (via `SupportsSpanBasedSemanticDiagnosticAnalysis()`)
4. Single member modified between document versions

**Execution flow when triggered:**
```
1. Detect single member modification (via IDocumentDifferenceService)
2. Retrieve cached member spans from previous document version
3. Obtain cached diagnostics from previous version
4. Re-analyze only the modified member
5. Merge:
   - New diagnostics for modified member
   - Adjusted previous diagnostics for unchanged members (span updates for edits)
6. Update cache for subsequent iteration
```

**Fallback:** Upon condition failure (multiple members modified, initial analysis, version mismatch), the system falls
back to complete document analysis.

**Analyzer support:** Analyzers indicate span-based support through:
```csharp
public bool SupportsSpanBasedSemanticDiagnosticAnalysis()
{
    return this is IBuiltInAnalyzer { RequestedAnalysisKind: AnalyzerCategory.SemanticSpanAnalysis };
}
```

Built-in analyzers opt into this via `IBuiltInAnalyzer.GetAnalyzerCategory()`. The `SemanticSpanAnalysis` category
signifies: "Edits within a method body affect only diagnostics reported on that method body."

## Deprioritization System

To enhance lightbulb performance, expensive analyzers are deprioritized from `CodeActionRequestPriority.Normal` to
`CodeActionRequestPriority.Low`:

```csharp
// Cached per analyzer - assumed stable across compilations
ConditionalWeakTable<DiagnosticAnalyzer, ImmutableHashSet<string>?> 
    s_analyzerToDeprioritizedDiagnosticIds;
```

**Deprioritized analyzers:**
- Register `SymbolStartAnalysisContext`/`SymbolEndAnalysisContext` actions
- Register `SemanticModelAction`s
- Expensive due to broad analysis scope

**Exception:** The compiler analyzer is never deprioritized.

**Execution model:**
1. During span-based semantic analysis at `Normal` priority
2. Verify if analyzer is deprioritized
3. If affirmative, defer to subsequent `Low` priority pass

This establishes a two-tier execution model:
- **Normal priority:** Fast analyzers + compiler
- **Low priority:** Expensive analyzers requiring deep analysis, or analyzers explicitly configured for this tier

Built-in analyzers utilize `IBuiltInAnalyzer.IsHighPriority = true` to elevate to high priority. This flag is employed
sparingly—high-priority items must complete rapidly and provide access to critical features users demand with minimal
latency. 

The most prominent example is "Add Using," which represents the most frequently used lightbulb feature by more than an
order of magnitude. Users expect to type an unresolved type name, press Ctrl+., and receive the suggestion to add the
relevant using directive near-instantaneously. Any analyzer interference with this workflow creates noticeable friction.

Similarly, when users overtype a variable or symbol with a new name, they expect to press Ctrl+. and immediately see
"Rename X to Y" at the top of the list, enabling instant invocation without waiting for other analyzer results. These
high-value, high-frequency operations must remain unimpeded by slower analyzers.

The cache is populated lazily by querying `CompilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync()` to inspect
registered actions.

## Diagnostic Versions

The system employs versions to determine when diagnostics require recomputation:

```csharp
public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
    => project.GetDependentVersionAsync(cancellationToken);
```

`GetDependentVersionAsync` returns a version that changes when:
- Any document within the project changes
- Project references change
- Compilation options change
- Any transitively referenced project changes

This version enables:
1. Determination of cached diagnostic validity
2. Tagging diagnostic results with version stamps
3. Incremental member edit analysis (version matching)

**Critical property:** If the version remains unchanged, diagnostics from previous computation remain correct for the
current snapshot.

## Analysis Scopes and Kinds

The system supports multiple analysis scopes:

### Analysis Kinds
```csharp
enum AnalysisKind
{
    Syntax,      // Syntax tree only, no semantic model
    Semantic,    // Complete semantic analysis
    NonLocal     // Project-level/compilation-level diagnostics
}
```

### Analysis Scopes
```csharp
class DocumentAnalysisScope
{
    TextDocument TextDocument;
    TextSpan? Span;                          // null = complete document
    ImmutableArray<DiagnosticAnalyzer> Analyzers;
    AnalysisKind Kind;
}
```

Different features request distinct scopes:
- **Error list:** Complete project, all analyzers, all kinds
- **Lightbulb:** Current span, filtered analyzers, syntax + semantic
- **Error squiggles:** Visible span, all analyzers, syntax + semantic
- **Code fixes:** Diagnostic span, specific analyzer, semantic only

## Filtering and Priority

When computing diagnostics for lightbulbs, the system applies aggressive filtering:

### Priority Matching
```csharp
CodeActionRequestPriority.High    → High-priority analyzers only (IBuiltInAnalyzer with IsHighPriority)
CodeActionRequestPriority.Normal  → Normal priority analyzers (excluding deprioritized)
CodeActionRequestPriority.Low     → Deprioritized analyzers or analyzers explicitly configured for this tier
CodeActionRequestPriority.Lowest  → All analyzers (for suppressions/configuration)
```

### Diagnostic ID Filtering
```csharp
class DiagnosticIdFilter
{
    static DiagnosticIdFilter All;                        // No filtering
    static DiagnosticIdFilter Include(string[] ids);      // Specified IDs only
    static DiagnosticIdFilter Exclude(string[] ids);      // All except specified
}
```

Diagnostic ID filtering exists to enable features to customize analyzer execution without requiring callbacks.
Previously, features passed lambda callbacks to control which analyzers would run. However, the migration to
out-of-process execution made this untenable—callbacks cannot be trivially serialized across process boundaries, and the
VS side lacks the analyzer references needed for interrogation (and deliberately avoids loading analyzers in the .NET
Framework process).

The pure-data `DiagnosticIdFilter` model enables features to specify filtering declaratively, which can be efficiently
remoted to OOP for execution. Examples:

- **CodeFixService** uses `Include` to specify only diagnostic IDs that have registered code fixes, avoiding unnecessary
  analyzer execution when computing lightbulb results.
- **Code Cleanup** uses `Exclude` to filter out IDE diagnostic IDs when computing third-party analyzer diagnostics,
  ensuring it processes only non-IDE diagnostics for cleanup operations.

### Diagnostic Kind Filtering
```csharp
DiagnosticKind.CompilerSyntax    → Compiler syntax diagnostics only
DiagnosticKind.CompilerSemantic  → Compiler semantic diagnostics only
DiagnosticKind.AnalyzerSyntax    → Analyzer syntax diagnostics only
DiagnosticKind.AnalyzerSemantic  → Analyzer semantic diagnostics only
DiagnosticKind.All               → All diagnostics
```

These filters combine to minimize computational work. For example, a high-priority lightbulb request for syntax
diagnostics will:
1. Execute only high-priority analyzers
2. Perform syntax analysis exclusively
3. Skip semantic analysis entirely
4. Return only diagnostics within the requested span

## Project vs. Host Analyzer Options

When instantiating `CompilationWithAnalyzers`, the system configures analyzer options differently for host versus
project analyzers:

```csharp
// Simplified logic from GetOptions():

if (all analyzers are host analyzers)
    return project.State.HostAnalyzerOptions;

if (all analyzers are project analyzers)
    return project.State.ProjectAnalyzerOptions;

// Mixed case: provide per-analyzer options
return (
    sharedOptions: project.State.HostAnalyzerOptions,
    analyzerSpecificOptionsFactory: analyzer =>
        isProjectAnalyzer(analyzer)
            ? project.State.ProjectAnalyzerOptions.AnalyzerConfigOptionsProvider
            : project.State.HostAnalyzerOptions.AnalyzerConfigOptionsProvider
);
```

**Rationale:**
- Host analyzers utilize IDE-wide settings (Tools > Options, global `.editorconfig`)
- Project analyzers utilize **exclusively** project-local `.editorconfig`, without host settings fallback
- This ensures project analyzers exhibit identical behavior in the IDE and command-line builds

## Historical Context: The Solution Crawler

The current system evolved from a fundamentally different architecture termed the "solution crawler":

**Problems with the previous system:**
1. **Opaque execution order:** The crawler traversed the solution in an unclear order
2. **Complex invalidation:** Determining when to invalidate cached diagnostics was difficult
3. **Stale data:** Features might operate on current snapshot while receiving stale diagnostics
4. **Complex event propagation:** Background changes triggered intricate event cascades

**The current model:**
- Features provide an immutable snapshot
- System guarantees correct diagnostics for that precise snapshot
- No background crawling or automatic updates
- Features explicitly request diagnostics when required (e.g., LSP pull diagnostics)

**Remaining artifacts:**
- Caching complexity potentially over-engineered for current requirements
- Layered abstractions appropriate for the crawler architecture
- The name "DiagnosticAnalyzerService" (originally a "service" within the crawler architecture)

Future refactoring opportunities include simplifying the caching layer and removing unnecessary indirection.

## Performance Characteristics

### Caching Strategy

**Cached entities:**
- `CompilationWithAnalyzers` per project per analyzer set
- Syntax diagnostics per document per analyzer set
- Semantic diagnostics per document per analyzer set
- Incremental member edit state

**Not cached:**
- Document-level diagnostic results (recomputed per request)
- Project-level diagnostics
- Diagnostic descriptors (computed on demand)

**Rationale:**
Compilation and analyzer execution are computationally expensive. Final diagnostic result merging/filtering is
inexpensive. This trades increased memory consumption for reduced response latency on repeated requests.

### Optimization Techniques

1. **Batched Analysis:** Computing diagnostics for all analyzers simultaneously shares compilation work
2. **Compiler Fast Path:** Compiler analyzer receives immediate execution for rapid error feedback
3. **Lazy Semantic Analysis:** Semantic analysis executes only when required for the request
4. **Incremental Member Edits:** Reuses diagnostics when only a single method changed
5. **Deprioritization:** Expensive analyzers execute in lower priority passes
6. **OOP Execution:** Heavy analysis executes out-of-process maintaining IDE responsiveness

### Parallelization

**Across documents:** Project-level analysis executes documents in parallel

**Across analyzers:** `CompilationWithAnalyzers` executes analyzers concurrently when configured

## Special Cases

### Project Load Failures

When a project fails to load (missing references, corrupted project file):
- Syntax diagnostics are reported
- Semantic diagnostics from the compiler are suppressed
- Analyzer diagnostics are computed and reported

This prevents overwhelming users with cascading semantic errors when the project is misconfigured.

### Additional Files

Additional files (non-code files like `.editorconfig`, resource files) support analysis:
- Provided to analyzers via `AdditionalFileAnalysisContext`
- Handled through the `TextDocument` abstraction
- Lack syntax trees or semantic models
- DocumentDiagnosticAnalyzers may analyze them

### Generated Files

Source-generated files receive treatment identical to regular documents:
- Included in project-level analysis
- Possess complete syntax and semantic support
- Visible to analyzers as standard files
- Cached with the project's compilation

### Suppressions

Diagnostic suppressors constitute a specialized analyzer type:
- Always executed, regardless of filtering
- Can suppress diagnostics from other analyzers
- Execute via `IPragmaSuppressionsAnalyzer` interface
- Applied after all other diagnostics are computed

## Future Improvements

Several areas warrant simplification or investigation:

### Compiler Analyzer Caching

It remains unclear whether compiler analyzer diagnostics are cached effectively. The compiler analyzer bypasses the
`_lazySemanticDiagnostics` cache in `DocumentAnalysisExecutor`, potentially resulting in recomputation on each
invocation. This warrants investigation to determine if additional caching would yield benefits.

### Cache Layer Simplification

The location and organization of caching remains unclear. Caching exists in the LSP layer, and various caching
mechanisms exist within the diagnostic service (both computation results and intermediary state like
`CompilationWithAnalyzers` and `IncrementalMemberEditAnalyzer`). Greater consistency would be beneficial, or at minimum,
better data/information indicating where caches are necessary and their effectiveness metrics (hit rates, memory
consumption).

### Enhanced Telemetry

Increased visibility into cache hit rates, analyzer execution times, and performance bottlenecks would guide
optimization efforts more effectively.

The system functions effectively but retains complexity from its evolution. Future work should emphasize simplification
while preserving correctness guarantees.
