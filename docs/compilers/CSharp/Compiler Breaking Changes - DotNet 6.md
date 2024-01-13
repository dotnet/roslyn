## This document lists known breaking changes in Roslyn in C# 10.0 which will be introduced with .NET 6.

1. <a name="1"></a>Beginning with C# 10.0, null suppression operator is no longer allowed in patterns.

    ```csharp
    void M(object o)
    {
        if (o is null!) {} // error
    }
    ```

2. <a name="2"></a>In C# 10, lambda expressions and method groups with inferred type are implicitly convertible to `System.MulticastDelegate`, and bases classes and interfaces of `System.MulticastDelegate` including `object`,
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

3. <a name="3"></a>In C#10, a lambda expression with inferred type may contribute an argument type that affects overload resolution.

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

4. <a name="4"></a><a name="roslyn-58339"></a>In Visual Studio 17.0.7, an error is reported in a `record struct` with a primary constructor if an explicit constructor has a `this()` initializer that invokes the implicit parameterless constructor. See [roslyn#58339](https://github.com/dotnet/roslyn/pull/58339).

    For instance, the following results in an error:
    ```csharp
    record struct R(int X, int Y)
    {
        // error CS8982: A constructor declared in a 'record struct' with parameter list must have a 'this'
        // initializer that calls the primary constructor or an explicitly declared constructor.
        public R(int x) : this() { X = x; Y = 0; }
    }
    ```

    The error could be resolved by invoking the primary constructor (as below) from the `this()` initializer, or by declaring a parameterless constructor that invokes the primary constructor.
    ```csharp
    record struct R(int X, int Y)
    {
        public R(int x) : this(x, 0) { } // ok
    }
    ```

5. <a name="5"></a><a name="roslyn-57925"></a>In Visual Studio 17.0.7, if a `struct` type declaration with no constructors includes initializers for some but not all fields, the compiler will report an error that all fields must be assigned.

    Earlier builds of 17.0 skipped _definite assignment analysis_ for the parameterless constructor synthesized by the compiler in this scenario and did not report unassigned fields, potentially resulting in instances with uninitialized fields. The updated analysis and error reporting is consistent with explicitly declared constructors. See [roslyn#57925](https://github.com/dotnet/roslyn/pull/57925).

    For instance, the following results in an error:
    ```csharp
    struct S // error CS0171: Field 'S.Y' must be fully assigned before control is returned to the caller
    {
        int X = 1;
        int Y;
    }
    ```

    To resolve the errors, declare a parameterless constructor and assign the fields that do not have initializers, or remove the existing field initializers so the compiler does not synthesize a parameterless constructor.
    (For compatibility with Visual Studio 17.1 which requires a `struct` with field initializers to [include an explicitly declared constructor](https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20DotNet%207.md#6), avoid adding initializers to the remaining fields without also declaring a constructor.)

    For instance, the error in the example above can be resolved by adding a constructor and assigning `Y`:
    ```csharp
    struct S
    {
        int X = 1;
        int Y;
        public S() { Y = 0; } // ok
    }
    ```

