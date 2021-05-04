## This document lists known breaking changes in Roslyn after .NET 5.

1. https://github.com/dotnet/roslyn/issues/46044 In C# 9.0 (Visual Studio 16.9), a warning is reported when assigning `default` to, or when casting a possibly `null` value to a type parameter type that is not constrained to value types or reference types. To avoid the warning, the type can be annotated with `?`.
    ```C#
    static void F1<T>(object? obj)
    {
        T t1 = default; // warning CS8600: Converting possible null value to non-nullable type
        t1 = (T)obj;    // warning CS8600: Converting possible null value to non-nullable type

        T? t2 = default; // ok
        t2 = (T?)obj;    // ok
    }
    ```

2. https://github.com/dotnet/roslyn/pull/50755 In .NET 5.0.200 (Visual Studio 16.9), if there is a common type between the two branches of a conditional expression, that type is the type of the conditional expression.

   This is a breaking change from 5.0.103 (Visual Studio 16.8) which due to a bug incorrectly used the target type of the conditional expression as the type even if there was a common type between the two branches.

   This latest change aligns the compiler behavior with the C# specification and with versions of the compiler before .NET 5.0.
    ```C#
    static short F1(bool b)
    {
        // 16.7, 16.9          : CS0266: Cannot implicitly convert type 'int' to 'short'
        // 16.8                : ok
        // 16.8 -langversion:8 : CS8400: Feature 'target-typed conditional expression' is not available in C# 8.0
        return b ? 1 : 2;
    }

    static object F2(bool b, short? a)
    {
        // 16.7, 16.9          : int
        // 16.8                : short
        // 16.8 -langversion:8 : CS8400: Feature 'target-typed conditional expression' is not available in C# 8.0
        return a ?? (b ? 1 : 2);
    }
    ```

3. https://github.com/dotnet/roslyn/issues/52630 In C# 9 (.NET 5, Visual Studio 16.9), it is possible that a record uses a hidden member from a base type as a positional member. In Visual Studio 16.10, this is now an error:
```csharp
record Base
{
    public int I { get; init; }
}
record Derived(int I) // The positional member 'Base.I' found corresponding to this parameter is hidden.
    : Base
{
    public int I() { return 0; }
}
```

4. In C# 10, method groups are implicitly convertible to `System.Delegate`, and lambda expressions are implicitly convertible to `System.Delegate` and `System.Linq.Expressions.Expression`.

    This is a breaking change to overload resolution if there exists an overload with a `System.Delegate` or `System.Linq.Expressions.Expression` parameter that is applicable and the closest applicable overload with a strongly-typed delegate parameter is in an enclosing namespace.

    ```C#
    class C
    {
        static void Main()
        {
            var c = new C();
            c.M(Main);      // C#9: E.M(), C#10: C.M()
            c.M(() => { }); // C#9: E.M(), C#10: C.M()
        }
    
        void M(System.Delegate d) { }
    }

    static class E
    {
        public static void M(this object x, System.Action y) { }
    }
    ```