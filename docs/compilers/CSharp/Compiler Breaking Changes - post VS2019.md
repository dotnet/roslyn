## This document lists known breaking changes in Roslyn in *Visual Studio 2019 Update 1* and beyond compared to *Visual Studio 2019*.

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. https://github.com/dotnet/roslyn/issues/34882 A new feature in C# `8.0` will permit using a constant pattern with an open type.  For example, the following code will be permitted:
    ``` c#
    bool M<T>(T t) => t is null;
    ```
However, in *Visual Studio 2019* we improperly permitted this to compile in language versions `7.0`, `7.1`, `7.2`, and `7.3`.  In *Visual Studio 2019 Update 1* we will make it an error (as it was in *Visual Studio 2017*), and suggest updating to `preview` or `8.0`.

2. https://github.com/dotnet/roslyn/issues/37527 The constant folding behavior of the compiler differed depending on your host architecture when converting a floating-point constant to an integral type where that conversion would be a compile-time error if not in an `unchecked` context.  We now yield a zero result for such conversions on all host architectures.
