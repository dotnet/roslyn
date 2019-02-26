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

- https://github.com/dotnet/roslyn/issues/20873 In Roslyn 2.3, the `includePrivateMembers` parameter of the `EmitOptions` constructor was changed to use `true` as its default value. This is a binary compatibility break. So clients using this API may have to re-compile, to pick up the new default value. An update will include a mitigation (ignoring the old default value when trying to emit a full assembly).

- https://github.com/dotnet/roslyn/issues/21582 In C# 7.1, when the default literal was introduced, it was accepted on the left-hand-side of a null-coalescing operator. For instance, in `default ?? 1`. In C# 7.2, this compiler bug was fixed to match the specification, and an error is produced instead ("Operator '??' cannot be applied to operand 'default'").

- https://github.com/dotnet/roslyn/issues/21979 In C# 7.1 and previous, the compiler permitted converting a method group, in which the receiver is of type `System.TypedReference`, to a delegate type. Such code would throw `System.InvalidProgramException` at runtime. In C# 7.2 this is a compile-time error. For example, the line with the comment, below, would cause the compiler to report an error:
``` c#
static Func<int> M(__arglist)
{
    ArgIterator ai = new ArgIterator(__arglist);
    while (ai.GetRemainingCount() > 0)
    {
        TypedReference tr = ai.GetNextArg();
        return tr.GetHashCode; // delegate conversion causes a subsequent System.InvalidProgramException
    }

    return null;
}
```

- https://github.com/dotnet/roslyn/issues/21485 In Roslyn 2.0, the `unsafe` modifier could be used on a local function without using the `/unsafe` flag on the compilation. In Roslyn 2.6 (Visual Studio 2017 verion 15.5) the compiler requires the `/unsafe` compilation flag, and produces a diagnostic if the flag is not used.

- https://github.com/dotnet/roslyn/issues/20210 In C# 7.2, there are some uses of the new pattern switch construct, in which the switch expression is a constant, for which the compiler will produce warnings or errors not previously produced.
``` c#
    switch (default(object))
    {
      case bool _:
      case true:  // new error: case subsumed by previous cases
      case false: // new error: case subsumed by previous cases
        break;
    }

    switch (1)
    {
      case 1 when true:
        break;
      default:
        break; // new warning: unreachable code
    }
```

- https://github.com/dotnet/roslyn/issues/20103 In C# 7.2, when testing a constant null expression against a declaration pattern in which the type is not inferred, the compiler will now warn that the expression is never of the provided type.
``` c#
const object o = null;
if (o is object res) { // warning CS0184: The given expression is never of the provided ('object') type
```

- https://github.com/dotnet/roslyn/issues/22578 In C# 7.1, the compiler would compute the wrong default value for an optional parameter of nullable type declared with the default literal. For instance, `void M(int? x = default)` would use `0` for the default parameter value, instead of `null`. In C# 7.2 (Visual Studio 2017 version 15.5), the proper default parameter value (`null`) is computed in such cases.

- https://github.com/dotnet/roslyn/issues/21979 In C# 7.2 (Visual Studio 2017 version 15.5) and previous it was allowed to convert to a delegate an instance method of a ref-like type such as `TypedReference`. Such operation invariable resulted in code that could not possibly run. The reason is that such conversion requires the receiver be boxed, which ref-like types cannot do.
In Visual Studio 2017 version 15.6 such conversions will be explicitly disallowed by the compiler and cause compile time errors.
Example: `Func<int> f = default(TypedReference).GetHashCode; // new error CS0123: No overload for 'GetHashCode' matches delegate 'Func<int>'`

- https://github.com/dotnet/roslyn/pull/23416 Before Visual Studio 2017 version 15.6 (Roslyn version 2.8) the compiler accepted `__arglist(...)` expressions with void-typed arguments. For instance, `__arglist(Console.WriteLine())`. But such program would fail at runtime. In Visual Studio 2017 version 15.6, this causes a compile-time error.

- https://github.com/dotnet/roslyn/pull/24023 In Visual Studio 2017 version 15.6, Microsoft.CodeAnalysis.CSharp.Syntax.CrefParameterSyntax constructor and Update(), the parameter refOrOutKeyword was renamed to refKindKeyword (source breaking change if you're using named arguments).

- Visual Studio 2017 15.0-15.5 shipped with a bug around definite assignment of local functions that did not produce definite assignment errors when an uncalled local function contains a nested lambda which captures a variable. For example:
    ```csharp
    void Method()
    {
        void Local()
        {
            Action a = () =>
            {
                int x;
                x++; // No error in 15.0 - 15.5
            };
        }
    }
    ```
    This is changed in 15.6 to now produce an error that the variable is not definitely assigned.

- Visual Studio 2017 version 15.7: https://github.com/dotnet/roslyn/issues/19792 C# compiler will now reject [IsReadOnly] symbols that should have an [InAttribute] modreq, but don't.

- Visual Studio 2017 version 15.7: https://github.com/dotnet/roslyn/pull/25131 C# compiler will now check `stackalloc T [count]` expressions to see if T matches constraints of `Span<T>`.

- https://github.com/dotnet/roslyn/issues/24806 In C# 7.2 and previous versions, it could be possible to observe cases when an RValue expression is reduced to a variable that can be passed by reference in the process of compiling.
For example `(x + 0)` becomes `x`. If such change happens in a context that allows both RValues and passing via direct reference (receiver of a struct call or an `in` parameter), then it could change the meaning of the code.
C# 7.3 will preserve the behavior of the rvalue where the value must always be passed via a copy.
Example:
   ```C#
   static int x = 123;
   static string Test1()
   {
       // cannot replace value of "x + 0" with a reference to "x"
       // since that would make the method see the mutations in M1();
       return (x + 0).ToString(M1());
   }

   static string M1()
   {
       x = 42;
       return "";
   }
   	```

- Visual Studio 2017 version 15.7: https://github.com/dotnet/roslyn/issues/25450 We added new restrictions on the use of the `default` expression in preparation for the planned addition of further pattern-matching features in C# 8.0:
  - If you write `e is default`, you will get the new error
    `error CS8363: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.`
  - If you write `case default:`, you will get the new error
    `error CS8313: A default literal 'default' is not valid as a case constant. Use another literal (e.g. '0' or 'null') as appropriate. If you intended to write the default label, use 'default:' without 'case'.`
- Visual Studio 2017 version 15.7: https://github.com/dotnet/roslyn/issues/25399 C# compiler will now produce errors if partial methods parameters have different ref-kinds in implementation vs definition.
- Visual Studio 2017 version 15.7: https://github.com/dotnet/roslyn/issues/23525 C# compiler will now produce errors if there was an invalid pdbpath supplied to an embedded pdb, instead of just writing it to the binary.
- Visutal Studio 2017 version 15.7: https://github.com/dotnet/csharplang/blob/master/proposals/csharp-7.3/tuple-equality.md#compatibility The "tuple equality" features (in C# 7.3) introduces built-in operators `==` and `!=` on tuple types. Those built-in operators take precedence over user-defined comparison operators on a custom `ValueTuple` type.

- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/22455 C# compiler will now produce errors if there was an "in" or an "out" argument to an "__arglist" call. "out" was always allowed, and "in" was introduced in 15.5.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/pull/27882 C# compiler will now produce errors on ref assigning a local to a parameter with a wider escape scope if it was a ref-like type.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/26418 C# compiler will now produce errors on out variable declarations that have "ref" or "ref readonly" ref kinds. Example: M(out ref int x);
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/27047 The C# compiler will now produce diagnostics for operators marked as obsolete when they are used as part of a tuple comparison.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/pull/27461 The method `LanguageVersionFacts.TryParse` is no longer an extension method.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/pull/27803 pattern matching now will produce errors when trying to return a stack bound value to an invalid escape scope.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/26743 C# will now reject expressions such as `public int* M => &this.Bar[0];` if `Bar` is a fixed field of the containing type.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/27772 invocation receivers will be checked for escape scope errors now in nested scope expressions.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/28117 C# will now reject ref assignments to byval parameters.
- Visual Studio 2017 version 15.8: https://github.com/dotnet/roslyn/issues/27049 C# will now reject base.Method() calls inside restricted types, because that requires boxing.
