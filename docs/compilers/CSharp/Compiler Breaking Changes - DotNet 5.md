## This document lists known breaking changes in Roslyn in C# 9.0 which will be introduced with .NET 5 (Visual Studio 2019 version 16.8).

1. Beginning with C# 9.0, when you switch on a value of type `byte` or `sbyte`, the compiler tracks which values have been handled and which have not.  Technically, we do so for all numeric types, but in practice it is only a breaking change for the types `byte` and `sbyte`.  For example, the following program contains a switch statement that explicitly handles *all* of the possible values of the switch's controlling expression
    ```csharp
    int M(byte b)
    {
        switch (b)
        {
            case 0: case 1: case 2: ...
                return 0;
        }
        return 1;
    }
    ```
    Since it returns from the method `M` on every value of the input, the following statement (`return 1;`) is considered not to be reachable and will produce a warning for unreachable code. Previously, the compiler did not analyze the set of values for completeness, and the following statement was considered reachable. This code compiles without warnings in C# 8.0 but will produce a warning in C# 9.0.

    Similarly, a switch expression that explicitly handles all values of an input of type `byte` or `sbyte` is considered to be complete.  For example, the following program

    ```csharp
    int M(byte b)
    {
        return b switch
        {
            0 => 0, 1 => 0, 2 => 0, ... byte.MaxValue => 0,
        };
    }
    ```

    Would produce a warning when compiled in C# 8.0 (the switch expression does not handle all values of its input), but produces no warning in C# 9.0. Conversely, adding a fallback branch to it would suppress the warning in C# 8.0, but cause an error in C# 9.0 (the case is handled by previous cases):

    ```csharp
    int M(byte b)
    {
        return b switch
        {
            0 => 0, 1 => 0, 2 => 0, ... byte.MaxValue => 0,
            _ => 1 // error in C# 9.0
        };
    }
    ```
2. `not` as a type in a pattern not permitted in C# 9.0
    In C# 9.0 we introduce a `not` pattern that negates the following pattern:
    ```csharp
        bool IsNull(object o) => o is not null;
    ```
    We recommend the pattern `not null` as the most clear way to check if a value is not null.

    Because a pattern in C# 9.0 can start with `not` as part of the pattern, we no longer consider a pattern that starts with `not` to be referencing a type named `not`.  The expression `o is not x` used to declare a variable `x` of type `not`.  Now, it checks that the input `o` is not the same as the constant named `x`.

3. `and` and `or` are not permitted as a pattern designator in C# 9.0
    In C# 9.0, we introduce `and` and `or` pattern combinators that combine other patterns:
    ```csharp
       bool IsSmall(int i) => o is 0 or 1 or 2;
    ```
    We also introduce type patterns so that you do not have to have an identifier naming the variable you are declaring:
    ```csharp
        bool IsSignedIntegral(object o) =>
            o is sbyte or short or int or long;
    ```
    Because the `and` and `or` combinators can follow a type pattern, the compiler interprets them as part of the pattern combinator rather than an identifier for the declaration pattern. Consequently, it is an error to use `or` or `and` as pattern variable identifiers starting with C# 9.0.
    
4. https://github.com/dotnet/roslyn/pull/44841 In *C# 9* and onwards the language views ambiguities between the `record` identifier as being
    either a type syntax or a record declaration as choosing the record declaration. The following examples will now be record declarations:

    ```C#
    abstract class C
    {
        record R2() { }
        abstract record R3();
    }
    ```

5. The fix for https://github.com/dotnet/roslyn/issues/44067 generates correct (different) code.
   In certain cases the compiler used to generate code whose behavior was ambiguous according
   to the CLR's specification. The compiler used to produce the warning CS1957 in those cases.
   The compiler now generates correct unambiguous code rather than reporting CS1957. Because
   it is possible that the runtime behavior of a program will change due to the change in our code
   generation strategy, this could be a breaking change. If your program did not elicit the warning
   CS1957 before then this does not affect your code.
