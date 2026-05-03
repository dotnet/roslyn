' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertAnonymousTypeToTuple
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertAnonymousTypeToTuple)>
    Partial Public Class ConvertAnonymousTypeToTupleTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertAnonymousTypeToTupleCodeRefactoringProvider()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact>
        Public Async Function ConvertSingleAnonymousType() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function NotOnEmptyAnonymousType() As Task
            Await TestMissingInRegularAndScriptAsync("
class Test
    sub Method()
        dim t1 = [||]new with { }
    end sub
end class
")
        End Function

        <Fact>
        Public Async Function NotOnSingleFieldAnonymousType() As Task
            Await TestMissingInRegularAndScriptAsync("
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1 }
    end sub
end class
")
        End Function

        <Fact>
        Public Async Function ConvertSingleAnonymousTypeWithInferredName() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { .a = 1, b }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = (a:=1, b)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function ConvertMultipleInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function ConvertMultipleInstancesAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function OnlyConvertMatchingTypesInSameMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, b }
        dim t3 = new with { .a = 4 }
        dim t4 = new with { .b = 5, .a = 6 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b)
        dim t3 = new with { .a = 4 }
        dim t4 = new with { .b = 5, .a = 6 }
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestFixAllInSingleMethod() As Task
            Dim text = "
class Test
    sub Method(b as integer)
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, b }
        dim t3 = new with { .a = 4 }
        dim t4 = new with { .b = 5, .a = 6 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method(b as integer)
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b)
        dim t3 = new with { .a = 4 }
        dim t4 = (b:=5, a:=6)
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact>
        Public Async Function TestFixNotAcrossMethods() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim t2 = (a:=3, b:=4)
    end sub

    sub Method2()
        dim t1 = new with { .a = 1, .b = 2 }
        dim t2 = new with { .a = 3, .b = 4 }
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestFixAllNestedTypes() As Task
            Dim text = "
class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = new with { .c = 1, .d = 2 } }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
        dim t1 = (a:=1, b:=(c:=1, d:=2))
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected, index:=1)
        End Function

        <Fact>
        Public Async Function ConvertMultipleNestedInstancesInSameMethod() As Task
            Dim text = "
class Test
    sub Method()
            dim t1 = [||]new with { .a = 1, .b = directcast(new with { .a = 1, .b = directcast(nothing, object) }, object) }
    end sub
end class
"
            Dim expected = "
class Test
    sub Method()
            dim t1 = (a:=1, b:=directcast((a:=1, b:=directcast(nothing, object)), object))
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestInLambda1() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim t1 = [||]new with { .a = 1, .b = 2 }
        dim a as Action =
            sub()
                dim t2 = new with { .a = 3, .b = 4 }
            end sub
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim a as Action =
            sub()
                dim t2 = (a:=3, b:=4)
            end sub
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestInLambda2() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim t1 = new with { .a = 1, .b = 2 }
        dim a as Action =
            sub()
                dim t2 = [||]new with { .a = 3, .b = 4 }
            end sub
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim t1 = (a:=1, b:=2)
        dim a as Action =
            sub()
                dim t2 = (a:=3, b:=4)
            end sub
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestIncomplete() As Task
            Dim text = "
Imports System

class Test
    sub Method()
        dim t1 = [||]new with { .a = , .b = }
    end sub
end class
"
            Dim expected = "
Imports System

class Test
    sub Method()
        dim t1 = (a:= , b:= )
    end sub
end class
"
            Await TestInRegularAndScriptAsync(text, expected)
        End Function
    End Class
End Namespace
