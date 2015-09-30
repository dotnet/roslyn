' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Public Class RenameCommandHandlerTests
        Private Function CreateCommandHandler(workspace As TestWorkspace) As RenameCommandHandler
            Return New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                            workspace.GetService(Of IEditorOperationsFactoryService),
                                            workspace.GetService(Of IWaitIndicator))
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCommandInvokesInlineRename()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class $$Foo
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                CreateCommandHandler(workspace).ExecuteCommand(New RenameCommandArgs(view, view.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Dim expectedTriggerToken = workspace.CurrentSolution.Projects.Single().Documents.Single().GetSyntaxRootAsync().Result.FindToken(view.Caret.Position.BufferPosition)
                Assert.Equal(expectedTriggerToken.Span.ToSnapshotSpan(view.TextSnapshot), view.Selection.SelectedSpans.Single())
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub RenameCommandDisabledInSubmission()
            Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(InteractiveDocumentSupportsCodeFixService)))

            Using workspace = TestWorkspaceFactory.CreateWorkspace(
                <Workspace>
                    <Submission Language="C#" CommonReferences="true">  
                        object $$foo;  
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                exportProvider:=exportProvider)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = CreateCommandHandler(workspace)
                Dim delegatedToNext = False
                Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                Dim state = handler.GetCommandState(New RenameCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCommandWithSelectionDoesNotSelect()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|F|]oo
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim document = workspace.Documents.Single()
                Dim view = document.GetTextView()
                Dim selectedSpan = workspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Single().ToSnapshotSpan(document.TextBuffer.CurrentSnapshot)
                view.Caret.MoveTo(selectedSpan.End)
                view.Selection.Select(selectedSpan, isReversed:=False)

                CreateCommandHandler(workspace).ExecuteCommand(New RenameCommandArgs(view, view.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))

                Assert.Equal(selectedSpan, view.Selection.SelectedSpans.Single())
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCommandWithReversedSelectionDoesNotSelectOrCrash()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class [|F|]oo
                                {
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim document = workspace.Documents.Single()
                Dim view = document.GetTextView()
                Dim selectedSpan = workspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Single().ToSnapshotSpan(document.TextBuffer.CurrentSnapshot)
                view.Caret.MoveTo(selectedSpan.End)
                view.Selection.Select(selectedSpan, isReversed:=True)

                CreateCommandHandler(workspace).ExecuteCommand(New RenameCommandArgs(view, view.TextBuffer), Sub() Throw New Exception("The operation should have been handled."))
                WaitForRename(workspace)
                Assert.Equal(selectedSpan.Span, view.Selection.SelectedSpans.Single().Span)
            End Using
        End Sub


        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingOutsideRenameSpanCommitsAndPreservesVirtualSelection()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class [|Fo$$o|]

End Class
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)
                Dim session = StartSession(workspace)

                editorOperations.MoveLineDown(extendSelection:=False)
                Assert.Equal(4, view.Caret.Position.VirtualSpaces)
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "p"c), Sub() editorOperations.InsertText("p"))

                Assert.Equal("    p", view.Caret.Position.BufferPosition.GetContainingLine.GetText())
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameCommandNotActiveWhenNotTouchingIdentifier()
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                                class Foo
                                {
                                    int |$$|x = 0;
                                }
                            </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim commandHandler = CreateCommandHandler(workspace)
                Dim commandState = commandHandler.GetCommandState(New RenameCommandArgs(view, view.TextBuffer), Function() New CommandState())

                Assert.True(commandState.IsAvailable)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingSpaceDuringRename()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class $$Foo
                                {
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)

                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, " "c), Sub() AssertEx.Fail("Space should not have been passed to the editor."))

                session.Cancel()
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingTabDuringRename()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class $$Foo
                                {
                                    Foo f;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))


                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)

                ' TODO: should we make tab wait instead?
                WaitForRename(workspace)

                ' Unfocus the dashboard
                Dim dashboard = DirectCast(view.GetAdornmentLayer("RoslynRenameDashboard").Elements(0).Adornment, Dashboard)
                dashboard.ShouldReceiveKeyboardNavigation = False

                commandHandler.ExecuteCommand(New TabKeyCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Tab should not have been passed to the editor."))

                Assert.Equal(3, view.Caret.Position.BufferPosition.GetContainingLine().LineNumber)

                session.Cancel()
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub SelectAllDuringRename()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class $$Foo // comment
{
Foo f;
}
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim startPosition = view.Caret.Position.BufferPosition.Position
                Dim identifierSpan = New Span(startPosition, 3)

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Assert.True(view.Selection.IsEmpty())
                Dim session = StartSession(workspace)
                WaitForRename(workspace)

                Assert.Equal(identifierSpan, view.Selection.SelectedSpans.Single().Span)
                Assert.Equal(identifierSpan.End, view.Caret.Position.BufferPosition.Position)
                view.Selection.Clear()
                Assert.True(view.Selection.IsEmpty())

                commandHandler.ExecuteCommand(New SelectAllCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Ctrl+A should not have been passed to the editor."))
                Assert.Equal(identifierSpan, view.Selection.SelectedSpans.Single().Span)
                Assert.Equal(identifierSpan.End, view.Caret.Position.BufferPosition.Position)

                commandHandler.ExecuteCommand(New SelectAllCommandArgs(view, view.TextBuffer), Sub() editorOperations.SelectAll())
                Assert.Equal(view.TextBuffer.CurrentSnapshot.GetFullSpan(), view.Selection.SelectedSpans.Single().Span)
            End Using
        End Sub

        <Fact>
        <WorkItem(851629)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub WordDeleteDuringRename()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class [|$$Foo|] // comment
{
}
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim startPosition = view.Caret.Position.BufferPosition.Position

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                WaitForRename(workspace)
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextSnapshot, startPosition))

                ' with the caret at the start, this should delete the whole identifier
                commandHandler.ExecuteCommand(New WordDeleteToEndCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Command should not have been passed to the editor."))
                VerifyTagsAreCorrect(workspace, "")

                editorOperations.InsertText("this")
                WaitForRename(workspace)
                Assert.Equal("@this", view.TextSnapshot.GetText(startPosition, 5))

                ' with a selection, we should delete the from the beginning of the rename span to the end of the selection
                ' Note that the default editor handling would try to delete the '@' character, we override this behavior since
                ' that '@' character is in a read only region during rename.
                view.Selection.Select(New SnapshotSpan(view.TextSnapshot, Span.FromBounds(startPosition + 2, startPosition + 4)), isReversed:=True)
                commandHandler.ExecuteCommand(New WordDeleteToStartCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Command should not have been passed to the editor."))
                VerifyTagsAreCorrect(workspace, "s")
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub NavigationDuringRename()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class $$Foo // comment
{
Foo f;
}
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim startPosition = view.Caret.Position.BufferPosition.Position
                Dim identifierSpan = New Span(startPosition, 3)
                view.Selection.Select(New SnapshotSpan(view.TextBuffer.CurrentSnapshot, identifierSpan), isReversed:=False)

                Dim lineStart = view.Caret.Position.BufferPosition.GetContainingLine().Start.Position
                Dim lineEnd = view.Caret.Position.BufferPosition.GetContainingLine().End.Position

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                WaitForRename(workspace)

#Region "LineStart"
                ' we start with the identifier selected
                Assert.Equal(identifierSpan, view.Selection.SelectedSpans.Single().Span)

                ' LineStart should move to the beginning of identifierSpan
                commandHandler.ExecuteCommand(New LineStartCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Home should not have been passed to the editor."))
                Assert.Equal(0, view.Selection.SelectedSpans.Single().Span.Length)
                Assert.Equal(startPosition, view.Caret.Position.BufferPosition.Position)

                ' LineStart again should move to the beginning of the line
                commandHandler.ExecuteCommand(New LineStartCommandArgs(view, view.TextBuffer), Sub() editorOperations.MoveToStartOfLine(extendSelection:=False))
                Assert.Equal(lineStart, view.Caret.Position.BufferPosition.Position)
#End Region
#Region "LineStartExtend"
                ' Reset the position to the middle of the identifier
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, startPosition + 1))

                ' LineStartExtend should move to the beginning of identifierSpan and extend the selection
                commandHandler.ExecuteCommand(New LineStartExtendCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Shift+Home should not have been passed to the editor."))
                Assert.Equal(New Span(startPosition, 1), view.Selection.SelectedSpans.Single().Span)

                ' LineStartExtend again should move to the beginning of the line and extend the selection
                commandHandler.ExecuteCommand(New LineStartExtendCommandArgs(view, view.TextBuffer), Sub() editorOperations.MoveToStartOfLine(extendSelection:=True))
                Assert.Equal(Span.FromBounds(lineStart, startPosition + 1), view.Selection.SelectedSpans.Single().Span)
#End Region
#Region "LineEnd"
                ' Reset the position to the middle of the identifier
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, startPosition + 1))

                ' LineEnd should move to the end of identifierSpan
                commandHandler.ExecuteCommand(New LineEndCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("End should not have been passed to the editor."))
                Assert.Equal(0, view.Selection.SelectedSpans.Single().Span.Length)
                Assert.Equal(identifierSpan.End, view.Caret.Position.BufferPosition.Position)

                ' LineEnd again should move to the end of the line
                commandHandler.ExecuteCommand(New LineEndCommandArgs(view, view.TextBuffer), Sub() editorOperations.MoveToEndOfLine(extendSelection:=False))
                Assert.Equal(lineEnd, view.Caret.Position.BufferPosition.Position)
#End Region
#Region "LineEndExtend"
                ' Reset the position to the middle of the identifier
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, startPosition + 1))

                ' LineEndExtend should move to the end of identifierSpan and extend the selection
                commandHandler.ExecuteCommand(New LineEndExtendCommandArgs(view, view.TextBuffer), Sub() AssertEx.Fail("Shift+End should not have been passed to the editor."))
                Assert.Equal(Span.FromBounds(startPosition + 1, identifierSpan.End), view.Selection.SelectedSpans.Single().Span)

                ' LineEndExtend again should move to the end of the line and extend the selection
                commandHandler.ExecuteCommand(New LineEndExtendCommandArgs(view, view.TextBuffer), Sub() editorOperations.MoveToEndOfLine(extendSelection:=True))
                Assert.Equal(Span.FromBounds(startPosition + 1, lineEnd), view.Selection.SelectedSpans.Single().Span)
#End Region
                session.Cancel()
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingTypeCharacterDuringRename()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                Class [|Fo$$o|]
                                End Class
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)
                Dim session = StartSession(workspace)

                editorOperations.MoveToNextCharacter(extendSelection:=False)
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "$"c), Sub() editorOperations.InsertText("$"))

                VerifyTagsAreCorrect(workspace, "Foo")

                session.Cancel()
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingInOtherPartsOfFileTriggersCommit()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class [|$$Foo|]
                                {
                                    [|Foo|] f;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Type first in the main identifier
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "B"c), Sub() editorOperations.InsertText("B"))

                ' Move selection and cursor to a readonly region
                Dim span = view.TextBuffer.CurrentSnapshot.GetSpanFromBounds(0, 0)
                view.Selection.Select(span, isReversed:=False)
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, span.End))

                ' Now let's type and that should commit Rename
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "Z"c), Sub() editorOperations.InsertText("Z"))

                VerifyTagsAreCorrect(workspace, "BFoo")

                ' Rename session was indeed committed and is no longer active
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' Verify that the key pressed went to the start of the file
                Assert.Equal("Z"c, view.TextBuffer.CurrentSnapshot(0))
            End Using
        End Sub

        <Fact()>
        <WorkItem(820248)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DeletingInEditSpanPropagatesEdit()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class [|$$Foo|]
                                {
                                    [|Foo|] f;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                view.Selection.Clear()
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Delete the first identifier char
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New DeleteKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Delete())

                VerifyTagsAreCorrect(workspace, "oo")
                Assert.NotNull(workspace.GetService(Of IInlineRenameService).ActiveSession)

                session.Cancel()
            End Using
        End Sub

        <Fact>
        <WorkItem(820248)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub BackspacingInEditSpanPropagatesEdit()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class [|Fo$$o|]
                                {
                                    [|Foo|] f;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                view.Selection.Clear()
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Delete the first identifier char
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New BackspaceKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Backspace())

                VerifyTagsAreCorrect(workspace, "Fo")
                Assert.NotNull(workspace.GetService(Of IInlineRenameService).ActiveSession)

                session.Cancel()
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub DeletingInOtherPartsOfFileTriggersCommit()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class [|$$Foo|]
                                {
                                    [|Foo|] f;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                view.Selection.Clear()
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Type first in the main identifier
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "B"c), Sub() editorOperations.InsertText("B"))

                ' Move selection and cursor to a readonly region
                Dim span = view.TextBuffer.CurrentSnapshot.GetSpanFromBounds(0, 0)
                view.Selection.Select(span, isReversed:=False)
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, span.End))

                ' Now let's type and that should commit Rename
                commandHandler.ExecuteCommand(New DeleteKeyCommandArgs(view, view.TextBuffer), Sub() editorOperations.Delete())

                VerifyTagsAreCorrect(workspace, "BFoo")

                ' Rename session was indeed committed and is no longer active
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)
            End Using
        End Sub

        <WorkItem(577178)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingInOtherFileTriggersCommit()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class [|$$Foo|]
                            {
                                [|Foo|] f;
                            }
                        </Document>
                        <Document>
                            class Bar
                            {
                                Bar b;
                            }
                        </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.ElementAt(0).GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                view.Selection.Clear()
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Type first in the main identifier
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "B"c), Sub() editorOperations.InsertText("B"))

                ' Move the cursor to the next file
                Dim newview = workspace.Documents.ElementAt(1).GetTextView()
                editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(newview)
                newview.Caret.MoveTo(New SnapshotPoint(newview.TextBuffer.CurrentSnapshot, 0))

                ' Type the char at the beginning of the file
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(newview, newview.TextBuffer, "Z"c), Sub() editorOperations.InsertText("Z"))

                VerifyTagsAreCorrect(workspace, "BFoo")

                ' Rename session was indeed committed and is no longer active
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' Verify that the key pressed went to the start of the file
                Assert.Equal("Z"c, newview.TextBuffer.CurrentSnapshot(0))

            End Using
        End Sub

        <WorkItem(577178)>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingInOtherFileWithConflictTriggersCommit()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public partial class A
                            {
                                public class [|$$B|]
                                {
                                }
                            }
                            
                        </Document>
                        <Document>
                            public partial class A
                            {
                                public BB bb;
                            }

                            public class BB
                            {
                            }
                        </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.ElementAt(0).GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                view.Selection.Clear()
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Type first in the main identifier
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "B"c), Sub() editorOperations.InsertText("B"))

                ' Move the cursor to the next file
                Dim newview = workspace.Documents.ElementAt(1).GetTextView()
                editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(newview)
                newview.Caret.MoveTo(New SnapshotPoint(newview.TextBuffer.CurrentSnapshot, 0))

                ' Type the char at the beginning of the file
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(newview, newview.TextBuffer, "Z"c), Sub() editorOperations.InsertText("Z"))

                VerifyTagsAreCorrect(workspace, "BB")

                ' Rename session was indeed committed and is no longer active
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' Verify that the key pressed went to the start of the file
                Assert.Equal("Z"c, newview.TextBuffer.CurrentSnapshot(0))

            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameMemberFromCref()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
class Program
{
    ///  <see cref="Program.[|$$Main|]"/> to start the program.
    static void [|Main|](string[] args)
    {
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                commandHandler.ExecuteCommand(New RenameCommandArgs(view, view.TextBuffer), Sub() Exit Sub)
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "Z"c), Sub() editorOperations.InsertText("Z"))
                commandHandler.ExecuteCommand(New ReturnKeyCommandArgs(view, view.TextBuffer), Sub() Exit Sub)

                VerifyTagsAreCorrect(workspace, "Z")
            End Using
        End Sub

        <Fact, WorkItem(878173), Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameInDocumentsWithoutOpenTextViews()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
                        <Document FilePath="Test.cs">
                            <![CDATA[
partial class  [|$$Program|]
{
    static void Main(string[] args)
    {
    }
}
]]>
                        </Document>
                        <Document FilePath="Test2.cs">
                            <![CDATA[
partial class [|Program|]
{
}
]]>
                        </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.First(Function(d) d.Name = "Test.cs").GetTextView()
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim closedDocument = workspace.Documents.First(Function(d) d.Name = "Test2.cs")
                closedDocument.CloseTextView()
                Assert.True(workspace.IsDocumentOpen(closedDocument.Id))
                commandHandler.ExecuteCommand(New RenameCommandArgs(view, view.TextBuffer), Sub() Exit Sub)
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "Z"c), Sub() editorOperations.InsertText("Z"))

                VerifyTagsAreCorrect(workspace, "Z")
            End Using
        End Sub

        <Fact>
        <WorkItem(942811)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingCtrlEnterDuringRenameCSharp()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class Foo
                                {
                                    int ba$$r;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                commandHandler.ExecuteCommand(New OpenLineAboveCommandArgs(view, view.TextBuffer), Sub() editorOperations.OpenLineAbove())

                ' verify rename session was committed.
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' verify the command was routed to the editor and an empty line was inserted.
                Assert.Equal(String.Empty, view.Caret.Position.BufferPosition.GetContainingLine.GetText())

            End Using
        End Sub

        <Fact>
        <WorkItem(942811)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingCtrlEnterOutsideSpansDuringRenameCSharp()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class Foo
                                {
                                    int ba$$r;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                ' Move caret out of rename session span
                editorOperations.MoveLineDown(extendSelection:=False)

                commandHandler.ExecuteCommand(New OpenLineAboveCommandArgs(view, view.TextBuffer), Sub() editorOperations.OpenLineAbove())

                ' verify rename session was committed.
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' verify the command was routed to the editor and an empty line was inserted.
                Assert.Equal(String.Empty, view.Caret.Position.BufferPosition.GetContainingLine.GetText())

            End Using
        End Sub

        <Fact>
        <WorkItem(942811)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingCtrlShiftEnterDuringRenameCSharp()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class Foo
                                {
                                    int ba$$r;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                commandHandler.ExecuteCommand(New OpenLineBelowCommandArgs(view, view.TextBuffer), Sub() editorOperations.OpenLineBelow())

                ' verify rename session was committed.
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' verify the command was routed to the editor and an empty line was inserted.
                Assert.Equal(String.Empty, view.Caret.Position.BufferPosition.GetContainingLine.GetText())

            End Using
        End Sub

        <Fact>
        <WorkItem(942811)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingCtrlEnterDuringRenameBasic()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                Class [|Fo$$o|]
                                End Class
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                commandHandler.ExecuteCommand(New OpenLineAboveCommandArgs(view, view.TextBuffer), Sub() editorOperations.OpenLineAbove())

                ' verify rename session was committed.
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' verify the command was routed to the editor and an empty line was inserted.
                Assert.Equal(String.Empty, view.Caret.Position.BufferPosition.GetContainingLine.GetText())
            End Using
        End Sub

        <Fact>
        <WorkItem(942811)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub TypingCtrlShiftEnterDuringRenameBasic()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                                Class [|Fo$$o|]
                                End Class
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                commandHandler.ExecuteCommand(New OpenLineBelowCommandArgs(view, view.TextBuffer), Sub() editorOperations.OpenLineBelow())

                ' verify rename session was committed.
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)

                ' verify the command was routed to the editor and an empty line was inserted.
                Assert.Equal(String.Empty, view.Caret.Position.BufferPosition.GetContainingLine.GetText())
            End Using
        End Sub

        <Fact>
        <WorkItem(1142095)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub SaveDuringRenameCommits()
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                                class [|$$Foo|]
                                {
                                    [|Foo|] f;
                                }
                            </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()

                Dim commandHandler As New RenameCommandHandler(workspace.GetService(Of InlineRenameService),
                                                               workspace.GetService(Of IEditorOperationsFactoryService),
                                                               workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                WaitForRename(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)

                ' Type first in the main identifier
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "B"c), Sub() editorOperations.InsertText("B"))

                ' Now save the document, which should commit Rename
                commandHandler.ExecuteCommand(New SaveCommandArgs(view, view.TextBuffer), Sub() Exit Sub)

                VerifyTagsAreCorrect(workspace, "BFoo")

                ' Rename session was indeed committed and is no longer active
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)
            End Using
        End Sub

        <Fact>
        <WorkItem(1142701)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub MoveSelectedLinesUpDuringRename()
            VerifyCommandCommitsRenameSessionAndExecutesCommand(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New MoveSelectedLinesUpCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(1142701)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub MoveSelectedLinesDownDuringRename()
            VerifyCommandCommitsRenameSessionAndExecutesCommand(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New MoveSelectedLinesDownCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(991517)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ReorderParametersDuringRename()
            VerifyCommandCommitsRenameSessionAndExecutesCommand(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New ReorderParametersCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(991517)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RemoveParametersDuringRename()
            VerifyCommandCommitsRenameSessionAndExecutesCommand(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New RemoveParametersCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(991517)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub ExtractInterfaceDuringRename()
            VerifyCommandCommitsRenameSessionAndExecutesCommand(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New ExtractInterfaceCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <WorkItem(991517)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub EncapsulateFieldDuringRename()
            VerifyCommandCommitsRenameSessionAndExecutesCommand(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New EncapsulateFieldCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CutDuringRename_InsideIdentifier()
            VerifySessionActiveAfterCutPasteInsideIdentifier(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New CutCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub PasteDuringRename_InsideIdentifier()
            VerifySessionActiveAfterCutPasteInsideIdentifier(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New PasteCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub CutDuringRename_OutsideIdentifier()
            VerifySessionCommittedAfterCutPasteOutsideIdentifier(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New CutCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub PasteDuringRename_OutsideIdentifier()
            VerifySessionCommittedAfterCutPasteOutsideIdentifier(
                Sub(commandHandler As RenameCommandHandler, view As IWpfTextView, nextHandler As Action)
                    commandHandler.ExecuteCommand(New PasteCommandArgs(view, view.TextBuffer), nextHandler)
                End Sub)
        End Sub

        Private Sub VerifyCommandCommitsRenameSessionAndExecutesCommand(executeCommand As Action(Of RenameCommandHandler, IWpfTextView, Action))
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
// Comment
class [|C$$|]
{
    [|C|] f;
}
                        </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                ' Type first in the main identifier
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "D"c), Sub() editorOperations.InsertText("D"))

                ' Then execute the command
                Dim commandInvokedString = "/*Command Invoked*/"
                executeCommand(commandHandler, view, Sub() editorOperations.InsertText(commandInvokedString))

                ' Verify rename session was committed.
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)
                Assert.Contains("D f", view.TextBuffer.CurrentSnapshot.GetText())

                ' Verify the command was routed to the editor.
                Assert.Contains(commandInvokedString, view.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        Private Sub VerifySessionActiveAfterCutPasteInsideIdentifier(executeCommand As Action(Of RenameCommandHandler, IWpfTextView, Action))
            Using workspace = CreateWorkspaceWithWaiter(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
// Comment
class [|C$$|]
{
    [|C|] f;
}
                        </Document>
                    </Project>
                </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                ' Then execute the command
                Dim commandInvokedString = "commandInvoked"
                executeCommand(commandHandler, view, Sub() editorOperations.InsertText(commandInvokedString))

                ' Verify rename session is still active
                Assert.NotNull(workspace.GetService(Of IInlineRenameService).ActiveSession)
                VerifyTagsAreCorrect(workspace, commandInvokedString)
            End Using
        End Sub

        Private Sub VerifySessionCommittedAfterCutPasteOutsideIdentifier(executeCommand As Action(Of RenameCommandHandler, IWpfTextView, Action))
            Using workspace = CreateWorkspaceWithWaiter(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
// Comment
class [|C$$|]
{
    [|C|] f;
}
                        </Document>
                        </Project>
                    </Workspace>)

                Dim view = workspace.Documents.Single().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))

                Dim renameService = workspace.GetService(Of InlineRenameService)()
                Dim commandHandler As New RenameCommandHandler(
                    renameService,
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IWaitIndicator))

                Dim session = StartSession(workspace)
                Dim editorOperations = workspace.GetService(Of IEditorOperationsFactoryService)().GetEditorOperations(view)

                ' Type first in the main identifier
                view.Selection.Clear()
                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value))
                commandHandler.ExecuteCommand(New TypeCharCommandArgs(view, view.TextBuffer, "D"c), Sub() editorOperations.InsertText("D"))

                ' Then execute the command
                Dim commandInvokedString = "commandInvoked"
                Dim selectionStart = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value - 6

                view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, selectionStart))
                view.SetSelection(New SnapshotSpan(view.TextBuffer.CurrentSnapshot, New Span(selectionStart, 2)))

                executeCommand(commandHandler, view, Sub() editorOperations.InsertText(commandInvokedString))

                ' Verify rename session was committed
                Assert.Null(workspace.GetService(Of IInlineRenameService).ActiveSession)
                Assert.Contains("D f", view.TextBuffer.CurrentSnapshot.GetText())
                Assert.Contains(commandInvokedString, view.TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub
    End Class
End Namespace
