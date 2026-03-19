' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.PasteTracking
    Friend Class PasteTrackingTestState
        Implements IDisposable

        Private ReadOnly Property PasteTrackingService As PasteTrackingService
        Private ReadOnly Property PasteTrackingPasteCommandHandler As PasteTrackingPasteCommandHandler
        Private ReadOnly Property FormatCommandHandler As FormatCommandHandler

        Public ReadOnly Property Workspace As EditorTestWorkspace

        Public Sub New(workspaceElement As XElement, Optional composition As TestComposition = Nothing)
            Workspace = EditorTestWorkspace.CreateWorkspace(workspaceElement, composition:=composition)
            PasteTrackingService = Workspace.GetService(Of PasteTrackingService)()
            PasteTrackingPasteCommandHandler = Workspace.GetService(Of PasteTrackingPasteCommandHandler)()
            FormatCommandHandler = Workspace.GetService(Of FormatCommandHandler)()
        End Sub

        Public Function OpenDocument(projectName As String, fileName As String) As EditorTestHostDocument
            Dim hostDocument = Workspace.Documents.FirstOrDefault(Function(document) document.Project.Name = projectName AndAlso document.Name = fileName)

            If Workspace.IsDocumentOpen(hostDocument.Id) Then
                hostDocument.GetTextView()
            Else
                OpenDocument(hostDocument)
            End If

            Return hostDocument
        End Function

        Public Sub OpenDocument(hostDocument As EditorTestHostDocument)
            Workspace.OpenDocument(hostDocument.Id)
            hostDocument.GetTextView()
        End Sub

        Public Sub CloseDocument(hostDocument As EditorTestHostDocument)
            hostDocument.CloseTextView()
            Workspace.CloseDocument(hostDocument.Id)
        End Sub

        Public Sub InsertText(hostDocument As EditorTestHostDocument, insertedText As String)
            Dim textView = hostDocument.GetTextView()
            Dim editorOperations = Workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

            editorOperations.InsertText(insertedText)
        End Sub

        Public Function SendPaste(hostDocument As EditorTestHostDocument, pastedText As String) As TextSpan
            Dim textView = hostDocument.GetTextView()
            Dim caretPosition = textView.Caret.Position.BufferPosition.Position
            Dim trackingSpan = textView.TextSnapshot.CreateTrackingSpan(caretPosition, 0, SpanTrackingMode.EdgeInclusive)

            Dim editorOperations = Workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)
            Dim insertAction As Action = Sub() editorOperations.InsertText(pastedText)

            Dim pasteCommandArgs = New PasteCommandArgs(textView, textView.TextBuffer)
            Dim executionContext = TestCommandExecutionContext.Create()

            ' Insert the formatting command hander to test format on paste scenarios
            Dim formattingHandler As Action = Sub() FormatCommandHandler.ExecuteCommand(pasteCommandArgs, insertAction, executionContext)
            PasteTrackingPasteCommandHandler.ExecuteCommand(pasteCommandArgs, formattingHandler, executionContext)

            Dim snapshotSpan = trackingSpan.GetSpan(textView.TextBuffer.CurrentSnapshot)
            Return New TextSpan(snapshotSpan.Start, snapshotSpan.Length)
        End Function

        ''' <summary>
        ''' Optionally pass in a TextSpan to assert it is equal to the pasted text span 
        ''' </summary>
        Public Async Function AssertHasPastedTextSpanAsync(hostDocument As TestHostDocument, Optional textSpan As TextSpan = Nothing) As Task
            Dim document = Workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Dim sourceText = Await document.GetTextAsync()

            Dim pastedTextSpan As TextSpan
            Assert.True(PasteTrackingService.TryGetPastedTextSpan(sourceText.Container, pastedTextSpan))

            If (textSpan.IsEmpty) Then
                Return
            End If

            Assert.Equal(textSpan, pastedTextSpan)
        End Function

        Public Sub AssertMissingPastedTextSpan(textBuffer As ITextBuffer)
            Dim textSpan As TextSpan
            Assert.False(PasteTrackingService.TryGetPastedTextSpan(textBuffer.AsTextContainer(), textSpan))
        End Sub

        Private Sub Dispose() Implements IDisposable.Dispose
            Workspace.Dispose()
        End Sub
    End Class
End Namespace
