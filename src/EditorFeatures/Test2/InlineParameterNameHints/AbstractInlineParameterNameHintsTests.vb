' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CSharp.InlineParameterNameHints
Imports Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.InlineParameterNameHints
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineParameterNameHints
    <[UseExportProvider]>
    Public MustInherit Class AbstractInlineParameterNameHintsTests

        Protected Async Function VerifyParamHints(test As XElement, Optional optionIsEnabled As Boolean = True) As Tasks.Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineParameterNameHintsTests)}.{NameOf(Me.VerifyParamHints)} creates asynchronous taggers")

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineParameterNameHintsService)
                Dim paramNameHintSpans = Await tagService.GetInlineParameterNameHintsAsync(document, New Text.TextSpan(0, snapshot.Length), New CancellationToken())

                Dim producedTags = From tag In paramNameHintSpans
                                   Select tag.Name + ":" + tag.Position.ToString

                Dim expectedTags As New List(Of String)

                Dim nameAndSpansList = hostDocument.AnnotatedSpans.SelectMany(
                    Function(name) name.Value,
                    Function(name, span) _
                    New With {.Name = name.Key,
                              .Span = span
                    })

                For Each nameAndSpan In nameAndSpansList.OrderBy(Function(x) x.Span.Start)
                    expectedTags.Add(nameAndSpan.Name + ":" + nameAndSpan.Span.Start.ToString())
                Next

                AssertEx.Equal(expectedTags, producedTags)

            End Using
        End Function
    End Class
End Namespace
