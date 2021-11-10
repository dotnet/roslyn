## This document lists known breaking changes in Roslyn in C# 10.0 which will be introduced with .NET 6.

1. Beginning with C# 10.0, null suppression operator is no longer allowed in patterns.
    ```csharp
    void M(object o)
    {
        if (o is null!) {} // error
    }
    ```

2. In C# 10, lambda expressions and method groups with inferred type are implicitly convertible to `System.MulticastDelegate`, and bases classes and interfaces of `System.MulticastDelegate` including `object`,
and lambda expressions and method groups are implicitly convertible to `System.Linq.Expressions.Expression` and `System.Linq.Expressions.LambdaExpression`.
These are _function_type_conversions_.

    The new implicit conversions may change overload resolution in cases where the compiler searches iteratively for overloads and stops at the first type or namespace scope containing any applicable overloads.

    a. Instance and extension methods

    ```csharp
    class C
    {
        static void Main()
        {
            var c = new C();
            c.M(Main);      // C#9: E.M(); C#10: C.M()
            c.M(() => { }); // C#9: E.M(); C#10: C.M()
        }
    
        void M(System.Delegate d) { }
    }

    static class E
    {
        public static void M(this object x, System.Action y) { }
    }
    ```

    b. Base and derived methods

    ```csharp
    using System;
    using System.Linq.Expressions;

    class A
    {
        public void M(Func<int> f) { }
        public object this[Func<int> f] => null;
        public static A operator+(A a, Func<int> f) => a;
    }

    class B : A
    {
        public void M(Expression e) { }
        public object this[Delegate d] => null;
        public static B operator+(B b, Delegate d) => b;
    }

    class Program
    {
        static int F() => 1;

        static void Main()
        {
            var b = new B();
            b.M(() => 1);   // C#9: A.M(); C#10: B.M()
            _ = b[() => 2]; // C#9: A.this[]; C#10: B.this[]
            _ = b + F;      // C#9: A.operator+(); C#10: B.operator+()
        }
    }
    ```

    c. Method group or anonymous method conversion to `Expression` or `LambdaExpression`

    ```csharp
    using System;
    using System.Linq.Expressions;

    var c = new C();
    c.M(F);                         // error CS0428: Cannot convert method group 'F' to non-delegate type 'Expression'
    c.M(delegate () { return 1; }); // error CS1946: An anonymous method expression cannot be converted to an expression tree

    static int F() => 0;

    class C
    {
        public void M(Expression e) { }
    }

    static class E
    {
        public static void M(this object o, Func<int> a) { }
    }
    ```

3. In C#10, a lambda expression with inferred type may contribute an argument type that affects overload resolution.

    ```csharp
    using System;

    class Program
    {
        static void F(Func<Func<object>> f, int i) { }
        static void F(Func<Func<int>> f, object o) { }

        static void Main()
        {
            F(() => () => 1, 2); // C#9: F(Func<Func<object>>, int); C#10: ambiguous
        }
    }
    ```

