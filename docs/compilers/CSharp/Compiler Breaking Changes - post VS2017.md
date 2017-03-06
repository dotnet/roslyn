Changes since VS2017 (C# 7)
===========================

- https://github.com/dotnet/roslyn/issues/17089
In C# 7, the compiler accepted a pattern of the form `dynamic identifier`, e.g. `if (e is dynamic x)`. This was accepted only if the expression `e` was statically of type `dynamic`. The compiler now rejects the use of the type `dynamic` for a pattern variable declaration, as no object's runtime type is `dynamic`.
