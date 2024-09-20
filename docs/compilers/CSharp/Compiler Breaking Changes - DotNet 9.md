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
