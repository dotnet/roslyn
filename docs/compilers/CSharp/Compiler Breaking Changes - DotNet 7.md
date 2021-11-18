## This document lists known breaking changes in Roslyn after .NET 6 all the way to .NET 7.

1. In Visual Studio 17.1, the contextual keyword `var` cannot be used as an explicit lambda return type.

    ```csharp
    using System;

    F(var () => default);  // error: 'var' cannot be used as an explicit lambda return type
    F(@var () => default); // ok

    static void F(Func<var> f) { }

    class var { }
    ```

2. In Visual Studio 17.1, indexers that take an interpolated string handler and require the receiver as an input for the constructor cannot be used in an object initializer.

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


3. In Visual Studio 17.1, `ref`/`ref readonly`/`in`/`out` are not allowed to be used on return/parameters of a method attributed with `UnmanagedCallersOnly`.  
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

4. Starting with Visual Studio 17.1, format specifiers can not contain curly braces. In previous versions `}}` were interpreted as an escaped `}` char in the format specifier. Now the first `}` char in a format specifier ends the interpolation.
https://github.com/dotnet/roslyn/issues/5775

```csharp
using System;

Console.WriteLine($"{{{12:X}}}");

//prints now: "{C}" - not "{X}}"
```

