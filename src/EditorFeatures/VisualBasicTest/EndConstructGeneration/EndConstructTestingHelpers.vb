' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Moq
Imports Roslyn.Test.EditorUtilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Friend Module EndConstructTestingHelpers

        Private Function CreateMockIndentationService() As ISmartIndentationService
            Dim mock As New Mock(Of ISmartIndentationService)
            mock.Setup(Function(service) service.GetDesiredIndentation(It.IsAny(Of ITextView), It.IsAny(Of ITextSnapshotLine))).Returns(0)
            Return mock.Object
        End Function

        <ThreadStatic>
        Private t_disabledLineCommitExportProvider As ExportProvider

        Private ReadOnly Property DisabledLineCommitExportProvider As ExportProvider
            Get
                If t_disabledLineCommitExportProvider Is Nothing Then
                    t_disabledLineCommitExportProvider = TestExportProvider.CreateExportProviderWithCSharpAndVisualBasic()
                End If

                Return t_disabledLineCommitExportProvider
            End Get
        End Property

        Private Sub DisableLineCommit(workspace As Workspace)
            ' Disable line commit
            Dim optionsService = workspace.Services.GetService(Of IOptionService)()
            optionsService.SetOptions(optionsService.GetOptions().WithChangedOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, False))
        End Sub

        Private Async Function VerifyTypedCharAppliedAsync(doFunc As Func(Of VisualBasicEndConstructService, ITextView, ITextBuffer, Boolean),
                                           before As String,
                                           after As String,
                                           typedChar As Char,
                                           endCaretPos As Integer()) As Tasks.Task
            Dim caretPos = before.IndexOf("$$", StringComparison.Ordinal)
            Dim beforeText = before.Replace("$$", "")
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(beforeText, exportProvider:=DisabledLineCommitExportProvider)
                DisableLineCommit(workspace)

                Dim view = workspace.Documents.First().GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextSnapshot, caretPos))

                Dim endConstructService As New VisualBasicEndConstructService(
                    CreateMockIndentationService(),
                    workspace.GetService(Of ITextUndoHistoryRegistry),
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IEditorOptionsFactoryService))
                view.TextBuffer.Replace(New Span(caretPos, 0), typedChar.ToString())

                Assert.True(doFunc(endConstructService, view, view.TextBuffer))
                Assert.Equal(after, view.TextSnapshot.GetText())

                Dim actualLine As Integer
                Dim actualCol As Integer
                view.Caret.Position.BufferPosition.GetLineAndColumn(actualLine, actualCol)
                Assert.Equal(endCaretPos(0), actualLine)
                Assert.Equal(endCaretPos(1), actualCol)
            End Using
        End Function

        Private Async Function VerifyAppliedAsync(doFunc As Func(Of VisualBasicEndConstructService, ITextView, ITextBuffer, Boolean),
                                  before As String,
                                  beforeCaret As Integer(),
                                  after As String,
                                  afterCaret As Integer()) As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(before, exportProvider:=DisabledLineCommitExportProvider)
                DisableLineCommit(workspace)

                Dim textView = workspace.Documents.First().GetTextView()
                Dim subjectBuffer = workspace.Documents.First().GetTextBuffer()

                textView.TryMoveCaretToAndEnsureVisible(GetSnapshotPointFromArray(textView, beforeCaret, beforeCaret.Length - 2))

                If beforeCaret.Length = 4 Then
                    Dim span = New SnapshotSpan(
                            GetSnapshotPointFromArray(textView, beforeCaret, 0),
                            GetSnapshotPointFromArray(textView, beforeCaret, 2))

                    textView.SetSelection(span)
                End If

                Dim endConstructService As New VisualBasicEndConstructService(
                    CreateMockIndentationService(),
                    workspace.GetService(Of ITextUndoHistoryRegistry),
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IEditorOptionsFactoryService))

                Assert.True(doFunc(endConstructService, textView, textView.TextSnapshot.TextBuffer))
                Assert.Equal(EditorFactory.LinesToFullText(after), textView.TextSnapshot.GetText())

                Dim afterLine = textView.TextSnapshot.GetLineFromLineNumber(afterCaret(0))
                Dim afterCaretPoint As SnapshotPoint
                If afterCaret(1) = -1 Then
                    afterCaretPoint = afterLine.End
                Else
                    afterCaretPoint = New SnapshotPoint(textView.TextSnapshot, afterLine.Start + afterCaret(1))
                End If

                Assert.Equal(Of Integer)(afterCaretPoint, textView.GetCaretPoint(subjectBuffer).Value.Position)
            End Using
        End Function

        Private Function GetSnapshotPointFromArray(view As ITextView, caret As Integer(), startIndex As Integer) As SnapshotPoint
            Dim line = view.TextSnapshot.GetLineFromLineNumber(caret(startIndex))

            If caret(startIndex + 1) = -1 Then
                Return line.End
            Else
                Return line.Start + caret(startIndex + 1)
            End If
        End Function

        Private Async Function VerifyNotAppliedAsync(doFunc As Func(Of VisualBasicEndConstructService, ITextView, ITextBuffer, Boolean),
                                     text As String,
                                     caret As Integer()) As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(text)
                Dim textView = workspace.Documents.First().GetTextView()
                Dim subjectBuffer = workspace.Documents.First().GetTextBuffer()

                Dim line = textView.TextSnapshot.GetLineFromLineNumber(caret(0))
                Dim caretPosition As SnapshotPoint
                If caret(1) = -1 Then
                    caretPosition = line.End
                Else
                    caretPosition = New SnapshotPoint(textView.TextSnapshot, line.Start + caret(1))
                End If

                textView.TryMoveCaretToAndEnsureVisible(caretPosition)

                Dim endConstructService As New VisualBasicEndConstructService(
                    CreateMockIndentationService(),
                    workspace.GetService(Of ITextUndoHistoryRegistry),
                    workspace.GetService(Of IEditorOperationsFactoryService),
                    workspace.GetService(Of IEditorOptionsFactoryService))

                Assert.False(doFunc(endConstructService, textView, textView.TextSnapshot.TextBuffer), "End Construct should not have generated anything.")

                ' The text should not have changed
                Assert.Equal(EditorFactory.LinesToFullText(text), textView.TextSnapshot.GetText())

                ' The caret should not have moved
                Assert.Equal(Of Integer)(caretPosition, textView.GetCaretPoint(subjectBuffer).Value.Position)
            End Using
        End Function

        Public Function VerifyStatementEndConstructAppliedAsync(before As String, beforeCaret As Integer(), after As String, afterCaret As Integer()) As Tasks.Task
            Return VerifyAppliedAsync(Function(s, v, b) s.TryDoEndConstructForEnterKey(v, b, CancellationToken.None), before, beforeCaret, after, afterCaret)
        End Function

        Public Async Function VerifyStatementEndConstructNotAppliedAsync(text As String, caret As Integer()) As Tasks.Task
            Await VerifyNotAppliedAsync(Function(s, v, b) s.TryDoEndConstructForEnterKey(v, b, CancellationToken.None), text, caret)
        End Function

        Public Function VerifyXmlElementEndConstructAppliedAsync(before As String, beforeCaret As Integer(), after As String, afterCaret As Integer()) As Tasks.Task
            Return VerifyAppliedAsync(Function(s, v, b) s.TryDoXmlElementEndConstruct(v, b, Nothing), before, beforeCaret, after, afterCaret)
        End Function

        Public Async Function VerifyXmlElementEndConstructNotAppliedAsync(text As String, caret As Integer()) As Tasks.Task
            Await VerifyNotAppliedAsync(Function(s, v, b) s.TryDoXmlElementEndConstruct(v, b, Nothing), text, caret)
        End Function

        Public Function VerifyXmlCommentEndConstructAppliedAsync(before As String, beforeCaret As Integer(), after As String, afterCaret As Integer()) As Tasks.Task
            Return VerifyAppliedAsync(Function(s, v, b) s.TryDoXmlCommentEndConstruct(v, b, Nothing), before, beforeCaret, after, afterCaret)
        End Function

        Public Async Function VerifyXmlCommentEndConstructNotAppliedAsync(text As String, caret As Integer()) As Tasks.Task
            Await VerifyNotAppliedAsync(Function(s, v, b) s.TryDoXmlCommentEndConstruct(v, b, Nothing), text, caret)
        End Function

        Public Function VerifyXmlCDataEndConstructAppliedAsync(before As String, beforeCaret As Integer(), after As String, afterCaret As Integer()) As Tasks.Task
            Return VerifyAppliedAsync(Function(s, v, b) s.TryDoXmlCDataEndConstruct(v, b, Nothing), before, beforeCaret, after, afterCaret)
        End Function

        Public Async Function VerifyXmlCDataEndConstructNotAppliedAsync(text As String, caret As Integer()) As Tasks.Task
            Await VerifyNotAppliedAsync(Function(s, v, b) s.TryDoXmlCDataEndConstruct(v, b, Nothing), text, caret)
        End Function

        Public Function VerifyXmlEmbeddedExpressionEndConstructAppliedAsync(before As String, beforeCaret As Integer(), after As String, afterCaret As Integer()) As Tasks.Task
            Return VerifyAppliedAsync(Function(s, v, b) s.TryDoXmlEmbeddedExpressionEndConstruct(v, b, Nothing), before, beforeCaret, after, afterCaret)
        End Function

        Public Async Function VerifyXmlEmbeddedExpressionEndConstructNotAppliedAsync(text As String, caret As Integer()) As Tasks.Task
            Await VerifyNotAppliedAsync(Function(s, v, b) s.TryDoXmlEmbeddedExpressionEndConstruct(v, b, Nothing), text, caret)
        End Function

        Public Function VerifyXmlProcessingInstructionEndConstructAppliedAsync(before As String, beforeCaret As Integer(), after As String, afterCaret As Integer()) As Tasks.Task
            Return VerifyAppliedAsync(Function(s, v, b) s.TryDoXmlProcessingInstructionEndConstruct(v, b, Nothing), before, beforeCaret, after, afterCaret)
        End Function

        Public Async Function VerifyXmlProcessingInstructionNotAppliedAsync(text As String, caret As Integer()) As Tasks.Task
            Await VerifyNotAppliedAsync(Function(s, v, b) s.TryDoXmlProcessingInstructionEndConstruct(v, b, Nothing), text, caret)
        End Function

        Public Async Function VerifyEndConstructAppliedAfterCharAsync(before As String, after As String, typedChar As Char, endCaretPos As Integer()) As Tasks.Task
            Await VerifyTypedCharAppliedAsync(Function(s, v, b) s.TryDo(v, b, typedChar, Nothing), before, after, typedChar, endCaretPos)
        End Function

        Public Async Function VerifyEndConstructNotAppliedAfterCharAsync(before As String, after As String, typedChar As Char, endCaretPos As Integer()) As Tasks.Task
            Await VerifyTypedCharAppliedAsync(Function(s, v, b) Not s.TryDo(v, b, typedChar, Nothing), before, after, typedChar, endCaretPos)
        End Function

        Public Async Function VerifyAppliedAfterReturnUsingCommandHandlerAsync(
            before As String,
            beforeCaret As Integer(),
            after As String,
            afterCaret As Integer()) As Tasks.Task

            ' create separate composition
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(before, exportProvider:=DisabledLineCommitExportProvider)
                DisableLineCommit(workspace)

                Dim view = workspace.Documents.First().GetTextView()
                Dim line = view.TextSnapshot.GetLineFromLineNumber(beforeCaret(0))
                If beforeCaret(1) = -1 Then
                    view.Caret.MoveTo(line.End)
                Else
                    view.Caret.MoveTo(New SnapshotPoint(view.TextSnapshot, line.Start + beforeCaret(1)))
                End If

                Dim factory = workspace.ExportProvider.GetExportedValue(Of IEditorOperationsFactoryService)()
                Dim endConstructor = New EndConstructCommandHandler(
                                    factory,
                                    workspace.ExportProvider.GetExportedValue(Of ITextUndoHistoryRegistry)())

                Dim operations = factory.GetEditorOperations(view)
                endConstructor.ExecuteCommand_ReturnKeyCommandHandler(New ReturnKeyCommandArgs(view, view.TextBuffer), Sub() operations.InsertNewLine())

                Assert.Equal(after, view.TextSnapshot.GetText())

                Dim afterLine = view.TextSnapshot.GetLineFromLineNumber(afterCaret(0))
                Dim afterCaretPoint As Integer
                If afterCaret(1) = -1 Then
                    afterCaretPoint = afterLine.End
                Else
                    afterCaretPoint = afterLine.Start + afterCaret(1)
                End If

                Dim caretPosition = view.Caret.Position.VirtualBufferPosition
                Assert.Equal(Of Integer)(afterCaretPoint, If(caretPosition.IsInVirtualSpace, caretPosition.Position + caretPosition.VirtualSpaces, caretPosition.Position))
            End Using
        End Function
    End Module
End Namespace
