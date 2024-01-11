' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InvertLogical

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertLogical
    <Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)>
    Partial Public Class InvertLogicalTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertLogicalCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function InvertLogical1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 [||]orelse b < 20
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 AndAlso b >= 20)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function InvertLogical2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 [||]andalso b >= 20)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 OrElse b < 20
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 [||]andalso
                  b >= 20)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 OrElse
                  b < 20
    end sub
end class")
        End Function

        <Fact>
        Public Async Function InvertMultiConditional1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = a > 10 [||]OrElse b < 20 OrElse c = 30
    end sub
end class",
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = Not (a <= 10 AndAlso b >= 20 AndAlso c <> 30)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function InvertMultiConditional2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = a > 10 OrElse b < 20 [||]OrElse c = 30
    end sub
end class",
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = Not (a <= 10 AndAlso b >= 20 AndAlso c <> 30)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function InvertMultiConditional3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = Not (a <= 10 [||]AndAlso b >= 20 AndAlso c <> 30)
    end sub
end class",
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = a > 10 OrElse b < 20 OrElse c = 30
    end sub
end class")
        End Function

        <Fact>
        Public Async Function InvertMultiConditional4() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = Not (a <= 10 AndAlso b >= 20 [||]AndAlso c <> 30)
    end sub
end class",
"class C
    sub M(a as integer, b as integer, c as integer)
        dim x = a > 10 OrElse b < 20 OrElse c = 30
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingOnSimpleOr() As Task
            Await TestMissingAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 [||]or b < 20
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingOnSimpleAnd() As Task
            Await TestMissingAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 [||]or b < 20
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestSelectedOperator() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = a > 10 [|orelse|] b < 20
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = Not (a <= 10 AndAlso b >= 20)
    end sub
end class")
        End Function
    End Class
End Namespace
