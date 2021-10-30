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
