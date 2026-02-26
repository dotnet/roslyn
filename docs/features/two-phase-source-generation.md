# Two-Phase Source Generation

## Summary

This feature introduces a **declaration phase** to the incremental source generator pipeline, allowing generators to produce types and declarations that are visible to _other_ generators before their main source/implementation outputs run. This solves the long-standing problem that multiple source generators in the same project cannot see each other's output.

Related: [dotnet/roslyn#81395](https://github.com/dotnet/roslyn/issues/81395)

## Motivation

Today, when multiple `IIncrementalGenerator` implementations are registered on the same project, each generator runs against the **original compilation** — none of them can see the types or source produced by any other generator. There is no defined ordering between generators, and the compiler makes no guarantee about execution order.

This is a real limitation for generator ecosystems. Consider:

- **Generator A** produces an `INotifyPropertyChanged` implementation with observable properties for classes annotated with `[ObservableProperty]`.
- **Generator B** wants to discover all observable properties and generate data-binding glue code.

Today, Generator B cannot see the types produced by Generator A. Users must resort to workarounds like multi-pass compilation, intermediate files, or tightly coupling generators.

Roslyn already has a precedent for phased execution:

| API | When it runs |
|-----|-------------|
| `RegisterPostInitializationOutput` | Before any pipeline execution; output is added to the compilation before generators run |
| `RegisterSourceOutput` | Main pipeline execution |
| `RegisterImplementationSourceOutput` | Main pipeline execution, but output is excluded from design-time builds |

This feature adds a new phase between `PostInit` and `Source`:

| API | Phase | Sees declarations? |
|-----|-------|---------------------|
| `RegisterPostInitializationOutput` | Phase 0 (pre-pipeline, constant) | N/A |
| **`RegisterDeclarationOutput`** | **Phase 1 (declaration)** | No (original compilation only) |
| `RegisterSourceOutput` | Phase 2 (source) | **No** (original compilation) |
| `RegisterImplementationSourceOutput` | Phase 3 (implementation) | **Yes** (enriched compilation) |

## Detailed Design

### New Public API

```csharp
// New enum value
public enum IncrementalGeneratorOutputKind
{
    // ... existing values ...
    Declaration = 0b10000,   // = 16
}

// New methods on IncrementalGeneratorInitializationContext
public void RegisterDeclarationOutput<TSource>(
    IncrementalValueProvider<TSource> source,
    Action<SourceProductionContext, TSource> action);

public void RegisterDeclarationOutput<TSource>(
    IncrementalValuesProvider<TSource> source,
    Action<SourceProductionContext, TSource> action);

// New constant
public static class WellKnownGeneratorOutputs
{
    public const string DeclarationOutput = "DeclarationOutput";
}
```

### Execution Model

The `GeneratorDriver.RunGeneratorsCore` method now executes in three phases:

```
┌─────────────────────────────────────────────────┐
│  Original Compilation (user source + PostInit)  │
└──────────────────────┬──────────────────────────┘
                       │
        ┌──────────────▼──────────────┐
        │       PHASE 1               │
        │  Run Declaration outputs    │
        │  for ALL generators         │
        │  against original           │
        │  compilation                │
        └──────────────┬──────────────┘
                       │
        ┌──────────────▼──────────────┐
        │  Enriched Compilation       │
        │  = Original + all Phase 1   │
        │    declaration trees        │
        └──────────┬─────────┬────────┘
                   │         │
    ┌──────────────▼───┐ ┌───▼──────────────┐
    │    PHASE 2       │ │    PHASE 3       │
    │  Source + Host   │ │  Implementation  │
    │  outputs against │ │  outputs against │
    │  ORIGINAL        │ │  ENRICHED        │
    │  compilation     │ │  compilation     │
    └──────────────────┘ └─────────────────-┘
```

#### Phase 1 — Declaration

1. For each generator, the driver checks whether any output nodes have `Kind == IncrementalGeneratorOutputKind.Declaration`.
2. If so, a `DriverStateTable.Builder` is created against the **original compilation** (plus PostInit trees).
3. Only `Declaration`-kind output nodes are executed via `UpdateOutputs`.
4. The resulting source files are parsed into syntax trees and stored in `GeneratorState.DeclarationTrees`.
5. All declaration trees from all generators are collected into a single list.

#### Compilation Enrichment

After Phase 1 completes for all generators, the driver calls:

```csharp
compilation = compilation.AddSyntaxTrees(allDeclarationTrees);
```

This produces an **enriched compilation** that contains the original source, PostInit trees, and all Phase 1 declaration trees.

#### Phase 2 — Source and Host

A `DriverStateTable.Builder` is created against the **original compilation** (without declaration trees). `Source` and `Host` outputs run here. These outputs do **not** see declaration trees from other generators — they see the same compilation as they would without the declaration feature.

#### Phase 3 — Implementation

A separate `DriverStateTable.Builder` is created against the **enriched compilation**. `Implementation` outputs run here. Because the compilation includes declaration trees, any generator's `CompilationProvider` in an `RegisterImplementationSourceOutput` callback will see types declared by other generators in Phase 1.

This design is intentional: `RegisterSourceOutput` defines the **public API surface** visible at design time, while `RegisterImplementationSourceOutput` provides runtime implementation details. Cross-generator visibility belongs in the implementation phase to avoid design-time coupling between generators.

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Phase 1 generators see only the original compilation | Prevents circular dependencies between declaration outputs. Generator A's declarations cannot depend on Generator B's declarations. |
| Only Implementation outputs see Phase 1 declarations | `RegisterSourceOutput` defines the design-time API surface. Cross-generator visibility is intentionally limited to `RegisterImplementationSourceOutput` to avoid design-time coupling. |
| Source and Host outputs see the original compilation | Ensures design-time builds are not affected by other generators' declarations. Source outputs remain deterministic and independent. |
| Declaration outputs reuse `SourceOutputNode<T>` | Minimizes code duplication. The existing node infrastructure handles caching, cancellation, and step tracking. Only the `Kind` flag differs. |
| Declaration trees are stored separately in `GeneratorState` | Allows independent tracking and the `WithDeclarationTrees()` update pattern, consistent with how `PostInitTrees` and `GeneratedTrees` are handled. |
| Declaration trees are included in `GetRunResult()` | Ensures tooling and tests can observe all generated sources, including declarations. |

### Impact on Existing Generators

- **No breaking changes.** Generators that do not call `RegisterDeclarationOutput` behave identically to before. Phase 1 is a no-op if no generator has declaration outputs.
- The `Declaration` output kind can be disabled via `GeneratorDriverOptions.DisabledOutputs`, consistent with how `Source` and `Implementation` can be disabled.
- Existing `IncrementalGeneratorOutputKind` flag combinations continue to work as before.

## Usage Example

### Generator A: Produces observable property types (Phase 1)

```csharp
[Generator]
public class ObservablePropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Phase 1: Declare the generated types so other generators can see them
        var classes = context.SyntaxProvider
            .ForAttributeWithMetadataName("ObservablePropertyAttribute", ...);

        context.RegisterDeclarationOutput(classes, (ctx, model) =>
        {
            // Emit the partial class with INotifyPropertyChanged
            ctx.AddSource($"{model.ClassName}.g.cs", GenerateObservableClass(model));
        });
    }
}
```

### Generator B: Consumes types from Generator A (Phase 3 — Implementation)

```csharp
[Generator]
public class AutoBinderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Phase 3 (Implementation): Can see types generated by ObservablePropertyGenerator
        var observableTypes = context.CompilationProvider.Select((comp, ct) =>
            comp.GetSymbolsWithName(s => true, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .Where(t => t.Interfaces.Any(i => i.Name == "INotifyPropertyChanged"))
                .ToList());

        // Must use RegisterImplementationSourceOutput to see declaration outputs
        context.RegisterImplementationSourceOutput(observableTypes, (ctx, types) =>
        {
            // Generate binding code for each observable type
            ctx.AddSource("Bindings.g.cs", GenerateBindings(types));
        });
    }
}
```

> **Note:** Using `RegisterSourceOutput` instead of `RegisterImplementationSourceOutput` would **not** see the types declared in Phase 1. This is by design — source outputs define the public API surface and should not depend on other generators' declarations.

## Files Changed

| File | Change |
|------|--------|
| `src/Compilers/Core/Portable/SourceGeneration/Nodes/IIncrementalGeneratorOutputNode.cs` | Added `Declaration = 0b10000` to `IncrementalGeneratorOutputKind` enum |
| `src/Compilers/Core/Portable/SourceGeneration/WellKnownGeneratorOutputs.cs` | Added `DeclarationOutput` constant |
| `src/Compilers/Core/Portable/SourceGeneration/Nodes/SourceOutputNode.cs` | Updated `Debug.Assert` and step name to handle `Declaration` kind |
| `src/Compilers/Core/Portable/SourceGeneration/IncrementalContexts.cs` | Added two `RegisterDeclarationOutput<TSource>()` overloads |
| `src/Compilers/Core/Portable/SourceGeneration/GeneratorState.cs` | Added `DeclarationTrees` property, `WithDeclarationTrees()`, `RequiresDeclarationReparse()`; updated all constructors |
| `src/Compilers/Core/Portable/SourceGeneration/GeneratorDriver.cs` | Two-phase execution in `RunGeneratorsCore`; updated `RunGeneratorsAndUpdateCompilation` and `GetRunResult` to include declaration trees |
| `src/Compilers/Core/Portable/PublicAPI.Unshipped.txt` | 4 new public API entries |

## Test Coverage

10 new tests in `GeneratorDriverTests.cs`:

| Test | What it validates |
|------|-------------------|
| `Declaration_Output_Is_Added_To_Compilation` | Single generator declares a type; the type exists in the output compilation |
| `Declaration_Output_Visible_To_Other_Generator_Implementation_Output` | **Key test:** Generator 1 declares `GeneratedType` in Phase 1; Generator 2's `RegisterImplementationSourceOutput` sees it via `CompilationProvider` |
| `Declaration_Output_Not_Visible_To_Other_Generator_Source_Output` | Generator 2's `RegisterSourceOutput` does **not** see Generator 1's Phase 1 declarations |
| `Declaration_Output_Not_Visible_To_Own_Declaration_Output` | Phase 1 generators cannot see other Phase 1 generators' declarations (prevents cycles) |
| `Declaration_And_Implementation_Same_Generator` | A single generator uses both `RegisterDeclarationOutput` and `RegisterImplementationSourceOutput` |
| `Multiple_Generators_With_Declarations` | Three generators each declare types; each sees the others' declarations in `RegisterImplementationSourceOutput` |
| `Declaration_Output_Can_Be_Disabled` | `Declaration` kind can be disabled via `GeneratorDriverOptions.DisabledOutputs` |
| `Existing_Generators_Unaffected_By_Declaration_Phase` | Generators without declarations work identically |
| `Declaration_Output_Tracked_In_Steps` | Step tracking includes declaration outputs |
| `Declaration_Sources_Included_In_Run_Result` | `GetRunResult()` includes declaration sources |

Additionally, the existing `Generator_Output_Kinds_Can_Be_Disabled` test was updated to include `Declaration` as an `[InlineData]` case.

## Future Considerations

- **Incremental caching**: Changes to declaration outputs should invalidate Phase 2 results. The current implementation re-runs both phases; a future optimization could track declaration tree identity to skip Phase 2 when declarations haven't changed.
- **VB support**: `VisualBasicGeneratorDriver` should receive the same two-phase logic (currently only `CSharpGeneratorDriver` is affected via the shared `GeneratorDriver` base class).
- **Performance**: The additional compilation creation (`AddSyntaxTrees`) in Phase 1 has a cost. For projects with many generators, profiling may be needed.
- **IDE integration**: Design-time builds already understand `Implementation` vs `Source` for performance. Declaration outputs should be treated similarly to `Source` (included in design-time builds) since they define the API surface.
- **Ordering within Phase 1**: Currently, Phase 1 generators run independently and cannot see each other's declarations. A future extension could introduce ordering hints or multiple declaration phases, but this significantly increases complexity.
