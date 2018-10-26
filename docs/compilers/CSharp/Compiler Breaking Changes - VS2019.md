**This document lists known breaking changes in Roslyn 3.0 (Visual Studio 2019) from Roslyn 2.* (Visual Studio 2017)

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

- https://github.com/dotnet/roslyn/issues/27800 C# will now preserve left-to-right evaluation for compound assignment addition/subtraction expressions where the left-hand side is dynamic. In this example code:
    ``` C#
    class DynamicTest
    {
        public int Property { get; set; }
        static dynamic GetDynamic() => return new DynamicTest();
        static int GetInt() => 1;
        public static void Main() => GetDynamic().Property += GetInt();
    }
    ```
  - Previous versions of Roslyn would have evaluated this as:
    1. GetInt()
    2. GetDynamic()
    3. get_Property
    4. set_Property
  - We now evaluate it as
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

6. C# 8 produces an error when a `switch` statement uses a constant named `_` as a case label. This would now be an error if you used such a constant previously. It is now ambiguous with the new discard pattern.

    ``` none
    (1,18): error CS8523: The discard pattern is not permitted as a case label in a switch statement. Use 'case var _:' for a discard pattern, or 'case @_:' for a constant named '_'.
    switch(e) { case _: break; }
    ```
