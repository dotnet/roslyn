' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.PasteTracking
    Friend Class PasteTrackingTestState
        Implements IDisposable

        Private ReadOnly Property PasteTrackingService As PasteTrackingService
        Private ReadOnly Property PasteCommandHandler As PasteTrackingPasteCommandHandler

        Public ReadOnly Property Workspace As TestWorkspace

        Public Sub New(workspaceElement As XElement, Optional exportProvider As ExportProvider = Nothing)
            Workspace = TestWorkspace.CreateWorkspace(workspaceElement, exportProvider:=exportProvider)
            PasteTrackingService = GetExportedValue(Of PasteTrackingService)()
            PasteCommandHandler = GetExportedValue(Of PasteTrackingPasteCommandHandler)()
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

            ' When all documents sharing the same TextBuffer are closed
            ' the TextBuffer Properties should be cleared.
            Dim textBufferClosed = Workspace.GetOpenDocumentIds().
                All(Function(id) Workspace.GetTestDocument(id)?.TextBuffer Is hostDocument.TextBuffer)
            If textBufferClosed Then
                ClearTextBufferProperties(hostDocument)
            End If
        End Sub

        Private Sub ClearTextBufferProperties(testDocument As TestHostDocument)
            Dim propertyKeys = testDocument.TextBuffer.Properties.PropertyList.Select(Function(kvp) kvp.Key)
            For Each key In propertyKeys
                testDocument.TextBuffer.Properties.RemoveProperty(key)
            Next
        End Sub

        Public Sub InsertText(hostDocument As TestHostDocument, insertedText As String)
            Dim textView = hostDocument.GetTextView()
            Dim editorOperations = GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

            editorOperations.InsertText(insertedText)
        End Sub

        Public Function SendPaste(hostDocument As TestHostDocument, pastedText As String) As TextSpan
            Dim textView = hostDocument.GetTextView()
            Dim caretPosition = textView.Caret.Position.BufferPosition.Position
            Dim editorOperations = GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

            SendPaste(textView, AddressOf PasteCommandHandler.ExecuteCommand, Sub() editorOperations.InsertText(pastedText))

            Return New TextSpan(caretPosition, pastedText.Length)
        End Function

        Private Sub SendPaste(textView As ITextView, commandHandler As Action(Of PasteCommandArgs, Action, CommandExecutionContext), nextHandler As Action)
            commandHandler(New PasteCommandArgs(textView, textView.TextBuffer), nextHandler, TestCommandExecutionContext.Create())
        End Sub

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

        Public Async Function AssertMissingPastedTextSpanAsync(hostDocument As TestHostDocument) As Task
            Dim document = Workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Dim sourceText = Await document.GetTextAsync()

            Dim textSpan As TextSpan
            Assert.False(PasteTrackingService.TryGetPastedTextSpan(sourceText.Container, textSpan))
        End Function

        Private Sub Dispose() Implements IDisposable.Dispose
            Workspace.Dispose()
        End Sub
    End Class
End Namespace
