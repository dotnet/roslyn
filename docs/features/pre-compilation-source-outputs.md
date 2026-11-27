# Pre-Compilation Source Outputs

## Summary

This document describes a new API, `RegisterPreCompilationSourceOutput`, for the incremental source generator pipeline. This API allows generators to produce source that is added to the *initial* compilation - before any compilation-dependent phases execute - while still being able to read from non-compilation input providers such as `AdditionalTextsProvider` and `ParseOptionsProvider`.

This bridges the gap between `RegisterPostInitializationOutput` (which takes no inputs at all) and `RegisterSourceOutput` (which has access to the full compilation), enabling scenarios where generated source must be visible to subsequent generator phases and other generators, but the generation logic itself only depends on non-compilation inputs.

## Motivation

Today, the incremental generator pipeline has a clear ordering:

1. **`RegisterPostInitializationOutput`** - Runs first, before the compilation is available. Takes no inputs. Source is added to the initial compilation.
2. **`RegisterSourceOutput` / `RegisterImplementationSourceOutput`** - Runs after the compilation (including post-init sources) is available. Can access the full compilation, syntax trees, and semantic model.

This creates a gap: if a generator needs to produce source that should be visible to later phases (its own or other generators'), but the generation logic depends on non-compilation inputs like additional files or parse options, there is no appropriate API.

### Primary Motivation: Razor Performance

The primary motivation for this API is a significant performance improvement for the Razor source generator.

Razor has **cross-file dependencies**: a `.razor` file can reference components declared in other `.razor` files. To resolve these references, the Razor generator must compile in two phases internally:

1. **Declaration collection**: Parse all `.razor` files and collect the partial type declarations (component names, parameters, etc.).
2. **Binding and implementation**: Use those declarations to perform semantic analysis - resolving component references, validating parameter types, generating the full implementation.

Today, because `RegisterSourceOutput` does not feed source back into the compilation, Razor must perform this two-phase process **entirely within its own generator**. Concretely, Razor:

1. Creates a **copy** of the compilation.
2. Adds the partial declarations into this copied compilation.
3. Performs binding against the copied compilation to discover cross-file information.
4. **Throws the copied compilation away**.
5. Produces its final output against the original compilation.

This means the system creates **three compilations** during a generator run: the initial compilation (pre-SG), Razor's private intermediate compilation (for binding), and the final compilation (with SG outputs). The intermediate compilation is pure overhead - it duplicates all the work of building a compilation just so Razor can ask semantic questions about its own declarations.

With `RegisterPreCompilationSourceOutput`, Razor can instead:

1. Use `RegisterPreCompilationSourceOutput` to emit partial declarations from `.razor` files. These are added to the **initial** compilation by the driver.
2. Use `RegisterSourceOutput` with the `CompilationProvider` to perform binding against the initial compilation - which now already contains the declarations. The generator produces the remaining implementation parts as standard source output.

In this model, the system creates only **two compilations**: the initial compilation (pre-SG, augmented with pre-compilation sources) and the final compilation (with all SG outputs). Razor no longer needs its private intermediate compilation. Early experiments show this yields a **roughly 50% performance improvement** for Razor source generation.

Critically, this improvement comes without adding any new compilation phases to the driver. The pre-compilation outputs are simply additional steps that run before the compilation is finalized - the total number of *compilation* objects created by the driver remains the same as today (the initial compilation is augmented in-place with `AddSyntaxTrees`, not rebuilt from scratch).

### Additional Scenarios

**Cross-Generator Type Visibility**: As a secondary benefit, pre-compilation sources are visible to *all* generators' standard phases, not just the generator that produced them. A generator that reads `.proto` files and emits C# types via `RegisterPreCompilationSourceOutput` makes those types available to other generators that perform binding in their `RegisterSourceOutput` phase. This enables cross-generator interoperability without requiring generators to know about each other.

**Configurable Post-Initialization**: Generator authors want to emit attributes or other marker types during post-initialization, but need to read `AnalyzerConfigOptions` or additional files to determine *which* sources to emit (see [#53632](https://github.com/dotnet/roslyn/issues/53632)). `RegisterPostInitializationOutput` cannot accept any inputs, forcing authors to either emit all possible sources unconditionally or split their generator into multiple packages. `RegisterPreCompilationSourceOutput` addresses this directly: the generated source is added to the initial compilation just like post-init source, but the generator can read parse options, analyzer config options, and additional files to decide what to emit.

## API

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
    /// Attempting to access the compilation or syntax trees during this phase will throw
    /// an <see cref="InvalidOperationException"/>.
    /// </remarks>
    [Experimental(RoslynExperiments.PreCompilationSourceOutput)]
    public void RegisterPreCompilationSourceOutput<TSource>(
        IncrementalValueProvider<TSource> source,
        Action<PreCompilationSourceProductionContext, TSource> action);

    [Experimental(RoslynExperiments.PreCompilationSourceOutput)]
    public void RegisterPreCompilationSourceOutput<TSource>(
        IncrementalValuesProvider<TSource> source,
        Action<PreCompilationSourceProductionContext, TSource> action);
}
```

The production context is a new type, distinct from `SourceProductionContext`:

```csharp
[Experimental(RoslynExperiments.PreCompilationSourceOutput)]
public readonly struct PreCompilationSourceProductionContext
{
    public CancellationToken CancellationToken { get; }

    public void AddSource(string hintName, string source);
    public void AddSource(string hintName, SourceText sourceText);
}
```

`PreCompilationSourceProductionContext` intentionally does **not** include `ReportDiagnostic`. Pre-compilation is an early phase focused purely on producing source; diagnostic reporting should be done in a separate analyzer.

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
        // messageType is non-null - it was generated in the pre-compilation phase
        ctx.AddSource("Serializers.g.cs", GenerateSerializer(messageType!));
    });
}
```

## Execution Model

The updated pipeline execution order is:

```
1. RegisterPostInitializationOutput
   +-- Source added to initial compilation (takes no inputs)

2. RegisterPreCompilationSourceOutput          <- NEW
   +-- Reads non-compilation inputs (additional files, parse options, etc.)
   +-- Source added to initial compilation
   +-- Compilation is rebuilt with new sources

3. RegisterSourceOutput / RegisterImplementationSourceOutput
   +-- Reads full compilation (now includes post-init AND pre-compilation sources)
   +-- Source is part of final output but not fed back into compilation
```

Concretely, in the generator driver:

1. Post-initialization sources are collected as today.
2. A `DriverStateTable.Builder` is created **without** the compilation or syntax store - these are not yet available.
3. Pre-compilation output nodes are evaluated for all generators. Their sources are parsed into syntax trees.
4. The initial compilation is augmented: `compilation = compilation.AddSyntaxTrees(preCompilationTrees)`.
5. `DriverStateTable.Builder.SetCompilation` is called, which stores the compilation and creates the `SyntaxStore.Builder` internally.
6. Standard source output nodes execute against the augmented compilation.

This means:
- **Within a single generator**: A `RegisterSourceOutput` callback can query the semantic model and see types produced by that same generator's `RegisterPreCompilationSourceOutput`.
- **Across generators**: Generator B's `RegisterSourceOutput` can see types produced by Generator A's `RegisterPreCompilationSourceOutput`.
- **Incremental behavior**: Pre-compilation outputs participate in the standard incremental caching. If the inputs (e.g., additional files) haven't changed, the pre-compilation sources are cached and the compilation is not rebuilt unnecessarily.

## Phase Enforcement

The `IncrementalValueProvider<T>` / `IncrementalValuesProvider<T>` type system does not distinguish between providers that depend on the compilation and those that do not. A generator author could write:

```csharp
// This compiles fine, but will fail at runtime!
context.RegisterPreCompilationSourceOutput(
    context.CompilationProvider,  // <- compilation is not available in this phase
    (ctx, compilation) =>
    {
        // This code never executes - an exception is thrown when evaluating the provider
    });
```

To catch this, the `DriverStateTable.Builder` does **not** have the `Compilation` or `SyntaxStore` set during the pre-compilation phase. Accessing either property throws an `InvalidOperationException`. This exception is thrown from the `DriverStateTable.Builder` property getter, caught by `InputNode.UpdateStateTable` (which wraps it in a `UserFunctionException`), and handled by the pre-compilation error handler in the driver.

When a pre-compilation output fails (whether from accessing compilation-dependent inputs or from any other exception), the generator is placed in **error state**: a diagnostic is reported, and the generator's standard phase is **skipped entirely**. Other generators are unaffected - their pre-compilation and standard phases continue to execute normally.

### Why Not Compile-Time Safety?

A more principled approach would introduce a parallel type system (`PreCompilationValueProvider<T>` / `PreCompilationValuesProvider<T>`) that prevents compilation-dependent providers from being passed to `RegisterPreCompilationSourceOutput` at compile time. However, this would require duplicating every transform operation (`Select`, `Where`, `Collect`, `SelectMany`, `Combine`, `WithComparer`, `WithTrackingName`) for the pre-compilation provider types, creating a significantly larger API surface. Given that this is an experimental API, runtime enforcement with clear error messages is sufficient for the initial release. If the feature proves valuable and the runtime safety issue becomes a real pain point, a compile-time safe type system can be pursued in a future iteration.

## Incremental Step Tracking

Pre-compilation outputs have a distinct step name: `WellKnownGeneratorOutputs.PreCompilationSourceOutput` (value `"PreCompilationSourceOutput"`). This is separate from `SourceOutput` and `ImplementationSourceOutput`, allowing tools to distinguish pre-compilation steps in incremental step tracking output.

Per-generator `GeneratorRunStateTable.Builder` instances are shared across both the pre-compilation and standard passes, so all steps for a generator (both pre-compilation and standard) appear in a unified view.

Pre-compilation sources appear in `GeneratorRunResult.GeneratedSources` alongside post-init and standard sources.

## Parse Options Reparse

Like post-initialization trees, pre-compilation trees are reparsed when parse options change between driver runs. A unified `RequiresConstantTreeReparse` check handles both post-init and pre-compilation trees in a single pass.

## Implementation Architecture

The output node hierarchy uses an abstract base class to share logic between source output kinds:

- **`AbstractSourceOutputNode<TInput>`** - Contains all shared logic: `UpdateStateTable` (caching, table building, step tracking), `AppendOutputs`, and interface boilerplate. Defines abstract members `Kind`, `StepName`, and `InvokeUserAction`.
- **`SourceOutputNode<TInput>`** - Sealed subclass for `Source` and `Implementation` output kinds. Creates a `SourceProductionContext` with the compilation and diagnostics.
- **`PreCompilationSourceOutputNode<TInput>`** - Sealed subclass for `PreCompilation` output kind. Creates a `PreCompilationSourceProductionContext` without the compilation or diagnostics.

## Comparison with Two-Phase Incremental Generators ([#81395](https://github.com/dotnet/roslyn/issues/81395))

Issue #81395 proposes a "Two-Phase Incremental Generators" model with `RegisterDeclarationOutput` and `RegisterImplementationSourceOutput`. While both proposals address the same fundamental problem - enabling cross-generator type visibility - they differ significantly in approach, scope, and trade-offs.

### Shared Goal

Both proposals address the limitation that generator-produced source is not visible to subsequent compilation-dependent phases. Issue #81395 frames this primarily as a cross-generator dependency problem (Generator A's types are invisible to Generator B). This proposal frames it primarily as a performance problem (generators that need to see their own prior output must create expensive intermediate compilations as a workaround). The underlying mechanism is the same: source produced during `RegisterSourceOutput` is not fed back into the compilation.

### Key Differences

| Dimension | `RegisterPreCompilationSourceOutput` | `RegisterDeclarationOutput` (#81395) |
|---|---|---|
| **Number of new APIs** | 1 new registration method | 2 new registration methods (declaration + implementation) |
| **Compilation phases** | **No additional compilation phases** - pre-compilation outputs are added to the existing initial compilation via `AddSyntaxTrees`. The driver still creates the same number of compilations as today. | **One additional compilation phase** - a new intermediate compilation must be created from the initial compilation plus all declarations, forcing a full rebuild of the source symbol graph. |
| **Input model** | Pre-compilation outputs read non-compilation inputs only (additional files, parse options). No compilation or syntax access. | Declaration outputs read the initial compilation (user code only). Can use syntax providers and semantic model. |
| **What gets fed back** | Any source - full type definitions, implementations, anything the generator produces. | Intended for declarations/signatures only (though enforcement of this is an open question in #81395). |
| **Generator awareness** | Generators don't need to split their logic. A single pre-compilation output can emit complete types. | Generators must explicitly split output into declarations (phase 1) and implementations (phase 2). |
| **Cross-generator visibility** | Pre-compilation sources are visible to all generators' standard phases. | Declaration sources are visible to all generators' implementation phases. |
| **Complexity for generator authors** | Low - add one call, ensure inputs don't depend on compilation. | Higher - must understand and correctly split declaration vs. implementation, restructure existing generators. |
| **Scope of change** | Narrow: one API, one output kind, minimal driver changes. | Broad: new execution model, new APIs, new concepts, potential changes to how `RegisterSourceOutput` and `RegisterImplementationSourceOutput` interact. |

### When Each Approach is Better

**`RegisterPreCompilationSourceOutput` is better when:**
- The generation logic doesn't need the compilation (e.g., reading and transforming additional files, config-driven code generation).
- The generator already has an internal multi-compilation workaround that this API can eliminate (e.g., Razor).
- You want to emit complete types that other generators can see, not just signatures.
- Performance is critical - this approach adds no additional compilation phases.
- You want minimal disruption to existing generator patterns.

**`RegisterDeclarationOutput` (#81395) is better when:**
- The generation logic *does* need the compilation and semantic model to determine what to declare (e.g., finding types annotated with attributes, analyzing inheritance hierarchies).
- You want a formal separation between "what types exist" (declarations) and "how they're implemented" (implementations), potentially enabling better incremental invalidation.
- You're willing to accept the cost of an additional compilation phase for a more structured execution model.

### Complementary, Not Competing

These two proposals are not mutually exclusive. They could coexist in the same pipeline:

```
1. RegisterPostInitializationOutput      -> Constant source, no inputs
2. RegisterPreCompilationSourceOutput    -> Non-compilation inputs, source added to compilation
3. RegisterDeclarationOutput (future)    -> Compilation inputs, declarations added to compilation
4. RegisterSourceOutput                  -> Full compilation, final output
5. RegisterImplementationSourceOutput    -> Full compilation, implementation-only output
```

### Performance: Compilation Phase Cost

The most significant practical difference between these proposals is their cost in terms of compilation phases.

As noted in the [discussion on #81395](https://github.com/dotnet/roslyn/issues/81395#issuecomment-3562700994), **each additional compilation is expensive**. Creating a new compilation forces an entirely new source symbol graph - all symbols are recreated, nothing is shared from the previous compilation, and all binding must be redone. This is the single most expensive operation in the compiler pipeline.

`RegisterPreCompilationSourceOutput` **does not add any compilation phases**. The pre-compilation sources are added to the initial compilation at the same point where post-init sources are added today (via `AddSyntaxTrees`). The driver still produces exactly two compilations: the initial compilation (now augmented with pre-compilation sources) and the final compilation (with all generator outputs). The feature simply runs more *steps* within the existing phases. This makes it extremely cheap - the cost is only the generator's own logic to produce the pre-compilation sources.

`RegisterDeclarationOutput` (#81395) **requires an additional compilation phase**: the initial compilation must be rebuilt with all declaration sources to produce an enriched intermediate compilation, which is then handed to the implementation phase. This means three compilations total: initial, enriched (with declarations), and final (with implementations). For generators like Razor that already create their own intermediate compilations as a workaround, this may not add net cost, but for the general case it represents a significant overhead that the pre-compilation approach avoids entirely.

## Related Issues and Documents

- [#81395 - Two-Phase Incremental Generators for Cross-Generator Dependencies](https://github.com/dotnet/roslyn/issues/81395)
- [#53632 - Make AnalyzerConfigOptions available during PostInitialization in SourceGenerators](https://github.com/dotnet/roslyn/issues/53632)
- [#57589 - Earlier two-phase proposal discussion](https://github.com/dotnet/roslyn/issues/57589)
- [Incremental Generators Design Document](incremental-generators.md)
- [Source Generators Cookbook](source-generators.cookbook.md)
- [Implementation PR](https://github.com/dotnet/roslyn/pull/83088)
- [API Review Issue](https://github.com/dotnet/roslyn/issues/83089)
