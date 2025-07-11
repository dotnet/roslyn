' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Highlighting
Imports Microsoft.CodeAnalysis.KeywordHighlighting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting

    <[UseExportProvider]>
    Public MustInherit Class AbstractKeywordHighlightingTests
        Protected Async Function VerifyHighlightsAsync(test As XElement, Optional optionIsEnabled As Boolean = True) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(test)
                Dim testDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim snapshot = testDocument.GetTextBuffer().CurrentSnapshot
                Dim caretPosition = testDocument.CursorPosition.Value
                Dim document As Document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)

                globalOptions.SetGlobalOption(KeywordHighlightingOptionsStorage.KeywordHighlighting, document.Project.Language, optionIsEnabled)

                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractKeywordHighlightingTests)}.{NameOf(Me.VerifyHighlightsAsync)} creates asynchronous taggers")

                Dim tagProducer = New HighlighterViewTaggerProvider(
                    workspace.GetService(Of TaggerHost),
                    workspace.GetService(Of IHighlightingService))

                Dim context = New TaggerContext(Of KeywordHighlightTag)(document, snapshot, frozenPartialSemantics:=False, New SnapshotPoint(snapshot, caretPosition))
                Await tagProducer.GetTestAccessor().ProduceTagsAsync(context)

                Dim producedTags = From tag In context.TagSpans
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
