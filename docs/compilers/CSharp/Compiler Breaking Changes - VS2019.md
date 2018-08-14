**This document lists known breaking changes in Roslyn vNext (VS2019) from Roslyn 2.9 (VS2017).**


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


- https://github.com/dotnet/roslyn/issues/27748 C# will now produce a warning for an expression of the form `e is _`. The *is_type_expression* can be used with a type named `_`. However, in C# 8 we are introducing a discard pattern written `_` (which cannot be used as the *pattern* of an *is_pattern_expression*). To reduce confusion, there is a new warning when you write `e is _`:

    ``` none
    // (11,31): error CS8413: The name '_' refers to the type 'Program1._', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
    //     bool M1(object o) => o is _;
    ```
