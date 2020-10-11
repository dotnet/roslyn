' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.InlineHints

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <[UseExportProvider]>
    Public MustInherit Class AbstractInlineParameterNameHintsTests

        Protected Async Function VerifyParamHints(test As XElement, Optional optionIsEnabled As Boolean = True) As Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineParameterNameHintsTests)}.{NameOf(Me.VerifyParamHints)} creates asynchronous taggers")

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options.WithChangedOption(
                    InlineHintsOptions.EnabledForParameters,
                    workspace.CurrentSolution.Projects().First().Language,
                    optionIsEnabled)))

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineParameterNameHintsService)
                Dim paramNameHintSpans = Await tagService.GetInlineParameterNameHintsAsync(document, New Text.TextSpan(0, snapshot.Length), New CancellationToken())

                Dim producedTags = From tag In paramNameHintSpans
                                   Select tag.Parameter.Name + ":" + tag.Position.ToString

                Dim expectedTags As New List(Of String)

                Dim nameAndSpansList = hostDocument.AnnotatedSpans.SelectMany(
                    Function(name) name.Value,
                    Function(name, span) New With {.Name = name.Key, span})

                For Each nameAndSpan In nameAndSpansList.OrderBy(Function(x) x.Span.Start)
                    expectedTags.Add(nameAndSpan.Name + ":" + nameAndSpan.Span.Start.ToString())
                Next

                AssertEx.Equal(expectedTags, producedTags)
            End Using
        End Function
    End Class
End Namespace
