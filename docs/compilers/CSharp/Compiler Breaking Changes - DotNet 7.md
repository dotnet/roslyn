## This document lists known breaking changes in Roslyn after .NET 6 all the way to .NET 7.

1. <a name="1"></a>In Visual Studio 17.1, the contextual keyword `var` cannot be used as an explicit lambda return type.

    ```csharp
    using System;

    F(var () => default);  // error: 'var' cannot be used as an explicit lambda return type
    F(@var () => default); // ok

    static void F(Func<var> f) { }

    class var { }
    ```

2. <a name="2"></a>In Visual Studio 17.1, indexers that take an interpolated string handler and require the receiver as an input for the constructor cannot be used in an object initializer.

    ```cs
    using System.Runtime.CompilerServices;
    _ = new C { [$""] = 1 }; // error: Interpolated string handler conversions that reference the instance being indexed cannot be used in indexer member initializers.

    class C
    {
        public int this[[InterpolatedStringHandlerArgument("")] CustomHandler c]
        {
            get => throw null;
            set => throw null;
        }
    }

    [InterpolatedStringHandler]
    class CustomHandler
    {
        public CustomHandler(int literalLength, int formattedCount, C c) {}
    }
    ```

3. <a name="3"></a>In Visual Studio 17.1, `ref`/`ref readonly`/`in`/`out` are not allowed to be used on return/parameters of a method attributed with `UnmanagedCallersOnly`.  
https://github.com/dotnet/roslyn/issues/57025

    ```cs
    using System.Runtime.InteropServices;
    [UnmanagedCallersOnly]
    static ref int M1() => throw null; // error CS8977: Cannot use 'ref', 'in', or 'out' in a method attributed with 'UnmanagedCallersOnly'.

    [UnmanagedCallersOnly]
    static ref readonly int M2() => throw null; // error CS8977: Cannot use 'ref', 'in', or 'out' in a method attributed with 'UnmanagedCallersOnly'.

    [UnmanagedCallersOnly]
    static void M3(ref int o) => throw null; // error CS8977: Cannot use 'ref', 'in', or 'out' in a method attributed with 'UnmanagedCallersOnly'.

    [UnmanagedCallersOnly]
    static void M4(in int o) => throw null; // error CS8977: Cannot use 'ref', 'in', or 'out' in a method attributed with 'UnmanagedCallersOnly'.

    [UnmanagedCallersOnly]
    static void M5(out int o) => throw null; // error CS8977: Cannot use 'ref', 'in', or 'out' in a method attributed with 'UnmanagedCallersOnly'.
    ```

4. <a name="4"></a>Beginning with C# 11.0, `Length` and `Count` properties on countable and indexable types
are assumed to be non-negative for purpose of subsumption and exhaustiveness analysis of patterns and switches.
Those types can be used with implicit Index indexer and list patterns.

    ```csharp
    void M(int[] i)
    {
        if (i is { Length: -1 }) {} // error: impossible under assumption of non-negative length
    }
    ```

5.  <a name="5"></a>Starting with Visual Studio 17.1, format specifiers in interpolated strings can not contain curly braces (either `{` or `}`). In previous versions `{{` was interpreted as an escaped `{` and `}}` was interpreted as an escaped `}` char in the format specifier. Now the first `}` char in a format specifier ends the interpolation, and any `{` char is an error.
https://github.com/dotnet/roslyn/issues/57750

    ```csharp
    using System;

    Console.WriteLine($"{{{12:X}}}");

    //prints now: "{C}" - not "{X}}"
    ```

6. <a name="6"></a><a name="roslyn-58581"></a>In Visual Studio 17.1, `struct` type declarations with field initializers must include an explicitly declared constructor. Additionally, all fields must be definitely assigned in `struct` instance constructors that do not have a `: this()` initializer so any previously unassigned fields must be assigned from the added constructor or from field initializers. See [csharplang#5552](https://github.com/dotnet/csharplang/issues/5552), [roslyn#58581](https://github.com/dotnet/roslyn/pull/58581).

    For instance, the following results in an error in 17.1:
    ```csharp
    struct S
    {
        int X = 1; // error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
        int Y;
    }
    ```

    The error could be resolved by adding a constructor and assigning the other field.
    ```csharp
    struct S
    {
        int X = 1;
        int Y;
        public S() { Y = 0; } // ok
    }
    ```
