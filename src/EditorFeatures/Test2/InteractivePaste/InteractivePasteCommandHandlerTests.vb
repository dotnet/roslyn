' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.VisualStudio.InteractiveWindow

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InteractivePaste
    Public Class InteractivePasteCommandhandlerTests
        Private Function CreateCommandHandler(workspace As TestWorkspace) As InteractivePasteCommandHandler
            Dim handler = New InteractivePasteCommandHandler(workspace.GetService(Of IEditorOperationsFactoryService),
                                                             workspace.GetService(Of ITextUndoHistoryRegistry))
            handler.RoslynClipBoard = New MockClipboard()
            Return handler
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithInteractiveFormat()
            Using workspace = TestWorkspaceFactory.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipBoard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Output, "a" & vbCr & vbLf & "bc"),
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Prompt, "< "),
                    New BufferBlock(ReplSpanKind.Input, "12"),
                    New BufferBlock(ReplSpanKind.StandardInput, "3")
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=True)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Assert.Equal("a" & vbCr & vbLf & "bc123", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithOutInteractiveFormat()
            Using workspace = TestWorkspaceFactory.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipBoard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Output, "a" & vbCr & vbLf & "bc"),
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Prompt, "< "),
                    New BufferBlock(ReplSpanKind.Input, "12"),
                    New BufferBlock(ReplSpanKind.StandardInput, "3")
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=False)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() editorOperations.InsertText("p"))

                Assert.Equal("p", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        Private Sub CopyToClipboard(clipboard As MockClipboard, blocks As BufferBlock(), includeRepl As Boolean)
            clipboard.Clear()
            Dim data = New DataObject()
            Dim builder = New StringBuilder()
            For Each block As BufferBlock In blocks
                builder.Append(block.Content)
            Next
            Dim text = builder.ToString()
            data.SetData(DataFormats.UnicodeText, text)
            data.SetData(DataFormats.StringFormat, text)
            If includeRepl Then
                data.SetData(InteractiveWindow.ClipboardFormat, BufferBlock.Serialize(blocks))
            End If
            clipboard.SetDataObject(data)
        End Sub

        Private Class MockClipboard
            Implements InteractivePasteCommandHandler.IRoslynClipboard

            Private _data As DataObject

            Friend Sub Clear()
                _data = Nothing
            End Sub

            Friend Sub SetDataObject(data As Object)
                _data = DirectCast(data, DataObject)
            End Sub

            Public Function ContainsData(format As String) As Boolean Implements InteractivePasteCommandHandler.IRoslynClipboard.ContainsData
                Return _data IsNot Nothing And _data.GetData(format) IsNot Nothing
            End Function

            Public Function GetData(format As String) As Object Implements InteractivePasteCommandHandler.IRoslynClipboard.GetData
                If _data Is Nothing Then
                    Return Nothing
                Else
                    Return _data.GetData(format)
                End If
            End Function
        End Class
    End Class
End Namespace

