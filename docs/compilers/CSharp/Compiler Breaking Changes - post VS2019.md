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

3. https://github.com/dotnet/roslyn/issues/35684 In C# `7.1` the resolution of a binary operator with a `default` literal and unconstrained type parameter could result in using an object equality and giving the literal a type `object`.
    For example, given a variable `t` of an unconstrained type `T`, `t == default` would be improperly allowed and emitted as `t == default(object)`.
    In *Visual Studio 2019 version 16.4* this scenario will now produce an error.

4. In C# `7.1`, `default as TClass` and `using (default)` were allowed. In *Visual Studio 2019 version 16.4* those scenarios will now produce errors.

5. https://github.com/dotnet/roslyn/issues/38240 Visual Studio 2019 version 16.3 incorrectly allowed for a `static` local function to create a delegate using a delegate creation expression whose target requires capturing state. For example:

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

6. https://github.com/dotnet/roslyn/issues/37527 The constant folding behavior of the compiler differed depending on your host architecture when converting a floating-point constant to an integral type where that conversion would be a compile-time error if not in an `unchecked` context.  We now yield a zero result for such conversions on all host architectures.

7. https://github.com/dotnet/roslyn/issues/38226 When there exists a common type among those arms of a switch expression that have a type, but there are some arms that have an expression without a type (e.g. `null`) that cannot convert to that common type, the compiler improperly inferred that common type as the natural type of the switch expression. That would cause an error.  In Visual Studio 2019 Update 4, we fixed the compiler to no longer consider such a switch expression to have a common type.  This may permit some programs to compile without error that would produce an error in the previous version.

8. User-defined unary and binary operators are re-inferred from the nullability of the arguments. This may result in additional warnings:
    ```C#
    struct S<T>
    {
        public static S<T> operator~(S<T> s) { ... }
        public T F;
    }
    static S<T> Create<T>(T t) { ... }
    static void F()
    {
        object o = null;
        var s = ~Create(o);
        s.F.ToString(); // warning: s.F may be null
    }
    ```

9. https://github.com/dotnet/roslyn/issues/38469 While looking for a name in an interface in context where only types are allowed,
the compiler didn't look for the name in base interfaces of the interface. Lookup could succeed by finding a type up the containership
hierarchy or through usings. We now look in base interfaces and find types declared within them, if any match the name. The type
could be different than the one that compiler used to find.

10. https://github.com/dotnet/roslyn/issues/38427 C# `7.0` incorrectly allowed duplicate type constraints with tuple name differences. In *Visual Studio 2019 version 16.4* this is an error.
    ```C#
    class C<T> where T : I<(int a, int b)>, I<(int c, int d)> { } // error
    ```

11. Previously, the language version was not checked for `this ref` and `this in` orderings of parameter modifiers. In *Visual Studio 2019 version 16.4* these orderings produce an error with langversion below 7.2. See https://github.com/dotnet/roslyn/issues/38486

12. https://github.com/dotnet/roslyn/issues/40092 Previously, the compiler allowed `extern event` declarations to have initializers, in violation of the C# language specification. In *Visual Studio 2019 version 16.5* such declarations produce compile errors.
    ```C#
    class C
    {
        extern event System.Action E = null; // error
    }
    ```

13. https://github.com/dotnet/roslyn/issues/10492 Under some circumstances, the compiler would accept an expression that does not obey the rules of the language grammar.  Examples include
    - `e is {} + c`
    - `e is T t + c`

    These all have in common that the left operand is of looser precedence than the `+` operator, but the left operand does not end in an expression so it cannot "consume" the addition.  Such expressions will no longer be permitted in Visual Studio 2019 version 16.5 and later.

14. In Visual Studio version 15.0 and onwards, the compiler would allow compiling some malformed definitions of the `System.ValueTuple` types. For instance, one with a private `Item1` field. In *Visual Studio 2019 version 16.5* such definitions produce warnings.

15. In Visual Studio version 15.0 and onwards, the compiler APIs would produce non-generic tuple symbols (`Arity` would be 0, would have no type arguments, would be original definitions). In *Visual Studio 2019 version 16.5* a 2-tuple symbol has `Arity` 2, and a 9-tuple symbol has `Arity` 8 (since it is a `ValueTuple'8`).


16. In *Visual Studio 2019 version 16.5* and language version 8.0 and later, the compiler will no longer accept `throw null` when there is no type `System.Exception`.

17. https://github.com/dotnet/roslyn/issues/39852 Previously the compiler would allow an invocation of an implicit index or range indexer to specify any named argument. In *Visual Studio 2019 version 16.5* argument names are no longer permitted for these invocations.

18. https://github.com/dotnet/roslyn/issues/36039 In *Visual Studio 2019 version 16.3* and onwards, the compiler only honored nullability flow annotation attributes for callers of an API. In *Visual Studio 2019 version 16.5* the compiler honors those attributes within member bodies.
For instance, returning `default` from a method declared with a `[MaybeNull] T` return type will no longer warn.
Similarly, a value from a `[DisallowNull] ref string? p` parameter will be assumed to be not-null when first read.
On the other hand, we'll warn for returning a `null` from a method declared with `[NotNull] string?` return type, and we'll treat the value from a `[AllowNull] ref string p` parameter as maybe-null.
Conditional attributes are treated leniently. For instance, no warning will be produced for assigning a maybe-null value to an `[MaybeNullWhen(...)] out string p` parameter.

19. https://github.com/dotnet/roslyn/issues/36039 In *Visual Studio 2019 version 16.3* and onwards, the compiler did not check the usage of nullable flow annotation attributes, such as `[MaybeNull]` or `[NotNull]`, in overrides or implementations. In *Visual Studio 2019 version 16.5*, those usages are checked to respect null discipline. For example:
``` csharp
public class Base<T>
{
    [return: NotNull]
    public virtual T M() { ... }
}
public class Derived : Base<string?>
{
    public override string? M() { ... } // Derived.M doesn't honor the nullability declaration made by Base.M with its [NotNull] attribute
}
```

20. In *Visual Studio 2019 version 16.9* and greater, the compiler will no longer allow query syntax over constrained type parameters. For example:

```csharp
using System;
using System.Collections.Generic;
 
class C
{
    static void M<T>() where T : C
    {
        var q = from x in T select x; // error CS0119: 'T' is a type parameter, which is not valid in the given context
    }

    static Func<Func<int, object>, IEnumerable<object>> Select = null;
}
```

21. https://github.com/dotnet/roslyn/issues/50182 In *Visual Studio 2019 version 16.9* and greater, the compiler will no longer allow `await foreach` over variables that implement a malformed version of `IAsyncEnumerable` that has a non-optional `CancellationToken` parameter.

22. https://github.com/dotnet/roslyn/issues/49596 In *Visual Studio 2019 version 16.9* and greater, conversions from `sbyte` or `short` to `nuint` require explicit casts,
and binary operations with `sbyte` or `short` and `nuint` arguments require explicit casts for one or both operands for `+`, `-`, `*`, `/`, `%`, `<`, `>`, `<=`, `>=`, `==`, `!=`, `|`, `&`, and `^`.
