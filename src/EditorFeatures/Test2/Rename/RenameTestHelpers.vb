﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Friend Module RenameTestHelpers

        Private ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeaturesWpf.AddParts(
            GetType(MockDocumentNavigationServiceFactory),
            GetType(MockPreviewDialogService),
            GetType(TestExperimentationService))

        Private Function GetSessionInfo(workspace As TestWorkspace) As (document As Document, textSpan As TextSpan)
            Dim hostdoc = workspace.DocumentWithCursor
            Dim caretPosition = hostdoc.CursorPosition.Value

            Dim textBuffer = hostdoc.GetTextBuffer()

            ' Make sure the undo manager is hooked up to this text buffer. This is automatically
            ' done in any real editor.
            workspace.GetService(Of ITextBufferUndoManagerProvider).GetTextBufferUndoManager(textBuffer)

            Dim solution = workspace.CurrentSolution
            Dim token = solution.GetDocument(hostdoc.Id).GetSyntaxRootAsync().Result.FindToken(caretPosition)

            Return (solution.GetDocument(hostdoc.Id), token.Span)
        End Function

        Public Function StartSession(workspace As TestWorkspace) As InlineRenameSession
            Dim renameService = workspace.GetService(Of IInlineRenameService)()
            Dim sessionInfo = GetSessionInfo(workspace)

            Return DirectCast(renameService.StartInlineSession(sessionInfo.document, sessionInfo.textSpan).Session, InlineRenameSession)
        End Function

        Public Sub AssertTokenRenamable(workspace As TestWorkspace)
            Dim renameService = DirectCast(workspace.GetService(Of IInlineRenameService)(), InlineRenameService)
            Dim sessionInfo = GetSessionInfo(workspace)

            Dim result = renameService.StartInlineSession(sessionInfo.document, sessionInfo.textSpan, CancellationToken.None)
            Assert.True(result.CanRename)
            Assert.Null(result.LocalizedErrorMessage)
        End Sub

        Public Sub AssertTokenNotRenamable(workspace As TestWorkspace)
            Dim renameService = DirectCast(workspace.GetService(Of IInlineRenameService)(), InlineRenameService)
            Dim sessionInfo = GetSessionInfo(workspace)

            Dim result = renameService.StartInlineSession(sessionInfo.document, sessionInfo.textSpan, CancellationToken.None)
            Assert.False(result.CanRename)
            Assert.NotNull(result.LocalizedErrorMessage)
        End Sub

        Public Async Function VerifyTagsAreCorrect(workspace As TestWorkspace, newIdentifierName As String) As Task
            Await WaitForRename(workspace)
            For Each document In workspace.Documents
                For Each selectedSpan In document.SelectedSpans
                    Dim trackingSpan = document.InitialTextSnapshot.CreateTrackingSpan(selectedSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)
                    Assert.Equal(newIdentifierName, trackingSpan.GetText(document.GetTextBuffer().CurrentSnapshot).Trim)
                Next
            Next

            For Each document In workspace.Documents
                For Each annotations In document.AnnotatedSpans
                    Dim expectedReplacementText = annotations.Key

                    If expectedReplacementText <> "CONFLICT" Then
                        For Each annotatedSpan In annotations.Value
                            Dim trackingSpan = document.InitialTextSnapshot.CreateTrackingSpan(annotatedSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)
                            Assert.Equal(expectedReplacementText, trackingSpan.GetText(document.GetTextBuffer().CurrentSnapshot))
                        Next
                    End If
                Next
            Next
        End Function

        Public Sub VerifyFileName(document As Document, newIdentifierName As String)
            Dim expectedName = Path.ChangeExtension(newIdentifierName, Path.GetExtension(document.Name))
            Assert.Equal(expectedName, document.Name)
        End Sub

        Public Sub VerifyFileName(workspace As TestWorkspace, newIdentifierName As String)
            Dim documentId = workspace.Documents.Single().Id
            VerifyFileName(workspace.CurrentSolution.GetDocument(documentId), newIdentifierName)
        End Sub

#Disable Warning IDE0060 ' Remove unused parameter - https://github.com/dotnet/roslyn/issues/45890
        Public Function CreateWorkspaceWithWaiter(element As XElement, host As RenameTestHost) As TestWorkspace
#Enable Warning IDE0060 ' Remove unused parameter
            Dim workspace = TestWorkspace.CreateWorkspace(element, composition:=s_composition)
            workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()
            Return workspace
        End Function

        Public Async Function WaitForRename(workspace As TestWorkspace) As Task
            Dim provider = workspace.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)
            Await provider.WaitAllDispatcherOperationAndTasksAsync(workspace, FeatureAttribute.EventHookup, FeatureAttribute.Rename, FeatureAttribute.RenameTracking)
        End Function

        Public Function CreateRenameTrackingTagger(workspace As TestWorkspace, document As TestHostDocument) As ITagger(Of RenameTrackingTag)
            Dim tracker = New RenameTrackingTaggerProvider(
                workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                workspace.ExportProvider.GetExport(Of IInlineRenameService)().Value,
                workspace.ExportProvider.GetExport(Of IDiagnosticAnalyzerService)().Value,
                workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider))

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
