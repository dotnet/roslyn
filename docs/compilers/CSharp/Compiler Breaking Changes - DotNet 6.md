## This document lists known breaking changes in Roslyn in C# 9.0 introduced with .NET 6.

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
