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

10. https://github.com/dotnet/csharplang/blob/master/meetings/2019/LDM-2019-09-11.md In C# `8.0` no warning is reported at the production or dereference of a maybe-null value for a type that is a type parameter that cannot be annotated with `?`, except if the value was produced by `default(T)`.
In Visual Studio version 16.4, the nullable analysis will be more stringent for such values. Whenever such a value is produced, as warning will be reported. For instance, when invoking a method that returns a `[MaybeNull]T`.

11. https://github.com/dotnet/roslyn/issues/38427 C# `7.0` incorrectly allowed duplicate type constraints with tuple name differences. In *Visual Studio 2019 version 16.4* we will make it an error.
    ```C#
    class C<T> where T : I<(int a, int b)>, I<(int c, int d)> { } // error
    ```

12. Previously, the language version was not checked for `this ref` and `this in` orderings of parameter modifiers. In *Visual Studio 2019 version 16.4* these orderings produce an error with langversion below 7.2. See https://github.com/dotnet/roslyn/issues/38486
