Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Public Class VisualBasicGoToNextAndPreviousMemberTests

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    Public Sub EmptyFile()
        Assert.Null(GetTargetPosition("$$", [next]:=True))
    End Sub

    Private Sub AssertNavigated(code As String, [next] As Boolean)
        Using workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.VisualBasic,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                VisualBasicParseOptions.Default,
                code)
            Dim hostDocument = workspace.DocumentWithCursor
            Dim targetPosition = GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    workspace.CurrentSolution.GetDocument(hostDocument.Id),
                    hostDocument.CursorPosition.Value,
                    [next],
                    CancellationToken.None)

            Assert.NotNull(targetPosition)
            Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value)
        End Using
    End Sub

    Private Function GetTargetPosition(code As String, [next] As Boolean) As Integer?
        Using workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.VisualBasic,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                VisualBasicParseOptions.Default,
                code)
            Dim hostDocument = workspace.DocumentWithCursor

            Return GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    workspace.CurrentSolution.GetDocument(hostDocument.Id),
                    hostDocument.CursorPosition.Value,
                    [next],
                    CancellationToken.None)
        End Using
    End Function

End Class
