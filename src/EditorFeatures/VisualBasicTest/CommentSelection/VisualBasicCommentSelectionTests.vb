' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CommentSelection
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CommentSelection
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.CommentSelection)>
    Public Class VisualBasicCommentSelectionTests
        <WpfFact>
        Public Sub Comment1()
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

            InvokeCommentOperationOnSelectionAfterReplacingLfToCrLf(code.Value, expected.Value, Operation.Comment)
        End Sub

        <WpfFact>
        Public Sub UncommentAndFormat1()
            Dim code = <code>Module Program
    [|            '       Sub         Main        (       args    As String           ())
        '
                        '           End Sub |]
End Module</code>

            Dim expected = <code>Module Program
    Sub Main(args As String())

    End Sub
End Module</code>

            InvokeCommentOperationOnSelectionAfterReplacingLfToCrLf(code.Value, expected.Value, Operation.Uncomment)
        End Sub

        <WpfFact>
        Public Sub UncommentAndFormat2()
            Dim code = <code>Module Program
    [|            '       Sub         Main        (       args    As String           ())           |]
    [|        '                                                                                     |]
    [|                    '           End Sub                                                       |]
End Module</code>

            Dim expected = <code>Module Program
    Sub Main(args As String())

    End Sub
End Module</code>

            InvokeCommentOperationOnSelectionAfterReplacingLfToCrLf(code.Value, expected.Value, Operation.Uncomment)
        End Sub

        Private Shared Sub InvokeCommentOperationOnSelectionAfterReplacingLfToCrLf(code As String, expected As String, operation As Operation)
            ' do this since xml value put only vbLf
            code = code.Replace(vbLf, vbCrLf)
            expected = expected.Replace(vbLf, vbCrLf)

            Dim codeWithoutMarkup As String = Nothing
            Dim spans As ImmutableArray(Of TextSpan) = Nothing

            MarkupTestFile.GetSpans(code, codeWithoutMarkup, spans)

            Using workspace = EditorTestWorkspace.CreateVisualBasic(codeWithoutMarkup)
                Dim doc = workspace.Documents.First()
                SetupSelection(doc.GetTextView(), spans.Select(Function(s) Span.FromBounds(s.Start, s.End)))

                Dim commandHandler = New CommentUncommentSelectionCommandHandler(
                    workspace.GetService(Of ITextUndoHistoryRegistry),
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of EditorOptionsService))
                Dim textView = doc.GetTextView()
                Dim textBuffer = doc.GetTextBuffer()
                commandHandler.ExecuteCommand(textView, textBuffer, operation, TestCommandExecutionContext.Create())

                Assert.Equal(expected, doc.GetTextBuffer().CurrentSnapshot.GetText())
            End Using
        End Sub

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
