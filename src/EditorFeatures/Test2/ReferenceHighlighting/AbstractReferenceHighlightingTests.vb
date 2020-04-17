' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.Shared.Tagging
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities.RemoteHost
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    <[UseExportProvider]>
    Public MustInherit Class AbstractReferenceHighlightingTests
        Protected Async Function VerifyHighlightsAsync(test As XElement, Optional optionIsEnabled As Boolean = True) As Tasks.Task
            Await VerifyHighlightsAsync(test, optionIsEnabled, outOfProcess:=False)
            Await VerifyHighlightsAsync(test, optionIsEnabled, outOfProcess:=True)
        End Function

        Private Async Function VerifyHighlightsAsync(test As XElement, optionIsEnabled As Boolean, outOfProcess As Boolean) As Tasks.Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractReferenceHighlightingTests)}.{NameOf(Me.VerifyHighlightsAsync)} creates asynchronous taggers")
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(RemoteHostOptions.RemoteHostTest, outOfProcess)))

                Dim tagProducer = New ReferenceHighlightingViewTaggerProvider(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    workspace.GetService(Of IForegroundNotificationService),
                    AsynchronousOperationListenerProvider.NullProvider)

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(FeatureOnOffOptions.ReferenceHighlighting, hostDocument.Project.Language, optionIsEnabled)))

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim context = New TaggerContext(Of NavigableHighlightTag)(
                    document, snapshot, New SnapshotPoint(snapshot, caretPosition))
                Await tagProducer.GetTestAccessor().ProduceTagsAsync(context)

                Dim producedTags = From tag In context.tagSpans
                                   Order By tag.Span.Start
                                   Let spanType = If(tag.Tag.Type = DefinitionHighlightTag.TagId, "Definition",
                                       If(tag.Tag.Type = WrittenReferenceHighlightTag.TagId, "WrittenReference", "Reference"))
                                   Select spanType + ":" + tag.Span.Span.ToTextSpan().ToString()

                Dim expectedTags As New List(Of String)

                For Each hostDocument In workspace.Documents
                    For Each nameAndSpans In hostDocument.AnnotatedSpans.OrderBy(Function(x) x.Value.First().Start)
                        For Each span In nameAndSpans.Value
                            expectedTags.Add(nameAndSpans.Key + ":" + span.ToString())
                        Next
                    Next
                Next

                AssertEx.Equal(expectedTags, producedTags)
            End Using
        End Function
    End Class
End Namespace
