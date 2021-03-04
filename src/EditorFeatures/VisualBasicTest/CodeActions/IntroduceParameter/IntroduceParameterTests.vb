' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.IntroduceParameter
    Public Class IntroduceParameterTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicIntroduceParameterService()
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return GetNestedActions(actions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionWithNoMethodCallsCase() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, v As Integer)
        Dim num As Integer = {|Rename:v|}
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionCaseWithSingleMethodCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, v As Integer)
        Dim num As Integer = {|Rename:v|}
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, z * y* x)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionCaseWithLocal() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim l As Integer = 5
        Dim num As Integer = [|l * x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestHighlightIncompleteExpressionCaseWithSingleMethodCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = 5 * [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Await TestMissingInRegularAndScriptAsync(source)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionCaseWithMultipleMethodCall() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
        M(a + b, 5, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, v As Integer)
        Dim num As Integer = {|Rename:v|}
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, z * y* x)
        M(a + b, 5, x, (a + b) * 5* x)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionAllOccurrences() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num2 As Integer = x * y * z
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer, v As Integer)
        Dim num2 As Integer = {|Rename:v|}
        Dim num As Integer = {|Rename:v|}
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, z * y* x)
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionWithNoMethodCallsTrampoline() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function M_v(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, v As Integer)
        Dim num As Integer = {|Rename:v|}
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)>
        Public Async Function TestSimpleExpressionWithSingleMethodCallTrampoline() As Task
            Dim source =
"Class Program
    Sub M(x As Integer, y As Integer, z As Integer)
        Dim num As Integer = [|x * y * z|]
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x)
    End Sub
End Class"
            Dim expected =
"Class Program
    Public Function M_v(x As Integer, y As Integer, z As Integer) As Integer
        Return x * y * z
    End Function

    Sub M(x As Integer, y As Integer, z As Integer, v As Integer)
        Dim num As Integer = {|Rename:v|}
    End Sub

    Sub M1(x As Integer, y As Integer, z As Integer)
        M(z, y, x, M_v(z, y, x))
    End Sub
End Class"
            Await TestInRegularAndScriptAsync(source, expected, index:=1)
        End Function
    End Class
End Namespace

