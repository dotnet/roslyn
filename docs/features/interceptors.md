# Interceptors

## Summary
[summary]: #summary

*Interceptors* are an experimental compiler feature.

An *interceptor* is a method which can declaratively substitute a call to an *interceptable* method with a call to itself at compile time. This substitution occurs by having the interceptor declare the source locations of the calls that it intercepts. This provides a limited facility to change the semantics of existing code by adding new code to a compilation (e.g. in a source generator).

```cs
using System;
using System.Runtime.CompilerServices;

var c = new C();
c.InterceptableMethod(1); // (L1,C1): prints "interceptor 1"
c.InterceptableMethod(1); // (L2,C2): prints "other interceptor 1"
c.InterceptableMethod(1); // prints "interceptable 1"

class C
{
    [Interceptable]
    public void InterceptableMethod(int param)
    {
        Console.WriteLine($"interceptable {param}");
    }
}

// generated code
static class D
{
    [InterceptsLocation("Program.cs", line: /*L1*/, character: /*C1*/)] // refers to the call at (L1, C1)
    public static void InterceptorMethod(this C c, int param)
    {
        Console.WriteLine($"interceptor {param}");
    }

    [InterceptsLocation("Program.cs", line: /*L2*/, character: /*C2*/)] // refers to the call at (L2, C2)
    public static void OtherInterceptorMethod(this C c, int param)
    {
        Console.WriteLine($"other interceptor {param}");
    }
}
```

## Detailed design
[design]: #detailed-design

### InterceptableAttribute

A method must indicate that its calls can be *intercepted* by including `[Interceptable]` on its declaration.

If a call is intercepted to a method which lacks this attribute, a warning is reported, and interception still occurs. This may be changed to an error in the future.

```cs
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InterceptableAttribute : Attribute { }
}
```

### InterceptsLocationAttribute

A method indicates that it is an *interceptor* by adding one or more `[InterceptsLocation]` attributes. These attributes refer to the source locations of the calls it intercepts.

```cs
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class InterceptsLocationAttribute(string filePath, int line, int character) : Attribute
    {
    }
}
```

PROTOTYPE(ic): open question in https://github.com/dotnet/roslyn/pull/67432#discussion_r1147822738

Are the `[InterceptsLocation]` attributes dropped when emitting the methods to metadata?

No. We could consider:

* having the compiler drop the attributes automatically (treating them as pseudo-custom attributes, perhaps)
* making the attribute declaration in the framework "conditional" on some debug symbol which is not usually provided.
* deliberately keeping them there. I can't think of any good reason to do this.

Perhaps the first option would be best.

#### File paths

File paths used in `[InterceptsLocation]` must exactly match the paths on the syntax trees they refer to by ordinal comparison. `SyntaxTree.FilePath` has already applied `/pathmap` substitution, so the paths used in the attribute will be less environment-specific in many projects.

The compiler does not map `#line` directives when determining if an `[InterceptsLocation]` attribute intercepts a particular call in syntax.

#### Position

Line and column numbers in `[InterceptsLocation]` are 1-indexed to match existing places where source locations are displayed to the user. For example, in `Diagnostic.ToString`.

The location of the call is the location of the simple name syntax which denotes the interceptable method. For example, in `app.MapGet(...)`, the name syntax for `MapGet` would be considered the location of the call. For a static method call like `System.Console.WriteLine(...)`, the name syntax for `WriteLine` is the location of the call. If we allow intercepting calls to property accessors in the future (e.g `obj.Property`), we would also be able to use the name syntax in this way.

#### Attribute creation

The goal of the above decisions is to make it so that when source generators are filling in `[InterceptsLocation(...)]`, they simply need to read `nameSyntax.SyntaxTree.FilePath` and `nameSyntax.GetLineSpan().Span.Start` for the exact file path and position information they need to use.

We should provide samples of recommended coding patterns for generator authors to show correct usage of these, including the "translation" from 0-indexed to 1-indexed positions.

### Non-invocation method usages

Conversion to delegate type, address-of, etc. usages of methods cannot be intercepted.

Interception can only occur for calls to ordinary member methods--not constructors, delegates, properties, local functions, etc.

### Arity

Interceptors cannot have type parameters or be declared in generic types at any level of nesting.

### Signature matching

When a call is intercepted, the interceptor and interceptable methods must meet the signature matching requirements detailed below:
- When an interceptable instance method is compared to a classic extension method, we use the extension method in reduced form for comparison. The extension method parameter with the `this` modifier is compared to the instance method `this` parameter.
- The returns and parameters, including the `this` parameter, must have the same ref kinds and types, except that reference types with oblivious nullability can match either annotated or unannotated reference types.
- Method names and parameter names are not required to match.
- Parameter default values are not required to match. When intercepting, default values on the interceptor method are ignored.
- `scoped` modifiers and `[UnscopedRefAttribute]` must be equivalent.

Arity does not need to match between intercepted and interceptor methods. In other words, it is permitted to intercept a generic method with a non-generic interceptor.

### Conflicting interceptors

If more than one interceptor refers to the same location, it is a compile-time error.

If an `[InterceptsLocation]` attribute is found in the compilation which does not refer to the location of an explicit method call, it is a compile-time error.

### Interceptor accessibility

An interceptor must be accessible at the location where interception is occurring. PROTOTYPE(ic): This enforcement is not yet implemented.

We imagine it will be common to want to discourage explicit use of interceptor methods. For this use case, should consider adjusting behavior of `[EditorBrowsable]` to work in the same compilation, and encouraging generator authors to use it to prevent interceptors from appearing in lookup, etc.

PROTOTYPE(ic): Generators often want to put things not intended to be user-visible in file-local types. This reduces the need to defend against name conflicts with other types in the user's project. A file-local type is not present in lookup outside of the file it is declared in. But, the type is *accessible* from the runtime point of view presently. Should we permit interceptors declared in file types to refer to other files?

### Editor experience

Interceptors are treated like a post-compilation step in this design. Diagnostics are given for misuse of interceptors, but some diagnostics are only given in the command-line build and not in the IDE. There is limited traceability in the editor for which calls in a compilation are actually being intercepted. If this feature is brought forward past the experimental stage, this limitation will need to be re-examined.

### User opt-in

PROTOTYPE(ic): design an opt-in step which meets the needs of the generators consuming this, and permits usage of the feature in the key NativeAOT scenarios we are targeting.

Because this is a generator-oriented feature, we'd like to have an opt-in step during the *experimental* phase which permits generators to opt-in without the end user needing to take additional steps.

Question: can a generator opt-in to a preview feature on behalf of the user? If not, is it fine to ask the user to take an additional step here?

There's a concern on the ASP.NET side that if the user needs to opt-in to something which is "preview/experimental" then they will need to use their non-interceptors codegen strategy *as well as* the interceptors one for NativeAOT, depending on whether the user did the gesture to enable the feature.

### Implementation strategy

During the binding phase, `InterceptsLocationAttribute` usages are decoded and the related data for each usage are collected in a `ConcurrentSet` on the compilation:
- intercepted file-path and location
- attribute location
- attributed method symbol
PROTOTYPE(ic): the exact collection used to collect the attribute usages, and the exact way it is used, are not finalized. The main concern is to ensure we can scale to large numbers of interceptors without issue, and that we can report diagnostics for duplicate interception of the same location in a deterministic way.

At this time, diagnostics are reported for the following conditions:
- problems specific to the attributed interceptor method itself, for example, that it is not an ordinary method.
- syntactic problems specific to the referenced location, for example, that it does not refer to an applicable simple name as defined in [Position](#position) subsection.

During the lowering phase, when a given `BoundCall` is lowered:
- we check if its syntax contains an applicable simple name
- if so, we lookup whether it is being intercepted, based on data about `InterceptsLocationAttribute` collected during the binding phase.
- if it is being intercepted, we perform an additional step after lowering of the receiver and arguments is completed:
  - substitute the interceptable method with the interceptor method on the `BoundCall`.
  - if the interceptor is a classic extension method, and the interceptable method is an instance method, we adjust the `BoundCall` to use the receiver as the first argument of the call, "pushing" the other arguments forward, similar to the way it would have bound if the original call were to an extension method in reduced form.

At this time, diagnostics are reported for the following conditions:
- incompatibility between the interceptor and interceptable methods, for example, in their signatures.
- *duplicate* `[InterceptsLocation]`, that is, multiple interceptors which intercept the same call. PROTOTYPE(ic): not yet implemented.
