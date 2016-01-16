' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CommentSelection
    Public Class VisualBasicCommentSelectionTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)>
        Public Async Function Comment1() As Threading.Tasks.Task
            Dim code = <code>Module Program
    [|Sub Main(args As String())
        'already commented

    End Sub|]
End Module</code>

            Dim expected = <code>Module Program
    'Sub Main(args As String())
    '    'already commented

    'End Sub
End Module</code>

            Await InvokeCommentOperationOnSelectionAfterReplacingLfToCrLfAsync(code.Value, expected.Value, CommentUncommentSelectionCommandHandler.Operation.Comment)
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)>
        Public Async Function UncommentAndFormat1() As Threading.Tasks.Task
            Dim code = <code>Module Program
    [|            '       Sub         Main        (       args    As String           ())
        '
                        '           End Sub |]
End Module</code>

            Dim expected = <code>Module Program
    Sub Main(args As String())

    End Sub
End Module</code>

            Await InvokeCommentOperationOnSelectionAfterReplacingLfToCrLfAsync(code.Value, expected.Value, CommentUncommentSelectionCommandHandler.Operation.Uncomment)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CommentSelection)>
        Public Async Function UncommentAndFormat2() As Threading.Tasks.Task
            Dim code = <code>Module Program
    [|            '       Sub         Main        (       args    As String           ())           |]
    [|        '                                                                                     |]
    [|                    '           End Sub                                                       |]
End Module</code>

            Dim expected = <code>Module Program
    Sub Main(args As String())

    End Sub
End Module</code>

            Await InvokeCommentOperationOnSelectionAfterReplacingLfToCrLfAsync(code.Value, expected.Value, CommentUncommentSelectionCommandHandler.Operation.Uncomment)
        End Function

        Private Shared Async Function InvokeCommentOperationOnSelectionAfterReplacingLfToCrLfAsync(code As String, expected As String, operation As CommentUncommentSelectionCommandHandler.Operation) As Threading.Tasks.Task
            ' do this since xml value put only vbLf
            code = code.Replace(vbLf, vbCrLf)
            expected = expected.Replace(vbLf, vbCrLf)

            Dim codeWithoutMarkup As String = Nothing
            Dim spans As IList(Of TextSpan) = Nothing

            MarkupTestFile.GetSpans(code, codeWithoutMarkup, spans)

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(codeWithoutMarkup)
                Dim doc = workspace.Documents.First()
                SetupSelection(doc.GetTextView(), spans.Select(Function(s) Span.FromBounds(s.Start, s.End)))

                Dim commandHandler = New CommentUncommentSelectionCommandHandler(
                    TestWaitIndicator.Default,
                    workspace.ExportProvider.GetExportedValue(Of ITextUndoHistoryRegistry),
                    workspace.ExportProvider.GetExportedValue(Of IEditorOperationsFactoryService))
                Dim textView = doc.GetTextView()
                Dim textBuffer = doc.GetTextBuffer()
                commandHandler.ExecuteCommand(textView, textBuffer, operation)

                Assert.Equal(expected, doc.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        Private Shared Sub SetupSelection(textView As IWpfTextView, spans As IEnumerable(Of Span))
            Dim snapshot = textView.TextSnapshot
            If spans.Count() = 1 Then
                textView.Selection.Select(New SnapshotSpan(snapshot, spans.Single()), isReversed:=False)
                textView.Caret.MoveTo(New SnapshotPoint(snapshot, spans.Single().End))
            Else
                textView.Selection.Mode = TextSelectionMode.Box
                textView.Selection.Select(New VirtualSnapshotPoint(snapshot, spans.First().Start), New VirtualSnapshotPoint(snapshot, spans.Last().End))
                textView.Caret.MoveTo(New SnapshotPoint(snapshot, spans.Last().End))
            End If
        End Sub
    End Class
End Namespace
