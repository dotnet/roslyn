**This document lists known breaking changes in Roslyn 3.0 (Visual Studio 2019) from Roslyn 2.* (Visual Studio 2017)

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

