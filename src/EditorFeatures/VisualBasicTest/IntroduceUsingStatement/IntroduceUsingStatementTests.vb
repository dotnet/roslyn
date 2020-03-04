﻿Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.IntroduceUsingStatement
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.IntroduceUsingStatement

    <Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceUsingStatement)>
    Public NotInheritable Class IntroduceUsingStatementTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(ByVal workspace As Workspace, ByVal parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicIntroduceUsingStatementCodeRefactoringProvider
        End Function

        <Theory>
        <InlineData("D[||]im name = disposable")>
        <InlineData("Dim[||] name = disposable")>
        <InlineData("Dim [||]name = disposable")>
        <InlineData("Dim na[||]me = disposable")>
        <InlineData("Dim name[||] = disposable")>
        <InlineData("Dim name [||]= disposable")>
        <InlineData("Dim name =[||] disposable")>
        <InlineData("Dim name = [||]disposable")>
        <InlineData("[|Dim name = disposable|]")>
        <InlineData("Dim name = disposable[||]")>
        <InlineData("Dim name = disposable[||]")>
        Public Async Function RefactoringIsAvailableForSelection(ByVal declaration As String) As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        " & declaration & "
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using name = disposable
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsAvailableForVerticalSelection() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)    [|    " & "
        Dim name = disposable                  |]
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using name = disposable
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsAvailableForSelectionAtStartOfStatementWithPrecedingDeclaration() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim ignore = disposable
        [||]Dim name = disposable
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Dim ignore = disposable
        Using name = disposable
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsAvailableForSelectionAtStartOfLineWithPrecedingDeclaration() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim ignore = disposable
[||]        Dim name = disposable
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Dim ignore = disposable
        Using name = disposable
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsAvailableForSelectionAtEndOfStatementWithFollowingDeclaration() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim name = disposable[||]
        Dim ignore = disposable
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using name = disposable
        End Using
        Dim ignore = disposable
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsAvailableForSelectionAtEndOfLineWithFollowingDeclaration() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim name = disposable    [||]
        Dim ignore = disposable
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using name = disposable
        End Using
        Dim ignore = disposable
    End Sub
End Class")
        End Function

        <Theory>
        <InlineData("Dim name = d[||]isposable")>
        <InlineData("Dim name = disposabl[||]e")>
        <InlineData("Dim name=[|disposable|]")>
        Public Async Function RefactoringIsNotAvailableForSelection(ByVal declaration As String) As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        " & declaration & "
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsNotAvailableForDeclarationMissingInitializerExpression() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim name As System.IDisposable =[||]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsNotAvailableForUsingStatementDeclaration() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Using [||]name = disposable
        End Using
    End Sub
End Class")
        End Function

        <Theory>
        <InlineData("[||]Dim x = disposable, y = disposable")>
        <InlineData("Dim [||]x = disposable, y = disposable")>
        <InlineData("Dim x = disposable, [||]y = disposable")>
        <InlineData("Dim x = disposable, y = disposable[||]")>
        Public Async Function RefactoringIsNotAvailableForMultiVariableDeclaration(ByVal declaration As String) As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        " & declaration & "
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsAvailableForConstrainedGenericTypeParameter() As Task
            Await TestInRegularAndScriptAsync("Class C(Of T As System.IDisposable)
    Sub M(disposable As T)
        Dim x = disposable[||]
    End Sub
End Class", "Class C(Of T As System.IDisposable)
    Sub M(disposable As T)
        Using x = disposable
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsNotAvailableForUnconstrainedGenericTypeParameter() As Task
            Await TestMissingAsync("Class C(Of T)
    Sub M(disposable as T)
        Dim x = disposable[||]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function LeadingCommentTriviaIsPlacedOnUsingStatement() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        ' Comment
        Dim x = disposable[||]
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        ' Comment
        Using x = disposable
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function CommentOnTheSameLineStaysOnTheSameLine() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim x = disposable[||] ' Comment
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using x = disposable ' Comment
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TrailingCommentTriviaOnNextLineGoesAfterBlock() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim x = disposable[||]
        ' Comment
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using x = disposable
        End Using
        ' Comment
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function ValidPreprocessorStaysValid() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
#If True Then
        Dim x = disposable[||]
#End If
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
#If True Then
        Using x = disposable
        End Using
#End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function InvalidPreprocessorStaysInvalid() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
#If True Then
        Dim x = disposable[||]
#End If
        Dim discard = x
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
#If True Then
        Using x = disposable
#End If
            Dim discard = x
        End Using
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function StatementsAreSurroundedByMinimalScope() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        M(null)
        Dim x = disposable[||]
        M(null)
        M(x)
        M(null)
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        M(null)
        Using x = disposable
            M(null)
            M(x)
        End Using
        M(null)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function CommentsAreSurroundedExceptLinesFollowingLastUsage() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Dim x = disposable[||]
        ' A
        M(x) ' B
        ' C
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Using x = disposable
            ' A
            M(x) ' B
        End Using
        ' C
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function WorksInSelectCases() As Task
            Await TestInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        Select Case disposable
            Case Else
                Dim x = disposable[||]
                M(x)
        End Select
    End Sub
End Class", "Class C
    Sub M(disposable As System.IDisposable)
        Select Case disposable
            Case Else
                Using x = disposable
                    M(x)
                End Using
        End Select
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsNotAvailableOnSingleLineIfStatements() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        If disposable IsNot Nothing Then Dim x = disposable[||]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsNotAvailableOnSingleLineIfElseClauses() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        If disposable IsNot Nothing Then Else Dim x = disposable[||]
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function RefactoringIsNotAvailableOnSingleLineLambda() As Task
            Await TestMissingInRegularAndScriptAsync("Class C
    Sub M(disposable As System.IDisposable)
        New Action(Function() Dim x = disposable[||])
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem(35237, "https://github.com/dotnet/roslyn/issues/35237")>
        Public Async Function ExpandsToIncludeSurroundedVariableDeclarations() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.IO

Class C
    Sub M()
        Dim reader = New MemoryStream()[||]
        Dim buffer = reader.GetBuffer()
        buffer.Clone()
        Dim a = 1
    End Sub
End Class",
"Imports System.IO

Class C
    Sub M()
        Using reader = New MemoryStream()
            Dim buffer = reader.GetBuffer()
            buffer.Clone()
        End Using
        Dim a = 1
    End Sub
End Class")
        End Function

        <Fact>
        <WorkItem(35237, "https://github.com/dotnet/roslyn/issues/35237")>
        Public Async Function ExpandsToIncludeSurroundedMultiVariableDeclarations() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.IO

Class C
    Sub M()
        Dim reader = New MemoryStream()[||]
        Dim buffer = reader.GetBuffer()
        Dim a As Integer = buffer(0), b As Integer = a
        Dim c = b
        Dim d = 1
    End Sub
End Class",
"Imports System.IO

Class C
    Sub M()
        Using reader = New MemoryStream()
            Dim buffer = reader.GetBuffer()
            Dim a As Integer = buffer(0), b As Integer = a
            Dim c = b
        End Using
        Dim d = 1
    End Sub
End Class")
        End Function
    End Class
End Namespace
