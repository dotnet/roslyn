## This document lists known breaking changes in Roslyn in *Visual Studio 2019 Update 1* and beyond compared to *Visual Studio 2019*.

*Breaking changes are formatted with a numerically delineated list so as to allow shorthand numerical references (e.g., "known break #1").
Each entry should include a short description of the breaking change, followed by either a link to the issue describing the full details of the change or the full details inline.*

1. https://github.com/dotnet/roslyn/issues/38305

The Visual Basic compiler previously generated code inconsistent with advertised language symantics when a built-in comparison operator producing `Boolean?` was used
as an operand of a logical, short-circuiting operator intended to be used as a `Boolean` expression.

For example, for the expression 
```
    GetBool3() = True AndAlso GetBool2()
```
using the function definitions
```
    Function GetBool2() As Boolean
        System.Console.WriteLine("GetBool2")
        Return True
    End Function

    Shared Function GetBool3() As Boolean?
         Return Nothing
    End Function
```

it is expected that `GetBool2()` is going to be called. This is also the expected behavior outside of
a `Boolean` expression, but in the context of the example `Boolean` expression `GetBool2()` was not called.
The Visual Basic compiler now follows language semantics and calls `GetBool2()`, generating the appropriate code in the case of the example expression above.
