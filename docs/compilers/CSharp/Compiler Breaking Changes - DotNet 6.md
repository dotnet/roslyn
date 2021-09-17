## This document lists known breaking changes in Roslyn in C# 10.0 which will be introduced with .NET 6.

1. Beginning with C# 10.0, null suppression operator is no longer allowed in patterns.
    ```csharp
    void M(object o)
    {
        if (o is null!) {} // error
    }
    ```

2. In C# 10, lambda expressions and method groups with inferred type are implicitly convertible to `System.MulticastDelegate`, and bases classes and interfaces of `System.MulticastDelegate` including `object`,
and lambda expressions are implicitly convertible to `System.Linq.Expressions.Expression` and `System.Linq.Expressions.LambdaExpression`.
These are _function_type_conversions_.

    In method overload resolution, if there is an applicable overload that relies on a _function_type_conversion_ of a lambda expression or method group,
    and the closest applicable extension method overload with a conversion to a strongly-type delegate parameter is in an enclosing namespace,
    the overload with the _function_type_conversion_ will be chosen.

    ```csharp
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

2. In C# 10, lambda expressions and method groups with inferred type are implicitly convertible to `System.MulticastDelegate`, and bases classes and interfaces of `System.MulticastDelegate` including `object`,
and lambda expressions are implicitly convertible to `System.Linq.Expressions.Expression` and `System.Linq.Expressions.LambdaExpression`.
These are _function_type_conversions_.

    In binary operator overload resolution, if there is an applicable operator overload that relies on a _function_type_conversion_ of a lambda expression or method group,
    and the closest applicable operator overload with a conversion to a strongly-type delegate parameter is defined in a base type,
    the overload with the _function_type_conversion_ will be chosen.

    ```csharp
    using System;

    var b = new B();
    _ = b + F;         // C#9: A.operator+(); C#10: B.operator+()
    _ = b + (() => 2); // C#9: A.operator+(); C#10: B.operator+()

    static int F() => 1;

    class A
    {
        public static A operator+(A a, Func<int> f) => a;
    }

    class B : A
    {
        public static B operator+(B b, Delegate d) => b;
    }
    ```
