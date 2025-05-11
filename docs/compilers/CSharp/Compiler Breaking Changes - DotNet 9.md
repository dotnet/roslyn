# Breaking changes in Roslyn after .NET 8.0.100 through .NET 9.0.100

This document lists known breaking changes in Roslyn after .NET 8 general release (.NET SDK version 8.0.100) through .NET 9 general release (.NET SDK version 9.0.100).

## InlineArray attribute on a record struct type is no longer allowed.

***Introduced in Visual Studio 2022 version 17.11***

```cs
[System.Runtime.CompilerServices.InlineArray(10)] // error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
record struct Buffer1()
{
    private int _element0;
}

[System.Runtime.CompilerServices.InlineArray(10)] // error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
record struct Buffer2(int p1)
{
}
```


## Iterators introduce safe context in C# 13 and newer

***Introduced in Visual Studio 2022 version 17.11***

Although the language spec states that iterators introduce a safe context, Roslyn does not implement that in C# 12 and lower.
This will change in C# 13 as part of [a feature which allows unsafe code in iterators](https://github.com/dotnet/roslyn/issues/72662).
The change does not break normal scenarios as it was disallowed to use unsafe constructs directly in iterators anyway.
However, it can break scenarios where an unsafe context was previously inherited into nested local functions, for example:

```cs
unsafe class C // unsafe context
{
    System.Collections.Generic.IEnumerable<int> M() // an iterator
    {
        yield return 1;
        local();
        void local()
        {
            int* p = null; // allowed in C# 12; error in C# 13
        }
    }
}
```

You can work around the break simply by adding the `unsafe` modifier to the local function.

## Collection expression breaking changes with overload resolution in C# 13 and newer

***Introduced in Visual Studio 2022 Version 17.12 and newer when using C# 13+***

There are a few changes in collection expression binding in C# 13. Most of these are turning ambiguities into successful compilations,
but a couple are breaking changes that either result in a new compilation error, or are a behavior breaking change. They are detailed
below.

### Empty collection expressions no longer use whether an API is a span to tiebreak on overloads

When an empty collection expression is provided to an overloaded method, and there isn't a clear element type, we no longer use whether
an API takes a `ReadOnlySpan<T>` or a `Span<T>` to decide whether to prefer that API. For example:

```cs
class C
{
    static void M(ReadOnlySpan<int> ros) {}
    static void M(Span<object> s) {}

    static void Main()
    {
        M([]); // C.M(ReadOnlySpan<int>) in C# 12, error in C# 13.
    }
}
```

### Exact element type is preferred over all else

In C# 13, we prefer an exact element type match, looking at conversions from expressions. This can result in a behavior change when involving
constants:

```cs
class C
{
    static void M1(ReadOnlySpan<byte> ros) {}
    static void M1(Span<int> s) {}

    static void M2(ReadOnlySpan<string> ros) {}
    static void M2(Span<CustomInterpolatedStringHandler> ros) {}

    static void Main()
    {
        M1([1]); // C.M(ReadOnlySpan<byte>) in C# 12, C.M(Span<int>) in C# 13

        M2([$"{1}"]); // C.M(ReadOnlySpan<string>) in C# 12, C.M(Span<CustomInterpolatedStringHandler>) in C# 13
    }
}
```

## Declaration of indexers in absence of proper declaration of DefaultMemberAttribute is no longer allowed.

***Introduced in Visual Studio 2022 version 17.13***

```cs
public interface I1
{
    public I1 this[I1 args] { get; } // error CS0656: Missing compiler required member 'System.Reflection.DefaultMemberAttribute..ctor'
}
```

## Default and params parameters are considered in method group natural type

***Introduced in Visual Studio 2022 version 17.13***

Previously the compiler [unexpectedly](https://github.com/dotnet/roslyn/issues/71333)
inferred different delegate type depending on the order of candidates in source
when default parameter values or `params` arrays were used. Now an ambiguity error is emitted.

```cs
using System;

class Program
{
    static void Main()
    {
        var x1 = new Program().Test1; // previously Action<long[]> - now error
        var x2 = new Program().Test2; // previously anonymous void delegate(params long[]) - now error

        x1();
        x2();
    }
}

static class E
{
    static public void Test1(this Program p, long[] a) => Console.Write(a.Length);
    static public void Test1(this object p, params long[] a) => Console.Write(a.Length);

    static public void Test2(this object p, params long[] a) => Console.Write(a.Length);
    static public void Test2(this Program p, long[] a) => Console.Write(a.Length);
}
```

Also in `LangVersion=12` or lower, `params` modifier must match across all methods to infer a unique delegate signature.
Note that this does not affect `LangVersion=13` and later because of [a different delegate inference algorithm](https://github.com/dotnet/csharplang/issues/7429).

```cs
var d = new C().M; // previously inferred Action<int[]> - now error CS8917: the delegate type could not be inferred

static class E
{
    public static void M(this C c, params int[] x) { }
}

class C
{
    public void M(int[] x) { }
}
```

A workaround is to use explicit delegate types instead of relying on `var` inference in those cases.

## `dotnet_style_require_accessibility_modifiers` now consistently applies to interface members

PR: https://github.com/dotnet/roslyn/pull/76324

Prior to this change, the analyzer for dotnet_style_require_accessibility_modifiers would simply ignore interface
members.  This was because C# initially disallowed modifiers for interface members entirely, having them always
be public.

Later versions of the language relaxed this restriction, allowing users to provide accessibility modifiers on
interface members, including a redundant `public` modifier.

The analyzer was updated to now enforce the value for this option on interface members as well.  The meaning
of the value is as follows:

1. `never`.  The analyzer does no analysis.  Redundant modifiers are allowed on all members.
2. `always`.  Redundant modifiers are always required on all members (including interface members).  For example:
   a `private` modifier on a class member, and a `public` modifier on an interface member.  This is the option to
   use if you feel that all members no matter what should state their accessibility explicitly.
4. `for_non_interface_members`.  Redundant modifiers are required on all members *that are not* part of an interface,
   but disallowed for interface members. For example: `private` will be required on private class members.  However,
   a public interface member will not be allowed to have redundant `public` modifiers.  This matches the standard
   modifier approach present prior to the language allowing modifiers on interface members.
5. `omit_if_default`.  Redundant modifiers are disallowed.  For example a private class member will be disallowed from
   using `private`, and a public interface member will be disallowed from using `public`.  This is the option to use
   if you feel that restating the accessibility when it matches what the language chooses by default is redundant and
   should be disallowed.
