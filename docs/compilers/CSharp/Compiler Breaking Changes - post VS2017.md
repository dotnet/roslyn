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

- https://github.com/dotnet/roslyn/issues/19151 The compiler is now more precise in detecting erroneous pattern-matching operations because the expression could not possibly match the pattern. The following situations now cause an error:
  1. `bool M(int? i) => i is long l; // error CS8121: An expression of type 'int?' cannot be handled by a pattern of type 'long'.`
  2. and other cases where the integral types are not the same
  3. the same error can occur in other pattern-matching contexts (i.e. `switch`)

 - https://github.com/dotnet/roslyn/issues/17963 In C# 7.0 and before C# 7.1, the compiler used to consider tuple element name differences and dynamic-ness differences as significant when using the "as" operator with a nullable tuple type. For instance, `(1, 1) as (int, int x)?` and `(1, new object()) as (int, dynamic)?` would always be considered `null`. The compiler now produces the correct value, instead of `null`, and no "always null" warning.

- https://github.com/dotnet/roslyn/issues/20208 In C# 7.0 and before C# 7.2, the compiler considered a local declared with a `var` type and a tuple literal value to be used. So it would not report a warning if that local was not used. The compiler now produces a diagnostic. For example, `var unused = (1, 2);`.
