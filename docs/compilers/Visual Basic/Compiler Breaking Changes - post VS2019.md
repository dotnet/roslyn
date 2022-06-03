## This document lists known breaking changes in Roslyn in *Visual Studio 2019 Update 1* and beyond compared to *Visual Studio 2019*.

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. https://github.com/dotnet/roslyn/issues/38305 

Compiler used to generate incorrect code when a built-in comparison operator producing Boolean? was used
as an operand of a logical short-circuiting operator used as a Boolean expression.
For example, for an expression 
```
    GetBool3() = True AndAlso GetBool2()
```
    and functions
```
    Function GetBool2() As Boolean
        System.Console.WriteLine("GetBool2")
        Return True
    End Function
    Shared Function GetBool3() As Boolean?
         Return Nothing
    End Function
```

it is expected that GetBool2 function going to be called. This is also the expected behavior outside of
a Boolean expression, but in context of a Boolean expression the GetBool2 function was not called.
Compiler now generates code that follows language semantics and calls GetBool2 function for the expression above.