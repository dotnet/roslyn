Changes since VS2017 (VB 15)
===========================

- https://github.com/dotnet/csharplang/issues/415
In VB 15, elements in tuple literals can be named explicitly, but in VB 15.3, elements that aren't named explicitly will get an inferred named. This uses the same rules as members in anonymous types which aren't named explicitly.
For instance, `Dim t = (a, b.c, Me.d)` will produce a tuple with element names "a", "c" and "d". As a result, an invocation on a tuple member may result in a different result than it did in VB 15.
Consider the case where the type of `a` is `System.Func(Of Boolean)` and you write `Dim local = t.a()`. This will now find the first element of the tuple and invoke it, whereas previously it could only mean "invoke an extension method named 'a'".
