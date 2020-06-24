' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.InlineParamNameHints
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineParamNameHints

    <[UseExportProvider]>
    Public MustInherit Class AbstractInlineParamNameHintsTests

        Protected Async Function VerifyParamHints(test As XElement, Optional optionIsEnabled As Boolean = True) As Tasks.Task
            Await VerifyParamHints(test, optionIsEnabled, outOfProcess:=False)
            Await VerifyParamHints(test, optionIsEnabled, outOfProcess:=True)
        End Function

        Private Async Function VerifyParamHints(test As XElement, optionIsEnabled As Boolean, outOfProcess As Boolean) As Tasks.Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineParamNameHintsTests)}.{NameOf(Me.VerifyParamHints)} creates asynchronous taggers")

                Dim tagProducer = New InlineParamNameHintsDataTaggerProvider(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    AsynchronousOperationListenerProvider.NullProvider,
                    workspace.GetService(Of IForegroundNotificationService))

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                Dim caretPosition = hostDocument.CursorPosition.Value
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim context = New TaggerContext(Of InlineParamNameHintDataTag)(
                    document, snapshot, New SnapshotPoint(snapshot, caretPosition))
                Await tagProducer.GetTestAccessor().ProduceTagsAsync(context)

                Dim producedTags = From tag In context.tagSpans
                                   Order By tag.Span.Start
                                   Let spanName = tag.Tag.TagName
                                   Select spanName + ":" + tag.Span.Span.ToTextSpan().ToString()

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
