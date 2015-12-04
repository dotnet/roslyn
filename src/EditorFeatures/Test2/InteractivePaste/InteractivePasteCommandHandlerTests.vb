' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Windows
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

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
        Public Async Function PasteCommandWithInteractiveFormat() As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Output, "a" & vbCrLf & "bc"),
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Prompt, "< "),
                    New BufferBlock(ReplSpanKind.Input, "12"),
                    New BufferBlock(ReplSpanKind.StandardInput, "3")
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=True, isLineCopy:=False, isBoxCopy:=False)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Assert.Equal("a" & vbCrLf & "bc123", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function PasteCommandWithOutInteractiveFormat() As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Output, "a" & vbCrLf & "bc"),
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Prompt, "< "),
                    New BufferBlock(ReplSpanKind.Input, "12"),
                    New BufferBlock(ReplSpanKind.StandardInput, "3")
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=False, isLineCopy:=False, isBoxCopy:=False)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() editorOperations.InsertText("p"))

                Assert.Equal("p", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function PasteCommandWithInteractiveFormatAsLineCopy() As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                editorOperations.InsertText("line1")
                editorOperations.InsertNewLine()
                editorOperations.InsertText("line2")

                Assert.Equal("line1" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Input, "InsertedLine"),
                    New BufferBlock(ReplSpanKind.Output, vbCrLf)
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=True, isLineCopy:=True, isBoxCopy:=False)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Assert.Equal("line1" & vbCrLf & "InsertedLine" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function PasteCommandWithInteractiveFormatAsBoxCopy() As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                editorOperations.InsertText("line1")
                editorOperations.InsertNewLine()
                editorOperations.InsertText("line2")
                editorOperations.MoveLineUp(False)
                editorOperations.MoveToPreviousCharacter(False)

                Assert.Equal("line1" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Input, "BoxLine1"),
                    New BufferBlock(ReplSpanKind.LineBreak, vbCrLf),
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Input, "BoxLine2"),
                    New BufferBlock(ReplSpanKind.LineBreak, vbCrLf)
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=True, isLineCopy:=False, isBoxCopy:=True)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Assert.Equal("lineBoxLine11" & vbCrLf & "    BoxLine2line2", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function PasteCommandWithInteractiveFormatAsBoxCopyOnBlankLine() As System.Threading.Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                editorOperations.InsertNewLine()
                editorOperations.InsertText("line1")
                editorOperations.InsertNewLine()
                editorOperations.InsertText("line2")
                editorOperations.MoveLineUp(False)
                editorOperations.MoveLineUp(False)

                Assert.Equal(vbCrLf & "line1" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim blocks = New BufferBlock() _
                {
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Input, "BoxLine1"),
                    New BufferBlock(ReplSpanKind.LineBreak, vbCrLf),
                    New BufferBlock(ReplSpanKind.Prompt, "> "),
                    New BufferBlock(ReplSpanKind.Input, "BoxLine2"),
                    New BufferBlock(ReplSpanKind.LineBreak, vbCrLf)
                }
                CopyToClipboard(clipboard, blocks, includeRepl:=True, isLineCopy:=False, isBoxCopy:=True)

                handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Assert.Equal("BoxLine1" & vbCrLf & "BoxLine2" & vbCrLf & "line1" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        Private Sub CopyToClipboard(clipboard As MockClipboard, blocks As BufferBlock(), includeRepl As Boolean, isLineCopy As Boolean, isBoxCopy As Boolean)
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
            If isLineCopy Then
                data.SetData(InteractiveWindow.ClipboardLineBasedCutCopyTag, True)
            End If
            If isBoxCopy Then
                data.SetData(InteractiveWindow.BoxSelectionCutCopyTag, True)
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

            Public Function GetDataObject() As IDataObject Implements InteractivePasteCommandHandler.IRoslynClipboard.GetDataObject
                Return _data
            End Function
        End Class
    End Class
End Namespace

