' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedExpressionsAndParameters

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedExpressionsAndParameters
    Partial Public Class RemoveUnusedExpressionsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRemoveUnusedExpressionsAndParametersDiagnosticAnalyzer(), New VisualBasicRemoveUnusedExpressionsAndParametersCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function ExpressionStatement_PreferNone() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Private Sub M()
        [|M2()|]
    End Sub

    Private Function M2() As Integer
        Return 0
    End Function
End Class", options:=PreferNone)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function ExpressionStatement_PreferDiscard() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Sub M()
        [|M2()|]
    End Sub

    Private Function M2() As Integer
        Return 0
    End Function
End Class",
$"Class C
    Private Sub M()
        Dim unused = M2()
    End Sub

    Private Function M2() As Integer
        Return 0
    End Function
End Class", options:=PreferDiscard)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function ExpressionStatement_PreferUnusedLocal() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Sub M()
        [|M2()|]
    End Sub

    Private Function M2() As Integer
        Return 0
    End Function
End Class",
$"Class C
    Private Sub M()
        Dim unused = M2()
    End Sub

    Private Function M2() As Integer
        Return 0
    End Function
End Class", options:=PreferUnusedLocal)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function CallStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Private Sub M()
        Call [|M2()|]
    End Sub

    Private Function M2() As Integer
        Return 0
    End Function
End Class")
        End Function
    End Class
End Namespace
