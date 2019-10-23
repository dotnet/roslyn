' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.PasteTracking
    Friend Class PasteTrackingTestState
        Implements IDisposable

        Private ReadOnly Property PasteTrackingService As PasteTrackingService
        Private ReadOnly Property PasteTrackingPasteCommandHandler As PasteTrackingPasteCommandHandler
        Private ReadOnly Property FormatCommandHandler As FormatCommandHandler

        Public ReadOnly Property Workspace As TestWorkspace

        Public Sub New(workspaceElement As XElement, Optional exportProvider As ExportProvider = Nothing)
            Workspace = TestWorkspace.CreateWorkspace(workspaceElement, exportProvider:=exportProvider)
            PasteTrackingService = GetExportedValue(Of PasteTrackingService)()
            PasteTrackingPasteCommandHandler = GetExportedValue(Of PasteTrackingPasteCommandHandler)()
            FormatCommandHandler = GetExportedValue(Of FormatCommandHandler)()
        End Sub

        Public Function GetService(Of T)() As T
            Return Workspace.GetService(Of T)()
        End Function

        Public Function GetExportedValue(Of T)() As T
            Return Workspace.ExportProvider.GetExportedValue(Of T)()
        End Function

        Public Function OpenDocument(projectName As String, fileName As String) As TestHostDocument
            Dim hostDocument = Workspace.Documents.FirstOrDefault(Function(document) document.Project.Name = projectName AndAlso document.Name = fileName)

            If Workspace.IsDocumentOpen(hostDocument.Id) Then
                hostDocument.GetTextView()
            Else
                OpenDocument(hostDocument)
            End If

            Return hostDocument
        End Function

        Public Sub OpenDocument(hostDocument As TestHostDocument)
            Workspace.OpenDocument(hostDocument.Id)
            hostDocument.GetTextView()
        End Sub

        Public Sub CloseDocument(hostDocument As TestHostDocument)
            hostDocument.CloseTextView()
            Workspace.CloseDocument(hostDocument.Id)
        End Sub

        Public Sub InsertText(hostDocument As TestHostDocument, insertedText As String)
            Dim textView = hostDocument.GetTextView()
            Dim editorOperations = GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

            editorOperations.InsertText(insertedText)
        End Sub

        Public Function SendPaste(hostDocument As TestHostDocument, pastedText As String) As TextSpan
            Dim textView = hostDocument.GetTextView()
            Dim caretPosition = textView.Caret.Position.BufferPosition.Position
            Dim trackingSpan = textView.TextSnapshot.CreateTrackingSpan(caretPosition, 0, SpanTrackingMode.EdgeInclusive)

            Dim editorOperations = GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)
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
