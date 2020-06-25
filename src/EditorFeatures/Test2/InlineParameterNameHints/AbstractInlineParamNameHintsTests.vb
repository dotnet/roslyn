' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineParameterNameHints
    <[UseExportProvider]>
    Public MustInherit Class AbstractInlineParamNameHintsTests

        Protected Async Function VerifyParamHints(test As XElement, Optional optionIsEnabled As Boolean = True) As Tasks.Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineParamNameHintsTests)}.{NameOf(Me.VerifyParamHints)} creates asynchronous taggers")

                Dim tagProducer = New InlineParamNameHintsDataTaggerProvider(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    AsynchronousOperationListenerProvider.NullProvider,
                    workspace.GetService(Of IForegroundNotificationService))

                Dim hostDocument = workspace.Documents.Single(Function(d) d.CursorPosition.HasValue)
                ' Dim caretPosition = hostDocument.CursorPosition.Value
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim context = New TaggerContext(Of InlineParamNameHintDataTag)(
                    document, snapshot, New SnapshotPoint(snapshot, 0))
                Await tagProducer.GetTestAccessor().ProduceTagsAsync(context)

                Dim producedTags = From tag In context.tagSpans
                                   Order By tag.Span.Start
                                   Let spanName = tag.Tag.ParameterName
                                   Select spanName + ":" + tag.Span.Span.ToTextSpan().Start.ToString()

                Dim expectedTags As New List(Of String)

                For Each hostDocument In workspace.Documents
                    Dim nameAndSpansList = hostDocument.AnnotatedSpans.SelectMany(
                        Function(name) name.Value,
                        Function(name, span) _
                        New With {.Name = name.Key,
                                  .Span = span
                        })

                    For Each nameAndSpan In nameAndSpansList.OrderBy(Function(x) x.Span.Start)
                        expectedTags.Add(nameAndSpan.Name + ":" + nameAndSpan.Span.Start.ToString())
                    Next
                Next

                AssertEx.Equal(expectedTags, producedTags)

            End Using
        End Function
    End Class
End Namespace
