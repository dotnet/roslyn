# Interceptors

## Summary
[summary]: #summary

*Interceptors* are an experimental compiler feature planned to ship in .NET 8. The feature may be subject to breaking changes or removal in a future release.

An *interceptor* is a method which can declaratively substitute a call to an *interceptable* method with a call to itself at compile time. This substitution occurs by having the interceptor declare the source locations of the calls that it intercepts. This provides a limited facility to change the semantics of existing code by adding new code to a compilation (e.g. in a source generator).

```cs
using System;
using System.Runtime.CompilerServices;

var c = new C();
c.InterceptableMethod(1); // (L1,C1): prints "interceptor 1"
c.InterceptableMethod(1); // (L2,C2): prints "other interceptor 1"
c.InterceptableMethod(2); // (L3,C3): prints "other interceptor 2"
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
    [InterceptsLocation("Program.cs", line: /*L3*/, character: /*C3*/)] // refers to the call at (L3, C3)
    public static void OtherInterceptorMethod(this C c, int param)
    {
        Console.WriteLine($"other interceptor {param}");
    }
}
```

## Detailed design
[design]: #detailed-design

### InterceptableAttribute

A method can indicate that its calls can be *intercepted* by including `[Interceptable]` on its declaration.

PROTOTYPE(ic): For now, if a call is intercepted to a method which lacks this attribute, a warning is reported, and interception still occurs. This may be changed to an error in the future.

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

`[InterceptsLocation]` attributes included in source are emitted to the resulting assembly, just like other custom attributes.

PROTOTYPE(ic): We may want to recognize `file class InterceptsLocationAttribute` as a valid declaration of the attribute, to allow generators to bring the attribute in without conflicting with other generators which may also be bringing the attribute in. See open question in [User opt-in](#user-opt-in).

#### File paths

File paths used in `[InterceptsLocation]` must exactly match the paths on the syntax trees they refer to by ordinal comparison. `SyntaxTree.FilePath` has already applied `/pathmap` substitution, so the paths used in the attribute will be less environment-specific in many projects.

The compiler does not map `#line` directives when determining if an `[InterceptsLocation]` attribute intercepts a particular call in syntax.

PROTOTYPE(ic): editorconfig support matches paths in cross-platform fashion (e.g. normalizing slashes). We should revisit how that works and consider if the same matching strategy should be used instead of ordinal comparison.

#### Position

Line and column numbers in `[InterceptsLocation]` are 1-indexed to match existing places where source locations are displayed to the user. For example, in `Diagnostic.ToString`.

The location of the call is the location of the simple name syntax which denotes the interceptable method. For example, in `app.MapGet(...)`, the name syntax for `MapGet` would be considered the location of the call. For a static method call like `System.Console.WriteLine(...)`, the name syntax for `WriteLine` is the location of the call. If we allow intercepting calls to property accessors in the future (e.g `obj.Property`), we would also be able to use the name syntax in this way.

#### Attribute creation

The goal of the above decisions is to make it so that when source generators are filling in `[InterceptsLocation(...)]`, they simply need to read `nameSyntax.SyntaxTree.FilePath` and `nameSyntax.GetLineSpan().Span.Start` for the exact file path and position information they need to use.

We should provide samples of recommended coding patterns for generator authors to show correct usage of these, including the "translation" from 0-indexed to 1-indexed positions.

### Non-invocation method usages

Conversion to delegate type, address-of, etc. usages of methods cannot be intercepted.

Interception can only occur for calls to ordinary member methods--not constructors, delegates, properties, local functions, operators, etc. Support for more member kinds may be added in the future.

### Arity

Interceptors cannot have type parameters or be declared in generic types at any level of nesting.

### Signature matching

PROTOTYPE(ic): It is suggested to permit nullability differences and other comparable differences. Perhaps we can revisit the matching requirements of "partial methods" and imitate them here.

When a call is intercepted, the interceptor and interceptable methods must meet the signature matching requirements detailed below:
- When an interceptable instance method is compared to a classic extension method, we use the extension method in reduced form for comparison. The extension method parameter with the `this` modifier is compared to the instance method `this` parameter.
- The returns and parameters, including the `this` parameter, must have the same ref kinds and types, except that reference types with oblivious nullability can match either annotated or unannotated reference types.
- Method names and parameter names are not required to match.
- Parameter default values are not required to match. When intercepting, default values on the interceptor method are ignored.
- `params` modifiers are not required to match.
- `scoped` modifiers and `[UnscopedRef]` must be equivalent.
- In general, attributes which normally affect the behavior of the call site, such as `[CallerLineNumber]` are ignored on the interceptor of an intercepted call.
  - The only exception to this is when the attribute affects "capabilities" of the method in a way that affects safety, such as with `[UnscopedRef]`. In this case, attributes are required to match across interceptable and interceptor methods.

Arity does not need to match between intercepted and interceptor methods. In other words, it is permitted to intercept a generic method with a non-generic interceptor.

### Conflicting interceptors

If more than one interceptor refers to the same location, it is a compile-time error.

If an `[InterceptsLocation]` attribute is found in the compilation which does not refer to the location of an explicit method call, it is a compile-time error.

### Interceptor accessibility

An interceptor must be accessible at the location where interception is occurring. PROTOTYPE(ic): This enforcement is not yet implemented.

An interceptor contained in a file-local type is permitted to intercept a call in another file, even though the interceptor is not normally *visible* at the call site.

This allows generator authors to avoid *polluting lookup* with interceptors, helps avoid name conflicts, and prevents use of interceptors in *unintended positions* from the interceptor author's point-of-view.

We may also want to consider adjusting behavior of `[EditorBrowsable]` to work in the same compilation.

### Editor experience

Interceptors are treated like a post-compilation step in this design. Diagnostics are given for misuse of interceptors, but some diagnostics are only given in the command-line build and not in the IDE. There is limited traceability in the editor for which calls in a compilation are actually being intercepted. If this feature is brought forward past the experimental stage, this limitation will need to be re-examined.

### User opt-in

Although interceptors are an experimental feature, there will be no explicit opt-in step needed to use them. We won't publicize the feature (e.g. in blog posts) as something generator authors should onboard to in .NET 8.

PROTOTYPE(ic): The BCL might not ship the attributes required by this feature, instead requiring them to be declared in some library brought in by a package reference, or in the user's project. But we haven't confirmed this.

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
