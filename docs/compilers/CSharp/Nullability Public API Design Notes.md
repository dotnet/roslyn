# Nullability API Design Notes

## Guiding Principles

When designing this API, there were a few core principles that we tried to keep in mind:

1. First and foremost, we do not want to break existing analyzers and refactorings.
This means that common patterns that used to work need to continue to work.
These include reference equality for `ITypeSymbol`s that aren't generic, such as `string`.
2. We'd like the ability to avoid computing nullability information if it's not required.
Currently, the `SemanticModel` APIs do a good job of only causing the minimal amount of binding required to answer the question being asked, but attempting to compute nullability information will cause significantly more to be bound.
To fully determine nullability, we must bind the entire method and run nullability analysis up to the point requested by the caller, unlike today where individual statements can get away with being bound by themselves.
Even if the standard path calculates nullability info, we feel that some scenarios will be both perf sensitive and not care about results of nullability analysis.
This includes features like intellisense.
3. Users should be able to get at both declared nullability for variables, as well as seeing the inferred nullability at each expression from flow analysis.

## General Concepts

Nullability information is exposed publicly as two enums, `NullableAnnotation` and `NullableFlowState`.

`NullableAnnotation` represents lvalues. There are 4 possible values:

* `NotApplicable` - This is used when nullability information is requested about something that does not have the concept of nullability, such as a statement, directive, or other similar constructs.
If we decide to expose a version of `SemanticModel` that does not compute nullability information for certain perf-sensitive scenarios, all annotation information returned would be `NotApplicable`.
* `Disabled` - This value was defined in a nullable-disabled context.
* `NotAnnotated` - This value was defined in a nullable-enabled context, and was not annotated in source (or in type inference scenarios, was inferred to be a not annotated type).
* `Annotated` - This value was defined in a nullable-enabled context, and was annotated with a `?` in source (or in type inference scenarios, was inferred to be a annotated type).

`NullableFlowState` represents rvalues. There are 3 possible values:

* `NotApplicable` - This is used when nullability information is requested about something that does not have the concept of nullability, such as a statement, directive, or other similar constructs.
If we decide to expose a version of `SemanticModel` that does not compute nullability information for certain perf-sensitive scenarios, all annotation information returned would be `NotApplicable`.
* `MaybeNull` - The nullable analysis of the compiler has determined that this value could be null.
* `NotNull` - The nullable analysis of the compiler has determined that this value is not null.

Nullability information will be retrieved from the `SemanticModel`, much like type information.
`GetSemanticModel` will return a `SemanticModel` that is aware of nullability information, and will force binding and calculation of nullability information when queried.
We will potentially offer an overload of `GetSemanticModel()`, `GetSemanticModel(bool skipNullabilityInformation)`, which will return a `SemanticModel` that does not calculate nullability information for performance sensitive scenarios.
This `SemanticModel` will share a cache with the nullable-aware `SemanticModel`, and all nullabilities accessed on symbols retrieved from this `SemanticModel` will be `NotAnalyzed`.
This API will only be added if, after dogfooding, we determine that the performance of any scenarios is bad enough to warrant the introduction of the API.

Generally speaking, when you want to determine the nullability of an `ITypeSymbol`, you need to check the containing context.
The declaring symbol (e.g. `ILocalSymbol`, `IFieldSymbol`, etc.) will have the `NullableAnnotation` of the type.
`GetTypeInfo` will give you the `ITypeSymbol`, the `NullableAnnotation` (if this expression could be used as an lvalue), and the `NullableFlowState` of an expression (if this expression can be used as an rvalue).
Nested `NullableAnnotation`s for type parameters are contained on the containing `ITypeSymbol`.
For example, if you wanted to know whether an array's contents are nullable, you will need to check the `IArrayTypeSymbol` for `ElementNullableAnnotation`.

Value types are always considered `NotNull`.
Nullable value types and reference types have their state tracked, and are either `NotNull` or `MaybeNull` depending on flow state.
Unconstrained generic parameters are being tracked.
When queried with `GetSymbolInfo`/`GetDeclaredSymbol`, variables/parameters/fields/etc of unconstrained type parameters have a `NullableAnnotation` of `NotAnnotated`.
When queried with `GetTypeInfo`, they have a flow state of `MaybeNull` if they have not yet been checked for null and directly or indirectly assigned a non-null value, and `NotNull` after they have been checked for null and have not been directly or indirectly assigned a `MaybeNull` value.

### Declared Nullability

Declared nullability is the nullability that was (usually) explicitly typed in the source code by the programmer.
This is always represented by the `NullableAnnotation` enum.
There are some cases where this is inferred, such as lambda arguments/return types, `var` variables, and type parameter substitution.
For these cases, flow state is necessary to compute the declared nullability.
For example:

```C#
string? s1 = string.Empty;
var s2 = s1;
s2 = null; // This will warn, because the type of s2 is string, not string?
```

The declared annotation of `s1` is `Annotated`, even though it is being initialized with a non-null value.
However, because of the flow state of `s1` is `NotNull`, we will infer the declared annotation of `s2` to be `NotAnnotated` as well.

The declared nullable annotation can be retrieved from the declaration symbol (e.g. `IFieldSymbol`, `IMethodSymbol`, etc.).
The nullable annotation of symbols at a location can be retrieved through the `GetSymbolInfo` API: if the expression successfully resolved to a single symbol, the `Symbol` property will have annotations calculated.
`CandidateSymbols` will not have calculated nullable annotations, as we do not run nullable analysis on code that has failed overload resolution.

### Flow State

When the nullable feature is enabled, the compiler will track the flow state of expressions throughout a method, regardless of what the variable was declared as.
This flow state will always be represented by the `NullableFlowState` enum.
`SemanticModel` can be used to request flow state information for individual expressions or sub-expressions through the `GetTypeInfo` APIs.
The `TypeInfo` struct will be augmented with `Nullability` and `ConvertedNullability` information, similar to how it currently has `Type` and `ConvertedType`.

### `GetSemanticModel(bool skipNullabilityInformation)`

This API proposal changes the way `SemanticModel` calls behave by default.
Currently, if semantic information is requested for a `SyntaxNode`, we make an effort to bind as little as possible to provide semantic information about that statement, usually a single statement.
However, if that semantic information will include `Nullability`, we must bind all statements that come before that statement and run nullability analysis up to and including that statement.
We believe that this change will be ok for most scenarios, as things such as colorization and analyzers are already running in the background near-constantly, binding pretty much everything on screen and in the file anyway.
There are a few specific scenarios that will be perf-sensitive, and an overload for `GetSemanticModel()` that skips nullability information will be provided for these.
It will share an initial-binding `BoundNode` cache with the standard semantic model, so it will get the benefits if something else with the same semantic model requests information, as it would have today.

## New APIs

We add the new APIs for retreiving and interacting with nullability:

```C#
public enum NullableAnnotation : byte
{
    None,
    NotAnnotated,
    Annotated
}

public enum NullableFlowState : byte
{
    NotApplicable,
    NotNull,
    MaybeNull
}

public readonly struct NullabilityInfo
{
    public NullableAnnotation Annotation { get; }
    public NullableFlowState FlowState { get; }
}

#region Declared Nullability

// These can potentially be influenced by calculated flow state, but do not change after initial declaration

public interface IDiscardSymbol : ISymbol
{
    NullableAnnotation NullableAnnotation { get; }
}

public interface IEventSymbol : ISymbol
{
    NullableAnnotation NullableAnnotation { get; }
}

public interface IFieldSymbol : ISymbol
{
    NullableAnnotation NullableAnnotation { get; }
}

public interface ILocalSymbol : ISymbol
{
    NullableAnnotation NullableAnnotation { get; }
}

public interface IMethodSymbol : ISymbol
{
    NullableAnnotation ReturnNullableAnnotation { get; }
    ImmutableArray<NullableAnnotation> TypeArgumentsNullableAnnotations { get; }
    NullableAnnotation ReceiverNullableAnnotation { get; }
}

public interface IParameterSymbol : ISymbol
{
    NullableAnnotation NullableAnnotation { get; }
}

public interface IArrayTypeSymbol : ITypeSymbol
{
    NullableAnnotation ElementNullableAnnotation { get; }
}

public interface INamedTypeSymbol : ITypeSymbol
{
    ImmutableArray<NullableAnnotation> TypeArgumentsNullableAnnotations { get; }
}

public interface ITypeParameterSymbol : ITypeSymbol
{
    NullableAnnotation ReferenceTypeConstraintNullableAnntotation { get; }
    ImmutableArray<NullableAnnotation> ConstraintsNullableAnnotations { get; }
}

#endregion

#region Flow State Nullability

public struct TypeInfo
{
    public NullabilityInfo Nullability { get; }
    public NullabilityInfo ConvertedNullability { get; }
}

#endregion

#region Compilation

public class Compilation
{
    // This API will only be added if we determine perf-sensitive scenarios will need it
    // after dogfooding the feature
    public SemanticModel GetSemanticModel(bool skipNullabilityInformation);
}

#endregion

#region Printing

public interface ITypeSymbol
{
    string ToDisplayString(NullableFlowState nullability, SymbolDisplayFormat format = null);
    ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState nullability, SymbolDisplayFormat format = null);
    string ToMinimalDisplayString(SemanticModel semanticModel, int position, NullableFlowState nullability, SymbolDisplayFormat format = null);
    ImmutableArray<SymbolDisplayPart> ToMinimalDisplayString(SemanticModel semanticModel, int position, NullableFlowState nullability, SymbolDisplayFormat format = null);
}

/*
 * There will be more API changes, such as on SyntaxFactory and SyntaxGenerator. This section will be updated later when we have designs for this ready.
 */

#endregion
```

## Considered Alternatives

### `Nullability` on `ITypeSymbol`

The basic principle of this alternative proposal is to add `Nullability Nullability { get; }` to `ITypeSymbol`, and remove the rest of the proposed nullability retrieval APIs in the main section.
This has a few benefits:

1. Consumers of `ITypeSymbol`s won't have to keep track of where the type symbol came from to retrieve nullability information, they just have to ask the type.
2. Printing APIs and code generation APIs do not need to have new overloads introduced to allow communication of top-level nullability information.
3. Consumers of the new API don't have to separately worry about nullability when comparing types, as `string`, `string~`, and `string?` are truly different types.

However, this approach has a few major issues that ultimately break our ability to do this design.

1. This approach will violate principle #1 of our goals for this API, breaking existing analyzers and refactorings extensively.
A common strategy for these APIs is to call `Compilation.GetTypeByMetadataName`, or one of the similar overloads, and then look for fields, locals, properties, or other similar symbols of the requested type.
Up to this point, our guidance has been that, for non-generic types, it is perfectly fine to just compare these symbols via `==` if you're looking for exact matches, and that you only need to resort to `OriginalDefinition` for types with generics.
This will no longer be possible if we were to add `Nullability` to `ITypeSymbol`.
2. The default `Nullability` of symbols is another pain point.
When a class is declared, it doesn't have a nullability.
It's just an `INamedTypeSymbol`.
It may have constraints that have nullabilities, but the top-level nullability is not a question that makes any sense.
However, in order to not break analyzers on nullable-unaware code, we will have to make the default nullability `Disabled`, as old C# code will have this as the nullability when `GetTypeInfo` is called on an expression.
This would further exacerbate issue #1.
3. This causes massive explosion in type heirarchies.
If a user got `string?` and called `GetDeclaredMembers()`, the parent of each of these members must point back to `string?`, and not `string` or `string~`.

For these reasons, we have decided not take this approach.

### `GetSingleStatementSemanticModel`

Instead of an overload on `Compilation` to get a `SemanticModel` without nullability info calculated, we had a `GetSingleStatementSemanticModel()` method on `SemanticModel`, that would get a `SemanticModel` that shared the `BoundNode` cache with the existing `SemanticModel` and did not calculate nullability information.
This was rejected for being too complicated a solution, and for not being very future-proof if we were to add more analyses that users would want to opt out of.

### `GetNullabilityAwareSemanticModel`

This alternative proposal flipped the `GetSingleStatementSemanticModel` proposal on its head: instead of `SemanticModel` calculating nullability by default, it would only calculate when requested.
We decided against this because we believe that, except for specific cases where perf is an issue, the savings gained from not calculating nullability would be moot as IDE features or analyzers will have already requested nullability anyway, and the time savings would be unused.
Therefore, we are not taking this approach.

### `GetNullabilityInfo`

An early alternative proposal had a parallel API to `GetTypeInfo()`, `GetNullabilityInfo()`, that would return a structure similar to `TypeInfo` that contained nullability information.
We decided that this would provide no benefits: because of nested nullability, we would still need to run nullability analysis on calls to `GetTypeInfo`, and it would needlessly separate information that users will want to get access to in tandem.
Additionally, we could not come up with a good set of APIs for `GetDeclaredSymbol` and `GetSymbolInfo` to go along with that API.

### Lazily calculated `Nullability` properties

This alternative proposal added similar `Nullability` properties to the interfaces as the main proposal, but did not calculate them ahead of time.
Rather, they would be calculated when requested by the user.
The big problem with this is that suddenly symbols, `TypeInfo`, and `SymbolInfo` must suddenly start carrying around the context in which they were called to be able to lazily calculate these properties, which is a major breaking change.

### `ITypeReference`

We briefly wandered down a thought experiment where we considered adding an `ITypeReference` API, which would contain an `ITypeSymbol` and a `Nullability`, plus potentially things like ref, custom modifiers, and other side car information we have today.
This would be an *extremely* breaking change, requiring massive changes across the API surface area.
It would also require revisiting an explicit design choice in the initial design of Roslyn.

### A single `Nullability` enum for both lvalues and rvalues

This proposal looked very similar to the current proposal, except there was only one public enum, `Nullability`, with possible values of `NotApplicable`, `NotComputed`, `Unknown`, `MaybeNull`, and `NotNull`.
After some work towards implementation, we felt that separating rvalue state and lvalue annotation would lead to an ultimately more useful API that was more maintainable and testable.
