' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedParametersAndValues
    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)>
    Partial Public Class RemoveUnusedValueExpressionStatementTests
        Inherits RemoveUnusedValuesTestsBase

        Private Protected Overrides ReadOnly Property PreferNone As OptionsCollection
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.None))
            End Get
        End Property

        Private Protected Overrides ReadOnly Property PreferDiscard As OptionsCollection
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.DiscardVariable, NotificationOption2.Silent))
            End Get
        End Property

        Private Protected Overrides ReadOnly Property PreferUnusedLocal As OptionsCollection
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Silent))
            End Get
        End Property

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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
