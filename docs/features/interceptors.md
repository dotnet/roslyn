# Interceptors

## Summary
[summary]: #summary

*Interceptors* are an experimental compiler feature. An *interceptor* is a method which can declaratively substitute a call to itself instead of a call to an *interceptable* method at compile time. This substitution occurs by having the interceptor declare the source locations of the calls that it intercepts. This provides a limited facility to change the semantics of existing code by adding new code to a compilation (e.g. in a source generator).

```cs
using System;
using System.Runtime.CompilerServices;

var c = new C();
c.InterceptableMethod(1); // (L1,C1): prints `interceptor 1`
c.InterceptableMethod(1); // (L2,C2): prints `other interceptor 1`
c.InterceptableMethod(1); // prints `interceptable 1`

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

If a call is intercepted to a method which lacks this attribute, a warning is reported. This may be changed to an error in the future.

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

#### File paths

File paths used in `[InterceptsLocation]` must exactly match the paths on the syntax trees they refer to by ordinal comparison. `SyntaxTree.FilePath` has already applied `/pathmap` substitution, so the paths used in the attribute will be less environment-specific in many projects.

The compiler does not map `#line` directives when determining if an `[InterceptsLocation]` attribute intercepts a particular call in syntax.

#### Position
The implementation currently uses 0-indexed line and character numbers. However, we may want to change that before shipping it as an experimental feature to be 1-indexed, to match existing places where these values are displayed to the user (e.g. `Diagnostic.ToString`).

The location of the call is the location of the name syntax which denotes the interceptable method. For example, in `app.MapGet(...)`, the name syntax for `MapGet` would be considered the location of the call. If we allow intercepting calls to property accessors in the future (e.g `obj.Property`), we would also be able to use the name syntax in this way.

#### Attribute creation

The goal of the above decisions is to make it so that when source generators are filling in `[InterceptsLocation(...)]`, they simply need to read `nameSyntax.SyntaxTree.FilePath` and `nameSyntax.GetLineSpan().Span.Start` for the exact file path and position information they need to use.

We should provide samples of recommended coding patterns for generator authors, and perhaps provide public helper methods, which will make it relatively easy for them to do the right thing. For example, we could provide a helper on InvocationExpressionSyntax which returns the values that need to be put in `[InterceptsLocation]`.

### Non-invocation method usages

Conversion to delegate type, address-of, etc. usages of methods cannot be intercepted.

Interception can only occur for calls to ordinary member methods--not constructors, delegates, properties, local functions, etc.

### Arity

Interceptors cannot have type parameters or be declared in generic types at any level of nesting.

### Signature matching

The return and parameter types of the interceptable and interceptor methods must match exactly, except that:
- when an interceptable instance method is compared to a classic extension method, we use the extension method in reduced form for comparison, and
- reference types with oblivious nullability can match either annotated or unannotated reference types.

Arity does not need to match between intercepted and interceptor methods. In other words, it is permitted to intercept a generic method with a non-generic interceptor.

### Conflicting interceptors

If more than one interceptor refers to the same location, it is a compile-time error.

If an `[InterceptsLocation]` attribute is found in the compilation which does not refer to the location of an interceptable method call, it is a compile-time error.

### Interceptor accessibility

An interceptor can intercept a call at a given location even if the interceptor would not ordinarily be accessible at that location.

### Editor experience

Interceptors are treated like a post-compilation step in this design. Diagnostics are given for misuse of interceptors, but some diagnostics are only given in the command-line build and not in the IDE. There is limited traceability in the editor for which calls in a compilation are actually being intercepted. If this feature is brought forward past the experimental stage, this limitation will need to be re-examined.
