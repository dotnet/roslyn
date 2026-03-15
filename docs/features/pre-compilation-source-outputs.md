# Pre-Compilation Source Outputs

## Summary

This document proposes a new API, `RegisterPreCompilationSourceOutput`, for the incremental source generator pipeline. This API allows generators to produce source that is added to the *initial* compilation — before any compilation-dependent phases execute — while still being able to read from non-compilation input providers such as `AdditionalTextsProvider` and `ParseOptionsProvider`.

This bridges the gap between `RegisterPostInitializationOutput` (which takes no inputs at all) and `RegisterSourceOutput` (which has access to the full compilation), enabling scenarios where generated source must be visible to subsequent generator phases and other generators, but the generation logic itself only depends on non-compilation inputs.

## Motivation

Today, the incremental generator pipeline has a clear ordering:

1. **`RegisterPostInitializationOutput`** — Runs first, before the compilation is available. Takes no inputs. Source is added to the initial compilation.
2. **`RegisterSourceOutput` / `RegisterImplementationSourceOutput`** — Runs after the compilation (including post-init sources) is available. Can access the full compilation, syntax trees, and semantic model.

This creates a gap: if a generator needs to produce source that should be visible to later phases (its own or other generators'), but the generation logic depends on non-compilation inputs like additional files or parse options, there is no appropriate API.

### Primary Motivation: Razor Performance

The primary motivation for this API is a significant performance improvement for the Razor source generator.

Razor has **cross-file dependencies**: a `.razor` file can reference components declared in other `.razor` files. To resolve these references, the Razor generator must compile in two phases internally:

1. **Declaration collection**: Parse all `.razor` files and collect the partial type declarations (component names, parameters, etc.).
2. **Binding and implementation**: Use those declarations to perform semantic analysis — resolving component references, validating parameter types, generating the full implementation.

Today, because `RegisterSourceOutput` does not feed source back into the compilation, Razor must perform this two-phase process **entirely within its own generator**. Concretely, Razor:

1. Creates a **copy** of the compilation.
2. Adds the partial declarations into this copied compilation.
3. Performs binding against the copied compilation to discover cross-file information.
4. **Throws the copied compilation away**.
5. Produces its final output against the original compilation.

This means the system creates **three compilations** during a generator run: the initial compilation (pre-SG), Razor's private intermediate compilation (for binding), and the final compilation (with SG outputs). The intermediate compilation is pure overhead — it duplicates all the work of building a compilation just so Razor can ask semantic questions about its own declarations.

With `RegisterPreCompilationSourceOutput`, Razor can instead:

1. Use `RegisterPreCompilationSourceOutput` to emit partial declarations from `.razor` files. These are added to the **initial** compilation by the driver.
2. Use `RegisterSourceOutput` with the `CompilationProvider` to perform binding against the initial compilation — which now already contains the declarations. The generator produces the remaining implementation parts as standard source output.

In this model, the system creates only **two compilations**: the initial compilation (pre-SG, augmented with pre-compilation sources) and the final compilation (with all SG outputs). Razor no longer needs its private intermediate compilation. Early experiments show this yields a **roughly 50% performance improvement** for Razor source generation.

Critically, this improvement comes without adding any new compilation phases to the driver. The pre-compilation outputs are simply additional steps that run before the compilation is finalized — the total number of *compilation* objects created by the driver remains the same as today (the initial compilation is augmented in-place with `AddSyntaxTrees`, not rebuilt from scratch).

### Additional Scenarios

**Cross-Generator Type Visibility**: As a secondary benefit, pre-compilation sources are visible to *all* generators' standard phases, not just the generator that produced them. A generator that reads `.proto` files and emits C# types via `RegisterPreCompilationSourceOutput` makes those types available to other generators that perform binding in their `RegisterSourceOutput` phase. This enables cross-generator interoperability without requiring generators to know about each other.

**Configurable Post-Initialization**: Generator authors want to emit attributes or other marker types during post-initialization, but need to read `AnalyzerConfigOptions` or additional files to determine *which* sources to emit (see [#53632](https://github.com/dotnet/roslyn/issues/53632)). `RegisterPostInitializationOutput` cannot accept any inputs, forcing authors to either emit all possible sources unconditionally or split their generator into multiple packages. `RegisterPreCompilationSourceOutput` addresses this directly: the generated source is added to the initial compilation just like post-init source, but the generator can read parse options, analyzer config options, and additional files to decide what to emit.

## Proposed API

```csharp
public readonly partial struct IncrementalGeneratorInitializationContext
{
    /// <summary>
    /// Register a source output that will execute before the compilation is available.
    /// The produced source will be added to the initial compilation, making it visible
    /// to subsequent phases and other generators.
    /// </summary>
    /// <remarks>
    /// The source value provider must not depend on compilation or syntax node inputs.
    /// Accessing compilation or syntax node data from this phase will result in null
    /// values or runtime exceptions.
    /// </remarks>
    [Experimental(RoslynExperiments.PreCompilationSourceOutput)]
    public void RegisterPreCompilationSourceOutput<TSource>(
        IncrementalValueProvider<TSource> source,
        Action<SourceProductionContext, TSource> action);

    [Experimental(RoslynExperiments.PreCompilationSourceOutput)]
    public void RegisterPreCompilationSourceOutput<TSource>(
        IncrementalValuesProvider<TSource> source,
        Action<SourceProductionContext, TSource> action);
}
```

A new `IncrementalGeneratorOutputKind` value is also introduced:

```csharp
[Flags]
public enum IncrementalGeneratorOutputKind
{
    // ... existing values ...

    /// <summary>
    /// A pre-compilation source output, registered via
    /// <see cref="IncrementalGeneratorInitializationContext.RegisterPreCompilationSourceOutput"/>.
    /// Source is added to the initial compilation before compilation-dependent phases execute.
    /// </summary>
    PreCompilation = 0b10000
}
```

### Usage Example

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // Pre-compilation phase: read additional files and emit source
    // that will be visible to the compilation.
    var protoFiles = context.AdditionalTextsProvider
        .Where(f => f.Path.EndsWith(".proto"));

    context.RegisterPreCompilationSourceOutput(protoFiles, (ctx, file) =>
    {
        var types = ParseProtoFile(file.GetText()!.ToString());
        ctx.AddSource($"{file.Path}.g.cs", GenerateTypes(types));
    });

    // Regular phase: the compilation now contains the types generated above.
    context.RegisterSourceOutput(context.CompilationProvider, (ctx, compilation) =>
    {
        var messageType = compilation.GetTypeByMetadataName("MyProto.MyMessage");
        // messageType is non-null — it was generated in the pre-compilation phase
        ctx.AddSource("Serializers.g.cs", GenerateSerializer(messageType!));
    });
}
```

## Execution Model

The updated pipeline execution order is:

```
1. RegisterPostInitializationOutput
   └── Source added to initial compilation (takes no inputs)

2. RegisterPreCompilationSourceOutput          ← NEW
   └── Reads non-compilation inputs (additional files, parse options, etc.)
   └── Source added to initial compilation
   └── Compilation is rebuilt with new sources

3. RegisterSourceOutput / RegisterImplementationSourceOutput
   └── Reads full compilation (now includes post-init AND pre-compilation sources)
   └── Source is part of final output but not fed back into compilation
```

Concretely, in the generator driver:

1. Post-initialization sources are collected as today.
2. Pre-compilation output nodes are evaluated for all generators. Their sources are parsed into syntax trees.
3. The initial compilation is augmented: `compilation = compilation.AddSyntaxTrees(postInitTrees ∪ preCompilationTrees)`.
4. The syntax store and driver state table are updated with the augmented compilation.
5. Standard source output nodes execute against this augmented compilation.

This means:
- **Within a single generator**: A `RegisterSourceOutput` callback can query the semantic model and see types produced by that same generator's `RegisterPreCompilationSourceOutput`.
- **Across generators**: Generator B's `RegisterSourceOutput` can see types produced by Generator A's `RegisterPreCompilationSourceOutput`.
- **Incremental behavior**: Pre-compilation outputs participate in the standard incremental caching. If the inputs (e.g., additional files) haven't changed, the pre-compilation sources are cached and the compilation is not rebuilt unnecessarily.

## Known Issues and Design Trade-offs

### The Null/Exception Problem

The core challenge with this API is that the `IncrementalValueProvider<T>` / `IncrementalValuesProvider<T>` type system does not distinguish between providers that depend on the compilation and those that do not. A generator author could write:

```csharp
// This compiles fine, but will fail at runtime!
context.RegisterPreCompilationSourceOutput(
    context.CompilationProvider,  // ← compilation is not available in this phase
    (ctx, compilation) =>
    {
        // compilation is null, or this throws an exception
        var type = compilation.GetTypeByMetadataName("Foo");
    });
```

Similarly, syntax node providers (via `CreateSyntaxProvider`) are not available because the syntax store depends on the compilation's semantic model, which does not exist yet at this point in the pipeline.

This is a **runtime failure, not a compile-time error**. The generator code compiles without issue, but the driver will return `null` values or throw exceptions when it attempts to evaluate a compilation-dependent node during the pre-compilation phase.

### Why the Phases Cannot Be Fully Separate

An intuitive solution might be to simply have two completely separate pipeline phases with no shared state: a pre-compilation phase that cannot see the compilation, and a post-compilation phase that can. However, **fully separating the phases would defeat the purpose of the feature**.

In practice, generators need to share intermediate results between the pre-compilation and post-compilation phases. Consider Razor: in the pre-compilation phase, it parses `.razor` files and produces partial declarations. But the parsing work done in that phase — the syntax trees, the extracted component metadata, the parameter lists — is also needed in the post-compilation phase to produce the full implementation. If the phases were completely isolated, the post-compilation phase would have to re-parse every `.razor` file from scratch, duplicating all of that work. This would negate much of the performance benefit the feature is designed to provide.

More generally, the pre-compilation phase often computes information that is a prerequisite for the post-compilation phase. The pre-compilation phase reads inputs and transforms them; the post-compilation phase takes those same transformed results, combines them with compilation data (semantic model, type information), and produces the final output. It is imperative that post-compilation phases can reference values produced during the pre-compilation phase.

This is why the proposed design uses the *same* `IncrementalValueProvider<T>` type for both phases — a value computed in the pre-compilation phase (e.g., parsed Razor syntax trees) can flow naturally into a post-compilation `Combine` with the `CompilationProvider`. The downside is that this same flexibility is what makes it possible to accidentally pass a compilation-dependent provider to `RegisterPreCompilationSourceOutput`.

### Approach 1: Simple API with Runtime Validation (Recommended)

This is the approach taken in the current [prototype implementation](https://github.com/chsienki/roslyn/tree/incremental-generators/precompilation-outputs).

- Add a single API (`RegisterPreCompilationSourceOutput`) with the `[Experimental]` attribute.
- Document that compilation and syntax node providers must not be used as inputs to this API.
- If a generator author mistakenly does so, they will encounter runtime failures (null compilation, exceptions from the syntax store).
- The driver could potentially detect and report a diagnostic for this, but the prototype does not currently enforce it.

**Pros:**
- Minimal API surface — just one new registration method and one new output kind.
- Easy to understand and adopt for generator authors.
- No changes to the existing `IncrementalValueProvider<T>` / `IncrementalValuesProvider<T>` types.
- Ships quickly as an experimental API to gather feedback.

**Cons:**
- Runtime failures are possible if the API is misused.
- No compile-time safety net.

### Approach 2: Segregated Phase Types (Correct-by-Construction)

A more principled approach would introduce a parallel type system for value providers:

- **`PreCompilationValueProvider<T>`** / **`PreCompilationValuesProvider<T>`**: Can only be constructed from non-compilation inputs (additional files, parse options, analyzer config, etc.). Cannot access `CompilationProvider` or `SyntaxProvider`.
- **`IncrementalValueProvider<T>`** / **`IncrementalValuesProvider<T>`** (existing): Continue to have access to all inputs including the compilation and syntax nodes.

The key insight is in how `Combine` operations work across the two type families:

| Left Operand | Right Operand | Result Type |
|---|---|---|
| `PreCompilationValueProvider<A>` | `PreCompilationValueProvider<B>` | `PreCompilationValueProvider<(A, B)>` |
| `PreCompilationValueProvider<A>` | `IncrementalValueProvider<B>` | `IncrementalValueProvider<(A, B)>` |
| `IncrementalValueProvider<A>` | `IncrementalValueProvider<B>` | `IncrementalValueProvider<(A, B)>` |

In other words:
- Combining two pre-compilation providers yields a pre-compilation provider.
- Combining a pre-compilation provider with a post-compilation provider "lifts" the result to post-compilation.
- Combining two post-compilation providers remains post-compilation.

This means `RegisterPreCompilationSourceOutput` would only accept `PreCompilationValueProvider<T>`, and it would be a **compile-time error** to pass a provider that depends on the compilation. If a generator author tries to combine a pre-compilation provider with `CompilationProvider`, the result automatically becomes a regular `IncrementalValueProvider<T>` and can only be used with `RegisterSourceOutput`.

Crucially, the ability to combine pre-compilation providers with post-compilation providers is what allows data to flow across phases. A generator can parse `.razor` files into a `PreCompilationValueProvider<RazorSyntaxTree>`, use it in `RegisterPreCompilationSourceOutput` to emit declarations, and *also* combine it with `CompilationProvider` to produce an `IncrementalValueProvider<(RazorSyntaxTree, Compilation)>` for use in `RegisterSourceOutput` — without re-parsing the files. The type system ensures the combined result is correctly classified as post-compilation, while the intermediate data is shared.

This also makes it clearer in code *when* certain pipeline steps will execute — the type of the provider tells you which phase it belongs to.

**Pros:**
- Correct by construction — impossible to misuse at compile time.
- Self-documenting: the type of a provider tells you its execution phase.
- No runtime exceptions from phase mismatches.

**Cons:**
- Significantly larger API surface: every transform (`Select`, `Where`, `Collect`, `SelectMany`, `Combine`, `WithComparer`, `WithTrackingName`) would need overloads or duplicates for the pre-compilation provider types.
- Combinatorial explosion of `Combine` overloads for the cross-phase cases.
- More complex for generator authors to understand the two type families.
- Breaking change or parallel evolution of the API.

### Recommendation

Given that this is being introduced as an **experimental API**, this document recommends **Approach 1** (simple API with runtime validation) for the initial release. This allows the feature to be evaluated in practice with minimal disruption to the existing API surface. If the feature proves valuable and the runtime safety issue becomes a real pain point, Approach 2 can be pursued in a future iteration with the benefit of real-world usage data to guide the type system design.

## Comparison with Two-Phase Incremental Generators ([#81395](https://github.com/dotnet/roslyn/issues/81395))

Issue #81395 proposes a "Two-Phase Incremental Generators" model with `RegisterDeclarationOutput` and `RegisterImplementationSourceOutput`. While both proposals address the same fundamental problem — enabling cross-generator type visibility — they differ significantly in approach, scope, and trade-offs.

### Shared Goal

Both proposals address the limitation that generator-produced source is not visible to subsequent compilation-dependent phases. Issue #81395 frames this primarily as a cross-generator dependency problem (Generator A's types are invisible to Generator B). This proposal frames it primarily as a performance problem (generators that need to see their own prior output must create expensive intermediate compilations as a workaround). The underlying mechanism is the same: source produced during `RegisterSourceOutput` is not fed back into the compilation.

### Key Differences

| Dimension | `RegisterPreCompilationSourceOutput` | `RegisterDeclarationOutput` (#81395) |
|---|---|---|
| **Number of new APIs** | 1 new registration method | 2 new registration methods (declaration + implementation) |
| **Compilation phases** | **No additional compilation phases** — pre-compilation outputs are added to the existing initial compilation via `AddSyntaxTrees`. The driver still creates the same number of compilations as today. | **One additional compilation phase** — a new intermediate compilation must be created from the initial compilation plus all declarations, forcing a full rebuild of the source symbol graph. |
| **Input model** | Pre-compilation outputs read non-compilation inputs only (additional files, parse options). No compilation or syntax access. | Declaration outputs read the initial compilation (user code only). Can use syntax providers and semantic model. |
| **What gets fed back** | Any source — full type definitions, implementations, anything the generator produces. | Intended for declarations/signatures only (though enforcement of this is an open question in #81395). |
| **Generator awareness** | Generators don't need to split their logic. A single pre-compilation output can emit complete types. | Generators must explicitly split output into declarations (phase 1) and implementations (phase 2). |
| **Cross-generator visibility** | Pre-compilation sources are visible to all generators' standard phases. | Declaration sources are visible to all generators' implementation phases. |
| **Complexity for generator authors** | Low — add one call, ensure inputs don't depend on compilation. | Higher — must understand and correctly split declaration vs. implementation, restructure existing generators. |
| **Scope of change** | Narrow: one API, one output kind, minimal driver changes. | Broad: new execution model, new APIs, new concepts, potential changes to how `RegisterSourceOutput` and `RegisterImplementationSourceOutput` interact. |

### When Each Approach is Better

**`RegisterPreCompilationSourceOutput` is better when:**
- The generation logic doesn't need the compilation (e.g., reading and transforming additional files, config-driven code generation).
- The generator already has an internal multi-compilation workaround that this API can eliminate (e.g., Razor).
- You want to emit complete types that other generators can see, not just signatures.
- Performance is critical — this approach adds no additional compilation phases.
- You want minimal disruption to existing generator patterns.

**`RegisterDeclarationOutput` (#81395) is better when:**
- The generation logic *does* need the compilation and semantic model to determine what to declare (e.g., finding types annotated with attributes, analyzing inheritance hierarchies).
- You want a formal separation between "what types exist" (declarations) and "how they're implemented" (implementations), potentially enabling better incremental invalidation.
- You're willing to accept the cost of an additional compilation phase for a more structured execution model.

### Complementary, Not Competing

These two proposals are not mutually exclusive. They could coexist in the same pipeline:

```
1. RegisterPostInitializationOutput      → Constant source, no inputs
2. RegisterPreCompilationSourceOutput    → Non-compilation inputs, source added to compilation
3. RegisterDeclarationOutput (future)    → Compilation inputs, declarations added to compilation
4. RegisterSourceOutput                  → Full compilation, final output
5. RegisterImplementationSourceOutput    → Full compilation, implementation-only output
```

### Performance: Compilation Phase Cost

The most significant practical difference between these proposals is their cost in terms of compilation phases.

As noted in the [discussion on #81395](https://github.com/dotnet/roslyn/issues/81395#issuecomment-3562700994), **each additional compilation is expensive**. Creating a new compilation forces an entirely new source symbol graph — all symbols are recreated, nothing is shared from the previous compilation, and all binding must be redone. This is the single most expensive operation in the compiler pipeline.

`RegisterPreCompilationSourceOutput` **does not add any compilation phases**. The pre-compilation sources are added to the initial compilation at the same point where post-init sources are added today (via `AddSyntaxTrees`). The driver still produces exactly two compilations: the initial compilation (now augmented with pre-compilation sources) and the final compilation (with all generator outputs). The feature simply runs more *steps* within the existing phases. This makes it extremely cheap — the cost is only the generator's own logic to produce the pre-compilation sources.

`RegisterDeclarationOutput` (#81395) **requires an additional compilation phase**: the initial compilation must be rebuilt with all declaration sources to produce an enriched intermediate compilation, which is then handed to the implementation phase. This means three compilations total: initial, enriched (with declarations), and final (with implementations). For generators like Razor that already create their own intermediate compilations as a workaround, this may not add net cost, but for the general case it represents a significant overhead that the pre-compilation approach avoids entirely.

## Prototype Implementation

A working prototype is available at: [chsienki/roslyn@incremental-generators/precompilation-outputs](https://github.com/chsienki/roslyn/tree/incremental-generators/precompilation-outputs)

The key changes in the prototype are:

1. **`IncrementalGeneratorInitializationContext`** — Two new `RegisterPreCompilationOutput` overloads (for `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>`). These delegate to the existing `RegisterSourceOutput` infrastructure with the `PreCompilation` output kind.

2. **`IncrementalGeneratorOutputKind`** — New `PreCompilation = 0b10000` flag.

3. **`GeneratorState`** — New `PreCompilationTrees` property (analogous to `PostInitTrees`) to track the trees produced by pre-compilation outputs. New `WithPreCompilations` method to update state.

4. **`GeneratorDriver.RunGeneratorsCore`** — Two-pass execution: first, pre-compilation output nodes are evaluated and their sources are parsed and added to the compilation. Then the compilation and syntax store are updated, and standard output nodes execute against the augmented compilation.

5. **`SourceOutputNode`** — Now accepts `IncrementalGeneratorOutputKind.PreCompilation` in addition to `Source` and `Implementation`.

The prototype includes two tests:
- **`IncrementalGenerator_Can_Add_PreCompilationSource`** — Verifies that a pre-compilation output adds a syntax tree to the output compilation.
- **`IncrementalGenerator_Can_Add_PreCompilationSource_And_SeeItInCompilation`** — Verifies that a subsequent `RegisterSourceOutput` can see the pre-compilation source in the compilation (i.e., `compilation.SyntaxTrees.Count()` reflects the added tree).

## Open Questions

1. **Naming**: Should it be `RegisterPreCompilationSourceOutput` (for consistency with `RegisterSourceOutput`) or `RegisterPreCompilationOutput` (as in the prototype)? Should the output kind be `PreCompilation` or `PreCompilationSource`?

2. **Diagnostics**: Should pre-compilation outputs be able to report diagnostics? The prototype uses `SourceProductionContext` which supports this, but should the driver do anything special with them?

3. **Incremental stepping**: How should pre-compilation outputs appear in incremental step tracking (`TrackIncrementalSteps`)? Should they have their own step name distinct from `SourceOutput`?

4. **Disabled output kinds**: The `GeneratorDriver` allows disabling output kinds via `IncrementalGeneratorOutputKind`. The new `PreCompilation` flag participates in this, but should hosts have a reason to disable it?

5. **Timing**: Should pre-compilation output execution time be reported separately from standard output time?

6. **Error handling**: If a pre-compilation output throws, should it prevent the standard phase from running? What compilation do subsequent phases see?

7. **IDE interaction**: How should the IDE treat pre-compilation outputs? Should they be treated like post-init (always run eagerly) or more like standard outputs (potentially deferred)?

## Related Issues and Documents

- [#81395 — Two-Phase Incremental Generators for Cross-Generator Dependencies](https://github.com/dotnet/roslyn/issues/81395)
- [#53632 — Make AnalyzerConfigOptions available during PostInitialization in SourceGenerators](https://github.com/dotnet/roslyn/issues/53632)
- [#57589 — Earlier two-phase proposal discussion](https://github.com/dotnet/roslyn/issues/57589)
- [Incremental Generators Design Document](incremental-generators.md)
- [Source Generators Cookbook](source-generators.cookbook.md)
