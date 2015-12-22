' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Friend Module RenameTestHelpers

        <ThreadStatic>
        Friend _exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(MockDocumentNavigationServiceFactory), GetType(RenameWaiter)))

        Friend ReadOnly Property ExportProvider As ExportProvider
            Get
                If _exportProvider Is Nothing Then
                    _exportProvider = MinimalTestExportProvider.CreateExportProvider(
                        TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(MockDocumentNavigationServiceFactory), GetType(RenameWaiter)))
                End If

                Return _exportProvider
            End Get
        End Property

        Private Function GetSessionInfo(workspace As TestWorkspace) As Tuple(Of Document, TextSpan)
            Dim hostdoc = workspace.DocumentWithCursor
            Dim caretPosition = hostdoc.CursorPosition.Value

            Dim textBuffer = hostdoc.GetTextBuffer()

            ' Make sure the undo manager is hooked up to this text buffer. This is automatically
            ' done in any real editor.
            workspace.GetService(Of ITextBufferUndoManagerProvider).GetTextBufferUndoManager(textBuffer)

            Dim solution = workspace.CurrentSolution
            Dim token = solution.GetDocument(hostdoc.Id).GetSyntaxRootAsync().Result.FindToken(caretPosition)

            Return Tuple.Create(solution.GetDocument(hostdoc.Id), token.Span)
        End Function

        Public Function StartSession(workspace As TestWorkspace) As InlineRenameSession
            Dim renameService = workspace.GetService(Of IInlineRenameService)()
            Dim sessionInfo = GetSessionInfo(workspace)

            Return DirectCast(renameService.StartInlineSession(sessionInfo.Item1, sessionInfo.Item2).Session, InlineRenameSession)
        End Function

        Public Sub AssertTokenRenamable(workspace As TestWorkspace)
            Dim renameService = DirectCast(workspace.GetService(Of IInlineRenameService)(), InlineRenameService)
            Dim sessionInfo = GetSessionInfo(workspace)

            Dim editorService = sessionInfo.Item1.GetLanguageService(Of IEditorInlineRenameService)
            Dim result = editorService.GetRenameInfoAsync(sessionInfo.Item1, sessionInfo.Item2.Start, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
            Assert.True(result.CanRename)
            Assert.Null(result.LocalizedErrorMessage)
        End Sub

        Public Sub AssertTokenNotRenamable(workspace As TestWorkspace)
            Dim renameService = DirectCast(workspace.GetService(Of IInlineRenameService)(), InlineRenameService)
            Dim sessionInfo = GetSessionInfo(workspace)

            Dim editorService = sessionInfo.Item1.GetLanguageService(Of IEditorInlineRenameService)
            Dim result = editorService.GetRenameInfoAsync(sessionInfo.Item1, sessionInfo.Item2.Start, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
            Assert.False(result.CanRename)
            Assert.NotNull(result.LocalizedErrorMessage)
        End Sub

        Public Async Function VerifyTagsAreCorrect(workspace As TestWorkspace, newIdentifierName As String) As Task
            Await WaitForRename(workspace)
            For Each document In workspace.Documents
                For Each selectedSpan In document.SelectedSpans
                    Dim trackingSpan = document.InitialTextSnapshot.CreateTrackingSpan(selectedSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)
                    Assert.Equal(newIdentifierName, trackingSpan.GetText(document.TextBuffer.CurrentSnapshot).Trim)
                Next
            Next

            For Each document In workspace.Documents
                For Each annotations In document.AnnotatedSpans
                    Dim expectedReplacementText = annotations.Key

                    If expectedReplacementText <> "CONFLICT" Then
                        For Each annotatedSpan In annotations.Value
                            Dim trackingSpan = document.InitialTextSnapshot.CreateTrackingSpan(annotatedSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)
                            Assert.Equal(expectedReplacementText, trackingSpan.GetText(document.TextBuffer.CurrentSnapshot))
                        Next
                    End If
                Next
            Next
        End Function

        Public Function CreateWorkspaceWithWaiter(workspace As XElement) As TestWorkspace
            Dim testWorkspace = TestWorkspaceFactory.CreateWorkspace(
                workspace,
                exportProvider:=ExportProvider)
            testWorkspace.GetOpenDocumentIds().Select(Function(id) testWorkspace.GetTestDocument(id).GetTextView()).ToList()
            Return testWorkspace
        End Function

        Public Async Function WaitForRename(workspace As TestWorkspace) As Task
            Dim waiters = workspace.ExportProvider.GetExportedValues(Of IAsynchronousOperationWaiter)
            Await waiters.WaitAllAsync()
        End Function

        Public Function CreateRenameTrackingTagger(workspace As TestWorkspace, document As TestHostDocument) As ITagger(Of RenameTrackingTag)
            Dim tracker = New RenameTrackingTaggerProvider(
                workspace.ExportProvider.GetExport(Of ITextUndoHistoryRegistry)().Value,
                workspace.ExportProvider.GetExport(Of IWaitIndicator)().Value,
                workspace.ExportProvider.GetExport(Of IInlineRenameService)().Value,
                workspace.ExportProvider.GetExport(Of IDiagnosticAnalyzerService)().Value,
                SpecializedCollections.SingletonEnumerable(New MockRefactorNotifyService()),
                DirectCast(workspace.ExportProvider.GetExports(Of IAsynchronousOperationListener, FeatureMetadata), IEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata))))

            Return tracker.CreateTagger(Of RenameTrackingTag)(document.GetTextBuffer())
        End Function

        Public Async Function VerifyNoRenameTrackingTags(tagger As ITagger(Of RenameTrackingTag), workspace As TestWorkspace, document As TestHostDocument) As Task
            Dim tags = Await GetRenameTrackingTags(tagger, workspace, document)
            Assert.Equal(0, tags.Count())
        End Function

        Public Async Function VerifyRenameTrackingTags(tagger As ITagger(Of RenameTrackingTag), workspace As TestWorkspace, document As TestHostDocument, expectedTagCount As Integer) As Task
            Dim tags = Await GetRenameTrackingTags(tagger, workspace, document)
            Assert.Equal(expectedTagCount, tags.Count())
        End Function

        Public Async Function GetRenameTrackingTags(tagger As ITagger(Of RenameTrackingTag), workspace As TestWorkspace, document As TestHostDocument) As Task(Of IEnumerable(Of ITagSpan(Of RenameTrackingTag)))
            Await WaitForRename(workspace)
            Dim view = document.GetTextView()
            Return tagger.GetTags(view.TextBuffer.CurrentSnapshot.GetSnapshotSpanCollection())
        End Function
    End Module
End Namespace
