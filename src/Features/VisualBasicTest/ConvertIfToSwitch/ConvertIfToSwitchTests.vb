' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.ConvertIfToSwitch
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertIfToSwitch)>
    Public Class ConvertIfToSwitchTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertIfToSwitchCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function TestMultipleCases() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [||]If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMultipleCaseLineContinuation() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [||]If i = 1 OrElse 2 = i OrElse i = 3 _
           Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 1, 2, 3 _
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestConstantExpression() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        Const A = 1, B = 2, C = 3
        [||]If i = A OrElse B = i OrElse i = C Then
            M(0)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Const A = 1, B = 2, C = 3
        Select i
            Case A, B, C
                M(0)
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnNonConstantExpression() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        Dim A = 1, B = 2, C = 3
        [||]If A = 1 OrElse 2 = B OrElse C = 3 Then
            M(0)
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnDifferentOperands() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer, j As Integer)
        [||]If i = 5 OrElse 6 = j Then
            M(0)
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingForSingleCase() As Task
            Await TestMissingAsync(
"Class C
    Sub M(i As Integer)
        [||]If i = 5 Then
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRange() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [||]If 5 >= i AndAlso 1 <= i Then
        Else If 7 >= i AndAlso 6 <= i
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 1 To 5
            Case 6 To 7
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestComparison() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [||]If i <= 5 OrElse i >= 1 Then
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case Is <= 5, Is >= 1
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestComplexIf() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [||]If i < 10 OrElse 20 < i OrElse (i >= 30 AndAlso 40 >= i) OrElse i = 50 Then
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case Is < 10, Is > 20, 30 To 40, 50
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSingleLineIf() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        [||]If i = 10 Then M(5) Else M(6)
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        Select i
            Case 10
                M(5)
            Case Else
                M(6)
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestSubsequentIfStatements_01() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(i As Integer) As Integer
        [||]If i = 10 Then Return 5
        If i = 20 Then Return 6
        Return 7
    End Function
End Class",
"Class C
    Function M(i As Integer) As Integer
        Select i
            Case 10
                Return 5
            Case 20
                Return 6
            Case Else
                Return 7
        End Select
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSubsequentIfStatements_02() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(i As Integer) As Integer
        [||]If i = 10 Then Return 5
        If i = 20 Then Return 6 Else Return 7
    End Function
End Class",
"Class C
    Function M(i As Integer) As Integer
        Select i
            Case 10
                Return 5
            Case 20
                Return 6
            Case Else
                Return 7
        End Select
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSubsequentIfStatements_03() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(i As Integer) As Integer
        [||]If i = 10 Then Return 5
        If i = 20 Then M(6)
        If i = 30 Then Return 6
        Return 7
    End Function
End Class",
"Class C
    Function M(i As Integer) As Integer
        Select i
            Case 10
                Return 5
            Case 20
                M(6)
        End Select
        If i = 30 Then Return 6
        Return 7
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSubsequentIfStatements_04() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(i As Integer) As Integer
        [||]If i = 10 Then Return 5
        If i = 20 Then Return 6
        If i = i Then Return 0
        Return 7
    End Function
End Class",
"Class C
    Function M(i As Integer) As Integer
        Select i
            Case 10
                Return 5
            Case 20
                Return 6
        End Select
        If i = i Then Return 0
        Return 7
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSubsequentIfStatements_05() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(i As Integer) As Integer
        [||]If i = 10 Then Return 5
        If i = 20 Then
            Return 6
        ElseIf i = 30 Then
            Return 7
        End If
        If i = i Then Return 0
        Return 8
    End Function
End Class",
"Class C
    Function M(i As Integer) As Integer
        Select i
            Case 10
                Return 5
            Case 20
                Return 6
            Case 30
                Return 7
        End Select
        If i = i Then Return 0
        Return 8
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSubsequentIfStatements_06() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Function M(i As Integer) As Integer
        [||]If i = 10 Then Return 5 Else Return 4
        If i = 20 Then
            Return 6
        ElseIf i = i Then
            Return 7
        End If
        If i = 5 Then Return 0
        Return 8
    End Function
End Class",
"Class C
    Function M(i As Integer) As Integer
        Select i
            Case 10
                Return 5
            Case Else
                Return 4
        End Select
        If i = 20 Then
            Return 6
        ElseIf i = i Then
            Return 7
        End If
        If i = 5 Then Return 0
        Return 8
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestExitWhile() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
        While i = i
            [||]If i = 10 Then
                Exit While
            Else If i = 1 Then
            End If
        End While
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
        While i = i
            Select i
                Case 10
                    Exit While
                Case 1
            End Select
        End While
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21103")>
        Public Async Function TestTrivia1() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M(i As Integer)
#if true
        Console.WriteLine()
#end if

        [||]If i = 1 OrElse 2 = i OrElse i = 3 Then
            M(0)
        ElseIf i = 4 OrElse 5 = i OrElse i = 6 Then
            M(1)
        Else
            M(2)
        End If
    End Sub
End Class",
"Class C
    Sub M(i As Integer)
#if true
        Console.WriteLine()
#end if

        Select i
            Case 1, 2, 3
                M(0)
            Case 4, 5, 6
                M(1)
            Case Else
                M(2)
        End Select
    End Sub
End Class")
        End Function
    End Class
End Namespace
