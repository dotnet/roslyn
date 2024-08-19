' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineCommit
    Friend Class CommitTestData
        Implements IDisposable

        Public ReadOnly Buffer As ITextBuffer
        Public ReadOnly CommandHandler As CommitCommandHandler
        Public ReadOnly EditorOperations As IEditorOperations
        Public ReadOnly Workspace As EditorTestWorkspace
        Public ReadOnly View As ITextView
        Public ReadOnly UndoHistory As ITextUndoHistory
        Private ReadOnly _formatter As FormatterMock
        Private ReadOnly _inlineRenameService As InlineRenameServiceMock

        Public Shared Function Create(test As XElement) As CommitTestData
            Dim workspace = EditorTestWorkspace.Create(test, composition:=EditorTestCompositions.EditorFeaturesWpf)
            Return New CommitTestData(workspace)
        End Function

        Public Sub New(workspace As EditorTestWorkspace)
            Me.Workspace = workspace
            View = workspace.Documents.Single().GetTextView()
            View.Options.GlobalOptions.SetOptionValue(DefaultOptions.IndentStyleId, IndentingStyle.Smart)

            EditorOperations = workspace.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(View)

            Dim position = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue).CursorPosition.Value
            View.Caret.MoveTo(New SnapshotPoint(View.TextSnapshot, position))

            Buffer = workspace.Documents.Single().GetTextBuffer()

            ' HACK: We may have already created a CommitBufferManager for the buffer, so remove it
            If Buffer.Properties.ContainsProperty(GetType(CommitBufferManager)) Then
                Dim oldManager = Buffer.Properties.GetProperty(Of CommitBufferManager)(GetType(CommitBufferManager))
                oldManager.RemoveReferencingView()
                Buffer.Properties.RemoveProperty(GetType(CommitBufferManager))
            End If

            Dim textUndoHistoryRegistry = workspace.GetService(Of ITextUndoHistoryRegistry)()
            UndoHistory = textUndoHistoryRegistry.GetHistory(View.TextBuffer)

            _formatter = New FormatterMock(workspace)
            _inlineRenameService = New InlineRenameServiceMock()
            Dim commitManagerFactory As New CommitBufferManagerFactory(_formatter, _inlineRenameService, workspace.GetService(Of IThreadingContext))

            ' Make sure the manager exists for the buffer
            Dim commitManager = commitManagerFactory.CreateForBuffer(Buffer)
            commitManager.AddReferencingView()

            CommandHandler = New CommitCommandHandler(
                commitManagerFactory,
                workspace.GetService(Of IEditorOperationsFactoryService),
                workspace.GetService(Of ISmartIndentationService),
                textUndoHistoryRegistry,
                workspace.GetService(Of IGlobalOptionService))
        End Sub

        Friend Sub AssertHadCommit(expectCommit As Boolean)
            Assert.Equal(expectCommit, _formatter.GotCommit)
        End Sub

        Friend Sub AssertUsedSemantics(expected As Boolean)
            Assert.Equal(expected, _formatter.UsedSemantics)
        End Sub

        Friend Sub StartInlineRenameSession()
            _inlineRenameService.HasSession = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Workspace.Dispose()
        End Sub

        Private Class InlineRenameServiceMock
            Implements IInlineRenameService

            Public Property HasSession As Boolean

            Public ReadOnly Property ActiveSession As IInlineRenameSession Implements IInlineRenameService.ActiveSession
                Get
                    Return If(HasSession, New MockInlineRenameSession(), Nothing)
                End Get
            End Property

            Public Function StartInlineSession(snapshot As Document, triggerSpan As TextSpan, cancellationToken As CancellationToken) As InlineRenameSessionInfo Implements IInlineRenameService.StartInlineSession
                Throw New NotImplementedException()
            End Function

            Private Class MockInlineRenameSession
                Implements IInlineRenameSession

                Public Sub Cancel() Implements IInlineRenameSession.Cancel
                    Throw New NotImplementedException()
                End Sub

                Public Function CommitAsync(previewChanges As Boolean, cancellationToken As CancellationToken) As Task(Of Boolean) Implements IInlineRenameSession.CommitAsync
                    Throw New NotImplementedException
                End Function
            End Class
        End Class

        Private Class FormatterMock
            Implements ICommitFormatter

            Private ReadOnly _testWorkspace As EditorTestWorkspace
            Public Property GotCommit As Boolean

            Public Property UsedSemantics As Boolean

            Public Sub New(testWorkspace As EditorTestWorkspace)
                _testWorkspace = testWorkspace
            End Sub

            Public Sub CommitRegion(spanToFormat As SnapshotSpan,
                                    isExplicitFormat As Boolean,
                                    useSemantics As Boolean,
                                    dirtyRegion As SnapshotSpan,
                                    baseSnapshot As ITextSnapshot,
                                    baseTree As SyntaxTree,
                                    cancellationToken As CancellationToken) Implements ICommitFormatter.CommitRegion
                GotCommit = True
                UsedSemantics = useSemantics

                ' Assert the span if we have an assertion
                If _testWorkspace.Documents.Any(Function(d) d.SelectedSpans.Any()) Then
                    Dim expectedSpan = _testWorkspace.Documents.Single(Function(d) d.SelectedSpans.Any()).SelectedSpans.Single()
                    Dim trackingSpan = _testWorkspace.Documents.Single().InitialTextSnapshot.CreateTrackingSpan(expectedSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)

                    Assert.Equal(trackingSpan.GetSpan(spanToFormat.Snapshot), spanToFormat.Span)
                End If

                Dim realCommitFormatter = Assert.IsType(Of CommitFormatter)(_testWorkspace.GetService(Of ICommitFormatter)())
                realCommitFormatter.CommitRegion(spanToFormat, isExplicitFormat, useSemantics, dirtyRegion, baseSnapshot, baseTree, cancellationToken)
            End Sub
        End Class
    End Class
End Namespace
