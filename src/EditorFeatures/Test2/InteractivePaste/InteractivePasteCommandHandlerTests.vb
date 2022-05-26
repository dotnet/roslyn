' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports System.Windows
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Interactive
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InteractivePaste
    <[UseExportProvider]>
    Public Class InteractivePasteCommandhandlerTests
        Private Const ClipboardLineBasedCutCopyTag As String = "VisualStudioEditorOperationsLineCutCopyClipboardTag"
        Private Const BoxSelectionCutCopyTag As String = "MSDEVColumnSelect"

        Private Shared Function CreateCommandHandler(workspace As TestWorkspace) As InteractivePasteCommandHandler
            Dim handler = workspace.ExportProvider.GetCommandHandler(Of InteractivePasteCommandHandler)(PredefinedCommandHandlerNames.InteractivePaste)
            handler.RoslynClipboard = New MockClipboard()
            Return handler
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithInteractiveFormat()
            Using workspace = TestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>,
                    composition:=EditorTestCompositions.EditorFeaturesWpf)

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim json = "
                [
                    {""content"":""a\u000d\u000abc"",""kind"":1},
                    {""content"":""> "",""kind"":0},
                    {""content"":""< "",""kind"":0},
                    {""content"":""12"",""kind"":2},
                    {""content"":""3"",""kind"":3},
                ]"

                Dim text = $"a{vbCrLf}bc123"

                CopyToClipboard(clipboard, text, json, includeRepl:=True, isLineCopy:=False, isBoxCopy:=False)

                Assert.True(handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), TestCommandExecutionContext.Create()))

                Assert.Equal("a" & vbCrLf & "bc123", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithOutInteractiveFormat()
            Using workspace = TestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>,
                    composition:=EditorTestCompositions.EditorFeaturesWpf)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim json = "
[
    {""content"":""a\u000d\u000abc"",""kind"":1},
    {""content"":""> "",""kind"":0},
    {""content"":""< "",""kind"":0},
    {""content"":""12"",""kind"":2},
    {""content"":""3"",""kind"":3}]
]"
                Dim text = $"a{vbCrLf}bc123"

                CopyToClipboard(clipboard, text, json, includeRepl:=False, isLineCopy:=False, isBoxCopy:=False)

                If Not handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), TestCommandExecutionContext.Create()) Then
                    editorOperations.InsertText("p")
                End If

                Assert.Equal("p", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithInteractiveFormatAsLineCopy()
            Using workspace = TestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>,
                    composition:=EditorTestCompositions.EditorFeaturesWpf)

                Dim textView = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(textView)

                editorOperations.InsertText("line1")
                editorOperations.InsertNewLine()
                editorOperations.InsertText("line2")

                Assert.Equal("line1" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())

                Dim handler = CreateCommandHandler(workspace)
                Dim clipboard = DirectCast(handler.RoslynClipboard, MockClipboard)

                Dim json = "
[
    {""content"":""> "",""kind"":0},
    {""content"":""InsertedLine"",""kind"":2},
    {""content"":""\u000d\u000a"",""kind"":4}]
]"
                Dim text = $"InsertedLine{vbCrLf}"

                CopyToClipboard(clipboard, text, json, includeRepl:=True, isLineCopy:=True, isBoxCopy:=False)

                Assert.True(handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), TestCommandExecutionContext.Create()))

                Assert.Equal("line1" & vbCrLf & "InsertedLine" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithInteractiveFormatAsBoxCopy()
            Using workspace = TestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>,
                    composition:=EditorTestCompositions.EditorFeaturesWpf)

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

                Dim json = "
[
    {""content"":""> "",""kind"":0},
    {""content"":""BoxLine1"",""kind"":2},
    {""content"":""\u000d\u000a"",""kind"":4},
    {""content"":""> "",""kind"":0},
    {""content"":""BoxLine2"",""kind"":2},
    {""content"":""\u000d\u000a"",""kind"":4}]
]"
                Dim text = $"BoxLine1{vbCrLf}BoxLine2{vbCrLf}"

                CopyToClipboard(clipboard, text, json, includeRepl:=True, isLineCopy:=False, isBoxCopy:=True)

                Assert.True(handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), TestCommandExecutionContext.Create()))

                Assert.Equal("lineBoxLine11" & vbCrLf & "    BoxLine2line2", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub PasteCommandWithInteractiveFormatAsBoxCopyOnBlankLine()
            Using workspace = TestWorkspace.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document/>
                        </Project>
                    </Workspace>,
                    composition:=EditorTestCompositions.EditorFeaturesWpf)

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

                Dim json = "
                [
                    {""content"":""> "",""kind"":0},
                    {""content"":""BoxLine1"",""kind"":2},
                    {""content"":""\u000d\u000a"",""kind"":4},
                    {""content"":""> "",""kind"":0},
                    {""content"":""BoxLine2"",""kind"":2},
                    {""content"":""\u000d\u000a"",""kind"":4}
                ]"

                Dim text = $"> BoxLine1{vbCrLf}> BoxLine2{vbCrLf}"

                CopyToClipboard(clipboard, text, json, includeRepl:=True, isLineCopy:=False, isBoxCopy:=True)

                Assert.True(handler.ExecuteCommand(New PasteCommandArgs(textView, textView.TextBuffer), TestCommandExecutionContext.Create()))

                Assert.Equal("BoxLine1" & vbCrLf & "BoxLine2" & vbCrLf & "line1" & vbCrLf & "    line2", textView.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        Private Shared Sub CopyToClipboard(clipboard As MockClipboard, text As String, json As String, includeRepl As Boolean, isLineCopy As Boolean, isBoxCopy As Boolean)
            clipboard.Clear()

            Dim data = New DataObject()
            Dim builder = New StringBuilder()

            data.SetData(DataFormats.UnicodeText, text)
            data.SetData(DataFormats.StringFormat, text)
            If includeRepl Then
                data.SetData(InteractiveClipboardFormat.Tag, json)
            End If

            If isLineCopy Then
                data.SetData(ClipboardLineBasedCutCopyTag, True)
            End If

            If isBoxCopy Then
                data.SetData(BoxSelectionCutCopyTag, True)
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

