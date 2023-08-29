# This document lists known breaking changes in Roslyn after .NET 7 all the way to .NET 8.

## `ref` arguments can be passed to `in` parameters

***Introduced in Visual Studio 2022 version 17.8p2***

Feature [`ref readonly` parameters](https://github.com/dotnet/csharplang/issues/6010) relaxed overload resolution
allowing `ref` arguments to be passed to `in` parameters when `LangVersion` is set to 12 or later.
This can lead to behavior or source breaking changes:

```cs
var i = 5;
System.Console.Write(new C().M(ref i)); // prints "E" in C# 11, but "C" in C# 12
System.Console.Write(E.M(new C(), ref i)); // workaround: prints "E" always

class C
{
    public string M(in int i) => "C";
}
static class E
{
    public static string M(this C c, ref int i) => "E";
}
```

```cs
var i = 5;
System.Console.Write(C.M(null, ref i)); // prints "1" in C# 11, but fails with an ambiguity error in C# 12
System.Console.Write(C.M((I1)null, ref i)); // workaround: prints "1" always

interface I1 { }
interface I2 { }
static class C
{
    public static string M(I1 o, ref int x) => "1";
    public static string M(I2 o, in int x) => "2";
}
```
