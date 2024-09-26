# This document lists known breaking changes in Roslyn after .NET 8 all the way to .NET 9.


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
