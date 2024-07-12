# Interceptors

## Summary
[summary]: #summary

*Interceptors* are an experimental compiler feature planned to ship in .NET 8 (with support for C# only). The feature may be subject to breaking changes or removal in a future release.

An *interceptor* is a method which can declaratively substitute a call to an *interceptable* method with a call to itself at compile time. This substitution occurs by having the interceptor declare the source locations of the calls that it intercepts. This provides a limited facility to change the semantics of existing code by adding new code to a compilation (e.g. in a source generator).

```cs
using System;
using System.Runtime.CompilerServices;

var c = new C();
c.InterceptableMethod(1); // L1: prints "interceptor 1"
c.InterceptableMethod(1); // L2: prints "other interceptor 1"
c.InterceptableMethod(2); // L3: prints "other interceptor 2"
c.InterceptableMethod(1); // prints "interceptable 1"

class C
{
    public void InterceptableMethod(int param)
    {
        Console.WriteLine($"interceptable {param}");
    }
}

// generated code
static class D
{
    [InterceptsLocation(version: 1, data: "...(refers to the call at L1)")]
    public static void InterceptorMethod(this C c, int param)
    {
        Console.WriteLine($"interceptor {param}");
    }

    [InterceptsLocation(version: 1, data: "...(refers to the call at L2)")]
    [InterceptsLocation(version: 1, data: "...(refers to the call at L3)")]
    public static void OtherInterceptorMethod(this C c, int param)
    {
        Console.WriteLine($"other interceptor {param}");
    }
}
```

## Detailed design
[design]: #detailed-design

### InterceptsLocationAttribute

A method indicates that it is an *interceptor* by adding one or more `[InterceptsLocation]` attributes. These attributes refer to the source locations of the calls it intercepts.

```cs
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class InterceptsLocationAttribute(int version, string data) : Attribute
    {
    }
}
```

Any "ordinary method" (i.e. with `MethodKind.Ordinary`) can have its calls intercepted.

In addition to "ordinary" forms `M()` and `receiver.M()`, a call within a conditional access, e.g. of the form `receiver?.M()` can be intercepted. A call whose receiver is a pointer member access, e.g. of the form `ptr->M()`, can also be intercepted.

`[InterceptsLocation]` attributes included in source are emitted to the resulting assembly, just like other custom attributes.

File-local declarations of this type (`file class InterceptsLocationAttribute`) are valid and usages are recognized by the compiler when they are within the same file and compilation. A generator which needs to declare this attribute should use a file-local declaration to ensure it doesn't conflict with other generators that need to do the same thing.

In prior experimental releases of the feature, a well-known constructor signature `InterceptsLocation(string path, int line, int column)]` was also supported. Support for this constructor will be **dropped** prior to stable release of the feature.

#### Location encoding

The arguments to `[InterceptsLocation]` are:
1. a version number. The compiler may introduce new encodings for the location in the future, with corresponding new version numbers.
2. an opaque data string. This is not intended to be human-readable.

The "version 1" data encoding is a base64-encoded string consisting of the following data:
- 16 byte xxHash128 content checksum of the file containing the intercepted call.
- int32 in little-endian format for the position (i.e. `SyntaxNode.Position`) of the call in syntax.
- utf-8 string data containing a display file name, used for error reporting.

#### Position

The location of the call is the location of the simple name syntax which denotes the interceptable method. For example, in `app.MapGet(...)`, the name syntax for `MapGet` would be considered the location of the call. For a static method call like `System.Console.WriteLine(...)`, the name syntax for `WriteLine` is the location of the call. If we allow intercepting calls to property accessors in the future (e.g `obj.Property`), we would also be able to use the name syntax in this way.

#### Attribute creation

Roslyn provides an API `GetInterceptableLocation(this SemanticModel, InvocationExpressionSyntax, CancellationToken)` for inserting `[InterceptsLocation]` into generated source code. We recommend that source generators depend on this API in order to intercept calls. See https://github.com/dotnet/roslyn/issues/72133 for further details.

### Non-invocation method usages

Conversion to delegate type, address-of, etc. usages of methods cannot be intercepted.

Interception can only occur for calls to ordinary member methods--not constructors, delegates, properties, local functions, operators, etc. Support for more member kinds may be added in the future.

### Arity

Interceptors cannot be declared in generic types at any level of nesting.

Interceptors must either be non-generic, or have arity equal to the sum of the arity of the original method's arity and containing type arities. For example:

```cs
Grandparent<int>.Parent<bool>.Original<string>(1, false, "a"); // L1

class Grandparent<T1>
{
    class Parent<T2>
    {
        public static void Original<T3>(T1 t1, T2 t2, T3 t3) { }
    }
}

class Interceptors
{
    [InterceptsLocation(1, "..(refers to call at L1)")]
    public static void Interceptor<T1, T2, T3>(T1 t1, T2 t2, T3 t3) { }
}
```

When an interceptor is generic, the type arguments from the original containing types and method are passed as type arguments to the interceptor, from outermost to innermost. In the above scenario, the interceptor receives `<int, bool, string>` as type arguments. If the interceptor type parameters have constraints which are violated by these type arguments, a compile-time error occurs.

This substitution allows interceptors to use type parameters which aren't in scope at its declaration site.

```cs
using System.Runtime.CompilerServices;

class C
{
    public static void InterceptableMethod<T1>(T1 t) => throw null!;
}

static class Program
{
    public static void M<T2>(T2 t)
    {
        C.InterceptableMethod(t); // L1
    }
}

static class D
{
    [InterceptsLocation(1, "..(refers to call at L1)")]
    public static void Interceptor1<T2>(T2 t) => throw null!;
}
```

### Signature matching

When a call is intercepted, the interceptor and interceptable methods must meet the signature matching requirements detailed below:
- When an interceptable instance method is compared to a static interceptor method (including a classic extension method), we use the method as if it is an extension in reduced form for comparison. The first parameter of the static method is compared to the instance method `this` parameter.
    - The implementation currently requires the interceptor to be an extension method for this comparison to work. We plan on addressing this before releasing .NET 8.
- The returns and parameters of the respective methods, including the `this` parameter, must have the same ref kinds and types.
- The `this` parameter of the respective methods must have the same ref kinds and types, except that when a `readonly` struct instance method is intercepted with a static method, it is permitted for the interceptor `this` parameter to be either `in` or `ref readonly`.
- A warning is reported instead of an error if a type difference is found where the types are not distinct to the runtime. For example, `object` and `dynamic`.
- No warning or error is reported for a *safe* nullability difference, such as when the interceptable method accepts a `string` parameter, and the interceptor accepts a `string?` parameter.
- Method names and parameter names are not required to match.
- Parameter default values are not required to match. When intercepting, default values on the interceptor method are ignored.
- `params` modifiers are not required to match.
- `scoped` modifiers and `[UnscopedRef]` must be equivalent.
- In general, attributes which normally affect the behavior of the call site, such as `[CallerLineNumber]` are ignored on the interceptor of an intercepted call.
  - The only exception to this is when the attribute affects "capabilities" of the method in a way that affects safety, such as with `[UnscopedRef]`. Such attributes are required to match across interceptable and interceptor methods.

Arity does not need to match between intercepted and interceptor methods. In other words, it is permitted to intercept a generic method with a non-generic interceptor.

### Conflicting interceptors

If more than one interceptor refers to the same location, it is a compile-time error.

If an `[InterceptsLocation]` attribute is found in the compilation which does not refer to the location of an explicit method call, it is a compile-time error.

### Interceptor accessibility

An interceptor must be accessible at the location where interception is occurring.

An interceptor contained in a file-local type is permitted to intercept a call in another file, even though the interceptor is not normally *visible* at the call site.

This allows generator authors to avoid *polluting lookup* with interceptors, helps avoid name conflicts, and prevents use of interceptors in *unintended positions* from the interceptor author's point-of-view.

We may also want to consider adjusting behavior of `[EditorBrowsable]` to work in the same compilation.

### Struct receiver capture

An interceptor whose `this` parameter takes a struct by-reference can generally be used to intercept a struct instance method call, assuming the methods are compatible per [Signature matching](#signature-matching). This includes a specific situation where the interceptor wouldn't be directly usable as an extension method, due to the receiver being an rvalue. See also [12.8.9.3 Extension method invocations
](https://github.com/dotnet/csharpstandard/blob/standard-v7/standard/expressions.md#12893-extension-method-invocations) in the standard.


```cs
using System.Runtime.CompilerServices;

struct S
{
    public void Original() { }
}

static class Program
{
    public static void Interceptor()
    {
        new S().Original(); // L1: interception is valid, no errors.
        new S().Interceptor(); // error CS1510: A ref or out value must be an assignable variable
    }
}

static class D
{
    [InterceptsLocation(1, "..(refers to call at L1)")]
    public static void Interceptor(this ref S s)
}
```

The goal of this is to avoid a "decoder ring" experience where an interceptor author needs to copy exactly the same ref kind sometimes and use a different ref kind other times, depending on the call arguments. We generally want that if your interceptor is accessible at the call site and uses all the same parameter ref kinds and types that you will be allowed to intercept.

### Editor experience

Interceptors are treated like a post-compilation step in this design. Diagnostics are given for misuse of interceptors, but some diagnostics are only given in the command-line build and not in the IDE. There is limited traceability in the editor for which calls in a compilation are actually being intercepted. If this feature is brought forward past the experimental stage, this limitation will need to be re-examined.

There is an experimental public API `GetInterceptorMethod(this SemanticModel, InvocationExpressionSyntax, CancellationToken)` which enables analyzers to determine if a call is being intercepted, and if so, which method is intercepting the call. See https://github.com/dotnet/roslyn/issues/72093 for further details.

### User opt-in

To use interceptors, the user project must specify the property `<InterceptorsPreviewNamespaces>`. This is a list of namespaces which are allowed to contain interceptors.
```xml
<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);Microsoft.AspNetCore.Http.Generated;MyLibrary.Generated</InterceptorsPreviewNamespaces>
```

It's expected that each entry in the `InterceptorsPreviewNamespaces` list roughly corresponds to one source generator. Well-behaved components are expected to not insert interceptors into namespaces they do not own.

### Implementation strategy

During the binding phase, `InterceptsLocationAttribute` usages are decoded and the related data for each usage are collected in a `ConcurrentSet` on the compilation:
- intercepted file-path and location
- attribute location
- attributed method symbol

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
- *duplicate* `[InterceptsLocation]`, that is, multiple interceptors which intercept the same call.
- interceptor is not accessible at the call site.
