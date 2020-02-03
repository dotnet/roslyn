**This document lists known breaking changes in Roslyn 3.0 (Visual Studio 2019) from Roslyn 2.\* (Visual Studio 2017)**

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. https://github.com/dotnet/roslyn/issues/27800 C# will now preserve left-to-right evaluation for compound assignment addition/subtraction expressions where the left-hand side is dynamic. In this example code:

    ``` C#
    class DynamicTest
    {
        public int Property { get; set; }
        static dynamic GetDynamic() => return new DynamicTest();
        static int GetInt() => 1;
        public static void Main() => GetDynamic().Property += GetInt();
    }
    ```

    Previous versions of Roslyn would have evaluated this as:
    1. GetInt()
    2. GetDynamic()
    3. get_Property
    4. set_Property

    We now evaluate it as:
    1. GetDynamic()
    2. get_Property
    3. GetInt()
    4. set_Property

2. Previously, we allowed adding a module with `Microsoft.CodeAnalysis.EmbeddedAttribute` or `System.Runtime.CompilerServices.NonNullTypesAttribute` types declared in it.
    In Visual Studio 2019, this produces a collision error with the injected declarations of those types.

3. Previously, you could refer to a `System.Runtime.CompilerServices.NonNullTypesAttribute` type declared in a referenced assembly.
    In Visual Studio 2019, the type from assembly is ignored in favor of the injected declaration of that type.

4. https://github.com/dotnet/roslyn/issues/29656 Previously, ref-returning async local functions would compile, by ignoring the `ref` modifier of the return type.
    In Visual Studio 2019, this now produces an error, just like ref-returning async methods do.

5. https://github.com/dotnet/roslyn/issues/27748 C# will now produce a warning for an expression of the form `e is _`. The *is_type_expression* can be used with a type named `_`. However, in C# 8 we are introducing a discard pattern written `_` (which cannot be used as the *pattern* of an *is_pattern_expression*). To reduce confusion, there is a new warning when you write `e is _`:

    ``` none
    (11,31): warning CS8413: The name '_' refers to the type 'Program1._', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
        bool M1(object o) => o is _;
    ```

6. C# 8 produces a warning when a `switch` statement uses a constant named `_` as a case label.

    ``` none
    (1,18): warning CS8512: The name '_' refers to the constant '_', not the discard pattern. Use 'var _' to discard the value, or '@_' to refer to a constant by that name.
    switch(e) { case _: break; }
    ```

7. In C# 8.0, the parentheses of a switch statement are optional when the expression being switched on is a tuple expression, because the tuple expression has its own parentheses:

    ``` c#
    switch (a, b)
    ```

    Due to this the `OpenParenToken` and `CloseParenToken` fields of a `SwitchStatementSyntax` node may now sometimes be empty.

8. In an *is-pattern-expression*, a warning is now issued when a constant expression does not match the provided pattern because of its value. Such code was previously accepted but gave no warning. For example:

    ``` c#
    if (3 is 4) // warning: the given expression never matches the provided pattern.
    ```

    We also issue a warning when a constant expression *always* matches a constant pattern in an *is-pattern-expression*. For example

    ``` c#
    if (3 is 3) // warning: the given expression always matches the provided constant.
    ```

    Other cases of the pattern always matching (e.g. `e is var t`) do not trigger a warning, even when they are known by the compiler to produce an invariant result.

9. https://github.com/dotnet/roslyn/issues/26098 In C# 8, we give a warning when an is-type expression is always `false` because the input type is an open class type and the type it is tested against is a value type:

    ``` c#
    class C<T> { }
    void M<T>(C<T> x)
    {
        if (x is int) { } // warning: the given expression is never of the provided ('int') type.
    }
    ```

    previously, we gave the warning only in the reverse case

    ``` c#
    void M<T>(int x)
    {
        if (x is C<T>) { } // warning: the given expression is never of the provided ('C<T>') type.
    }
    ```

10. Previously, reference assemblies were emitted including embedded resources. In Visual Studio 2019, embedded resources are no longer emitted into ref assemblies.
  See https://github.com/dotnet/roslyn/issues/31197

11. Ref structs now support disposal via pattern. A ref struct enumerator with an accessible `void Dispose()` instance method will now have it invoked at the end of enumeration, regardless of whether the struct type implements IDisposable:

    ``` c#
    public class C
    {
        public ref struct RefEnumerator
        {
            public int Current => 0;
            public bool MoveNext() => false;
            public void Dispose() => Console.WriteLine("Called in C# 8.0 only");
        }

        public RefEnumerator GetEnumerator() => new RefEnumerator();

        public static void Main()
        {
            foreach(var x in new C())
            {
            }
            // RefEnumerator.Dispose() will be called here in C# 8.0
        }
    }
    ```
