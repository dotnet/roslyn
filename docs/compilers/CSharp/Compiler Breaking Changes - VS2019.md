**This document lists known breaking changes in Roslyn vNext (VS2019) from Roslyn 2.9 (VS2017) and native C# compiler (VS2013 and previous).**


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
