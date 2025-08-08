' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedParametersAndValues
    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedValues)>
    Partial Public Class RemoveUnusedValueAssignmentTests
        Inherits RemoveUnusedValuesTestsBase

        Private Protected Overrides ReadOnly Property PreferNone As OptionsCollection
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueAssignment,
                                New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.None))
            End Get
        End Property

        Private Protected Overrides ReadOnly Property PreferDiscard As OptionsCollection
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueAssignment,
                                New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.DiscardVariable, NotificationOption2.Suggestion))
            End Get
        End Property

        Private Protected Overrides ReadOnly Property PreferUnusedLocal As OptionsCollection
            Get
                Return [Option](VisualBasicCodeStyleOptions.UnusedValueAssignment,
                                New CodeStyleOption2(Of UnusedValuePreference)(UnusedValuePreference.UnusedLocalVariable, NotificationOption2.Suggestion))
            End Get
        End Property

        <Fact>
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

        <Fact>
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
End Class", New TestParameters(options:=PreferDiscard))
        End Function

        <Fact>
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
End Class", New TestParameters(options:=PreferUnusedLocal))
        End Function

        <Fact>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48070")>
        Public Async Function Initialization_ConstantValue_DoNotCopyLeadingTriviaDirectives() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M()
#region """"

        dim value as integer = 3

#end region
        dim [|x|] as integer? = nothing
        dim y = value + value

        x = y
        System.Console.WriteLine(x)
    end sub
end class",
"class C
    sub M()
#region """"

        dim value as integer = 3

#end region
        dim y = value + value

        Dim x As Integer? = y
        System.Console.WriteLine(x)
    end sub
end class")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function StaticLocals() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Function Increment() As Boolean
        Static count As Integer = 0
        If count > 10 Then
            Return True
        End If

        [|count|] = count + 1
        Return False
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function UsedAssignment_ConditionalPreprocessorDirective() As Task
            Await TestMissingInRegularAndScriptAsync(
$"Class C
    Function M() As Integer
        Dim [|p|] = 0
#If DEBUG Then
        p = 1
#End If
        Return p
    End Function
End Class")
        End Function

        <Fact, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")>
        Public Async Function RedundantAssignment_WithLeadingAndTrailingComment() As Task
            Await TestInRegularAndScriptAsync(
$"Class C
    Private Function M() As Integer
        'Preceding comment.'
        Dim [|x|] As Integer = 0 'Trailing comment'

        If True Then
            x = 2
        End If
        Return x
    End Function
End Class",
$"Class C
    Private Function M() As Integer
        'Preceding comment.'
        Dim x As Integer

        If True Then
            x = 2
        End If
        Return x
    End Function
End Class")
        End Function

        <Fact, WorkItem(32856, "https://github.com/dotnet/roslyn/issues/33312")>
        Public Async Function MultipleRedundantAssignment_WithLeadingAndTrailingComment() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Private Function M() As Integer
        'Preceding comment.'
        {|FixAllInDocument:Dim x, y As Integer = 0|} 'Trailing comment'

        If True Then
            x = 2
            y = 2
        End If
        Return x + y
    End Function
End Class",
$"Class C
    Private Function M() As Integer

        'Preceding comment.'
        Dim x As Integer
        Dim y As Integer
        If True Then
            x = 2
            y = 2
        End If
        Return x + y
    End Function
End Class")
        End Function
    End Class
End Namespace
