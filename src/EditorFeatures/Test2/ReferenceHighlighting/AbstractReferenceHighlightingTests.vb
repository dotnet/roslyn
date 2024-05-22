' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.Shared.Tagging
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    <[UseExportProvider]>
    Public MustInherit Class AbstractReferenceHighlightingTests
        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures.WithTestHostParts(TestHost.OutOfProcess).AddParts(
            GetType(NoCompilationContentTypeDefinitions),
            GetType(NoCompilationContentTypeLanguageService))

        Protected Async Function VerifyHighlightsAsync(test As XElement, testHost As TestHost, Optional optionIsEnabled As Boolean = True) As Task
            Using workspace = EditorTestWorkspace.Create(test, composition:=s_composition.WithTestHostParts(testHost))
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractReferenceHighlightingTests)}.{NameOf(Me.VerifyHighlightsAsync)} creates asynchronous taggers")

                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                Dim tagProducer = New ReferenceHighlightingViewTaggerProvider(
                    workspace.GetService(Of IThreadingContext),
                    globalOptions,
                    visibilityTracker:=Nothing,
                    AsynchronousOperationListenerProvider.NullProvider)

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot

                globalOptions.SetGlobalOption(ReferenceHighlightingOptionsStorage.ReferenceHighlighting, hostDocument.Project.Language, optionIsEnabled)

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim context = New TaggerContext(Of NavigableHighlightTag)(
                    document, snapshot, New SnapshotPoint(snapshot, caretPosition))
                Await tagProducer.GetTestAccessor().ProduceTagsAsync(context)

                Dim producedTags = From tag In context.TagSpans
                                   Order By tag.Span.Start
                                   Let spanType = If(tag.Tag.Type = DefinitionHighlightTag.TagId, "Definition",
                                       If(tag.Tag.Type = WrittenReferenceHighlightTag.TagId, "WrittenReference", "Reference"))
                                   Select spanType + ":" + tag.Span.Span.ToTextSpan().ToString()

                Dim expectedTags As New List(Of String)

                For Each hostDocument In workspace.Documents
                    Dim nameAndSpansList = hostDocument.AnnotatedSpans.SelectMany(
                        Function(name) name.Value,
                        Function(name, span) _
                        New With {.Name = name.Key,
                                  .Span = span
                        })

                    For Each nameAndSpan In nameAndSpansList.OrderBy(Function(x) x.Span.Start)
                        expectedTags.Add(nameAndSpan.Name + ":" + nameAndSpan.Span.ToString())
                    Next
                Next

                AssertEx.Equal(expectedTags, producedTags)

            End Using
        End Function
    End Class
End Namespace
