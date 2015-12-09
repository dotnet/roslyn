' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting

    Public MustInherit Class AbstractKeywordHighlightingTests
        Protected Async Function VerifyHighlightsAsync(test As XElement, Optional optionIsEnabled As Boolean = True) As Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(test)
                Dim testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim buffer = testDocument.TextBuffer
                Dim snapshot = testDocument.InitialTextSnapshot
                Dim caretPosition = testDocument.CursorPosition.Value
                Dim document As Document = workspace.CurrentSolution.Projects.First.Documents.First

                workspace.Options = workspace.Options.WithChangedOption(FeatureOnOffOptions.KeywordHighlighting, document.Project.Language, optionIsEnabled)

                Dim highlightingService = workspace.GetService(Of IHighlightingService)()
                Dim tagProducer = New HighlighterViewTaggerProvider(
                    highlightingService,
                    workspace.GetService(Of IForegroundNotificationService),
                    AggregateAsynchronousOperationListener.EmptyListeners)

                Dim context = New TaggerContext(Of KeywordHighlightTag)(document, snapshot, New SnapshotPoint(snapshot, caretPosition))
                tagProducer.ProduceTagsAsync_ForTestingPurposesOnly(context).Wait()

                Dim producedTags = From tag In context.tagSpans
                                   Order By tag.Span.Start
                                   Select (tag.Span.Span.ToTextSpan().ToString())

                Dim expectedTags As New List(Of String)

                For Each hostDocument In workspace.Documents
                    For Each span In hostDocument.SelectedSpans
                        expectedTags.Add(span.ToString())
                    Next
                Next

                AssertEx.Equal(expectedTags, producedTags)
            End Using
        End Function

    End Class

End Namespace
