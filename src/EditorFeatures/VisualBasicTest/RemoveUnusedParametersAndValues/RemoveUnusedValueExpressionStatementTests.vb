' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedParametersAndValues
    Partial Public Class RemoveUnusedValueExpressionStatementTests
        Inherits RemoveUnusedValuesTestsBase

        Protected Overrides ReadOnly Property PreferNone As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                New CodeStyleOption(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption.None))
            End Get
        End Property

        Protected Overrides ReadOnly Property PreferDiscard As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                New CodeStyleOption(Of UnusedValuePreference)(UnusedValuePreference.DiscardVariable, NotificationOption.Silent))
            End Get
        End Property

        Protected Overrides ReadOnly Property PreferUnusedLocal As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                New CodeStyleOption(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption.Silent))
            End Get
        End Property

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)>
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
