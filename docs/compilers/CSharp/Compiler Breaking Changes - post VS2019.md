## This document lists known breaking changes in Roslyn in *Visual Studio 2019 Update 1* and beyond compared to *Visual Studio 2019*.

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. https://github.com/dotnet/roslyn/issues/34882 A new feature in C# `8.0` will permit using a constant pattern with an open type.  For example, the following code will be permitted:
    ``` c#
    bool M<T>(T t) => t is null;
    ```
    However, in *Visual Studio 2019* we improperly permitted this to compile in language versions `7.0`, `7.1`, `7.2`, and `7.3`.  In *Visual Studio 2019 Update 1* we will make it an error (as it was in *Visual Studio 2017*), and suggest updating to `preview` or `8.0`.

2. https://github.com/dotnet/roslyn/issues/38129 Visual Studio 2019 version 16.3 incorrectly allowed for a `static` local function to call a non-`static` local function. For example:

    ```c#
    void M()
    {
        void Local() {}

        static void StaticLocal()
        {
            Local();
        }
    }
    ```

    Such code will produce an error in version 16.4.

3. https://github.com/dotnet/roslyn/issues/38240 Visual Studio 2019 version 16.3 incorrectly allowed for a `static` local function to create a delegate using a delegate creation expression whose target requires capturing state. For example:

    ```c#
    void M()
    {
        object local;

        static void F()
        {
            _ = new Func<int>(local.GetHashCode);
        }
    }
    ```

    Such code will produce an error in version 16.4.
