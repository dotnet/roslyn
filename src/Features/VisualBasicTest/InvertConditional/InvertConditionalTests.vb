' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InvertConditional

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertConditional
    <Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)>
    Partial Public Class InvertConditionalTests
        Inherits AbstractVisualBasicCodeActionTest_NoEditor

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As TestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertConditionalCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function InvertConditional1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = [||]if(x, a, b)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = if(Not x, b, a)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function InvertConditional2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = [||]if(not x, a, b)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = if(x, b, a)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestTrivia() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = [||]if(x,
            a,
            b)
    end sub
end class",
"class C
    sub M(x as boolean, a as integer, b as integer)
        dim c = if(Not x,
            b,
            a)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MissingOnBinaryIf() As Task
            Await TestMissingAsync(
"class C
    sub M(x as integer?, a as integer)
        dim c = [||]if(x, a)
    end sub
end class")
        End Function
    End Class
End Namespace
