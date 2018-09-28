' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedExpressionsAndParameters
    Partial Public Class RemoveUnusedExpressionsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Private ReadOnly Property PreferNone As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.UnusedExpressionAssignment,
                                New CodeStyleOption(Of UnusedExpressionAssignmentPreference)(UnusedExpressionAssignmentPreference.None, NotificationOption.Suggestion))
            End Get
        End Property

        Private ReadOnly Property PreferDiscard As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.UnusedExpressionAssignment,
                                New CodeStyleOption(Of UnusedExpressionAssignmentPreference)(UnusedExpressionAssignmentPreference.DiscardVariable, NotificationOption.Suggestion))
            End Get
        End Property

        Private ReadOnly Property PreferUnusedLocal As IDictionary(Of OptionKey, Object)
            Get
                Return [Option](CodeStyleOptions.UnusedExpressionAssignment,
                                New CodeStyleOption(Of UnusedExpressionAssignmentPreference)(UnusedExpressionAssignmentPreference.UnusedLocalVariable, NotificationOption.Suggestion))
            End Get
        End Property

        Private Overloads Function TestMissingInRegularAndScriptAsync(initialMarkup As String, options As IDictionary(Of OptionKey, Object)) As Task
            Return TestMissingInRegularAndScriptAsync(initialMarkup, New TestParameters(options:=options))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function TestPreferNone() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] = 1
        x = 2
        Return x
    End Function
End Class", options:=PreferNone)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function TestPreferDiscard() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] = 1
        x = 2
        Return x
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim x As Integer = 2
        Return x
    End Function
End Class", options:=PreferDiscard)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function TestPreferUnusedLocal() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] = 1
        x = 2
        Return x
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim x As Integer = 2
        Return x
    End Function
End Class", options:=PreferUnusedLocal)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Initialization_ConstantValue() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] As Integer = 1
        x = 2
        Return x
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim x As Integer = 2
        Return x
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Initialization_ConstantValue_UnusedLocal() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] As Integer = 1
        x = 2
        Return 0
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim x As Integer = 2
        Return 0
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Assignment_ConstantValue() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim x As Integer
        [|x|] = 1
        x = 2
        Return x
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim x As Integer
        x = 2
        Return x
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Initialization_NonConstantValue() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] = M2()
        x = 2
        Return x
    End Function

    Private Function M2() As Integer
        Return 0
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim unused = M2()
        Dim x As Integer = 2
        Return x
    End Function

    Private Function M2() As Integer
        Return 0
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Initialization_NonConstantValue_UnusedLocal() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim [|x|] As Integer = M2()
        x = 2
        Return 0
    End Function

    Private Function M2() As Integer
        Return 0
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Assignment_NonConstantValue() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim x As Integer
        [|x|] = M2()
        x = 2
        Return x
    End Function

    Private Function M2() As Integer
        Return 0
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        Dim x As Integer
        Dim unused As Integer = M2()
        x = 2
        Return x
    End Function

    Private Function M2() As Integer
        Return 0
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function Assignment_NonConstantValue_UnusedLocal() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        Dim x As Integer
        [|x|] = M2()
        x = 2
        Return 0
    End Function

    Private Function M2() As Integer
        Return 0
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function UseInLambda() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Imports System

Class C
    Private Sub M(p As Object)
        Dim lambda As Action = Sub()
                                   Dim x = p
                               End Sub

        [|p|] = Nothing
        lambda()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function CatchClause_ExceptionVariable_01() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Imports System

Class C
    Private Sub M(p As Object)
        Try
        Catch [|ex|] As Exception
        End Try
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function CatchClause_ExceptionVariable_02() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Imports System

Class C
    Public ReadOnly Property P As Boolean
        Get
            Try
                Return True
            Catch [|ex|] As Exception
                Return False
            End Try
            Return 0
        End Get
    End Property
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function CatchClause_ExceptionVariable_03() As Task
            Await TestInRegularAndScriptAsync(
$"Imports System

Class C
    Private Sub M(p As Object)
        Try
        Catch [|ex|] As Exception
            ex = Nothing
            Dim x = ex
        End Try
    End Sub
End Class",
$"Imports System

Class C
    Private Sub M(p As Object)
        Try
        Catch unused As Exception
            Dim ex As Exception = Nothing

            Dim x = ex
        End Try
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function ForToLoopStatement_01() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Imports System

Class C
    Private Sub M()
        For [|i|] As Integer = 0 To 10
            Dim x = i
        Next
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function ForToLoopStatement_02() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Imports System

Class C
    Private Sub M()
        For [|i|] As Integer = 0 To 10
            i = 1
        Next
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedExpressions)>
        Public Async Function ForToLoopStatement_03() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Imports System

Class C
    Private Sub M()
        For i As Integer = 0 To 10
            [|i|] = 1
        Next
    End Sub
End Class")
        End Function
    End Class
End Namespace
