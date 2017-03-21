Changes since VS2017 (C# 7)
===========================

- https://github.com/dotnet/roslyn/issues/17089
In C# 7, the compiler accepted a pattern of the form `dynamic identifier`, e.g. `if (e is dynamic x)`. This was accepted only if the expression `e` was statically of type `dynamic`. The compiler now rejects the use of the type `dynamic` for a pattern variable declaration, as no object's runtime type is `dynamic`.

- https://github.com/dotnet/roslyn/issues/17674 In C# 7, the compiler accepted an assignment statement of the form `_ = M();` where M is a `void` method. The compiler now rejects that.

- https://github.com/dotnet/roslyn/issues/17173 Before C# 7.1, `csc` would accept leading zeroes in the `/langversion` option. Now it should reject it. Example: `csc.exe source.cs /langversion:07`.
