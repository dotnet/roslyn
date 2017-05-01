Changes since VS2017 (C# 7)
===========================

- https://github.com/dotnet/roslyn/issues/17089
In C# 7, the compiler accepted a pattern of the form `dynamic identifier`, e.g. `if (e is dynamic x)`. This was accepted only if the expression `e` was statically of type `dynamic`. The compiler now rejects the use of the type `dynamic` for a pattern variable declaration, as no object's runtime type is `dynamic`.

- https://github.com/dotnet/roslyn/issues/17674 In C# 7, the compiler accepted an assignment statement of the form `_ = M();` where M is a `void` method. The compiler now rejects that.

- https://github.com/dotnet/roslyn/issues/17173 Before C# 7.1, `csc` would accept leading zeroes in the `/langversion` option. Now it should reject it. Example: `csc.exe source.cs /langversion:07`.

- https://github.com/dotnet/csharplang/issues/415
In C# 7.0, elements in tuple literals can be named explicitly, but in C# 7.1, elements that aren't named explicitly will get an inferred named. This uses the same rules as members in anonymous types which aren't named explicitly.
For instance, `var t = (a, b.c, this.d);` will produce a tuple with element names "a", "c" and "d". As a result, an invocation on a tuple member may result in a different result than it did in C# 7.0.
Consider the case where the type of `a` is `System.Func<bool>` and you write `var local = t.a();`. This will now find the first element of the tuple and invoke it, whereas previously it could only mean "invoke an extension method named 'a'".

- https://github.com/dotnet/roslyn/issues/16870 In C# 7.0 and before C# 7.1, the compiler accepted self-assignments in deconstruction-assignment. The compiler now produces a warning for that. For instance, in `(x, y) = (x, 2);`.
