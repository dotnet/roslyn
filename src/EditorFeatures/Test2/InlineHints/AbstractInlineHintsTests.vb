﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.InlineHints

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <[UseExportProvider]>
    Public MustInherit Class AbstractInlineHintsTests
        Protected Async Function VerifyParamHints(test As XElement, Optional optionIsEnabled As Boolean = True) As Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineHintsTests)}.{NameOf(Me.VerifyParamHints)} creates asynchronous taggers")

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options.WithChangedOption(
                    InlineHintsOptions.EnabledForParameters,
                    workspace.CurrentSolution.Projects().First().Language,
                    optionIsEnabled)))

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineParameterNameHintsService)
                Dim inlineHints = Await tagService.GetInlineHintsAsync(document, New Text.TextSpan(0, snapshot.Length), New CancellationToken())

                Dim producedTags = From hint In inlineHints
                                   Select hint.DisplayParts.GetFullText() + hint.Span.ToString

                ValidateSpans(hostDocument, producedTags)
            End Using
        End Function

        Private Shared Sub ValidateSpans(hostDocument As TestHostDocument, producedTags As IEnumerable(Of String))
            Dim expectedTags As New List(Of String)

            Dim nameAndSpansList = hostDocument.AnnotatedSpans.SelectMany(
                Function(name) name.Value,
                Function(name, span) New With {.Name = name.Key, span})

            For Each nameAndSpan In nameAndSpansList.OrderBy(Function(x) x.span.Start)
                expectedTags.Add(nameAndSpan.Name + ":" + nameAndSpan.span.ToString())
            Next

            AssertEx.Equal(expectedTags, producedTags)
        End Sub

        Protected Async Function VerifyTypeHints(test As XElement, Optional optionIsEnabled As Boolean = True) As Task
            Using workspace = TestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineHintsTests)}.{NameOf(Me.VerifyTypeHints)} creates asynchronous taggers")

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options.WithChangedOption(
                    InlineHintsOptions.EnabledForTypes,
                    workspace.CurrentSolution.Projects().First().Language,
                    optionIsEnabled)))

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineTypeHintsService)
                Dim typeHints = Await tagService.GetInlineHintsAsync(document, New Text.TextSpan(0, snapshot.Length), New CancellationToken())

                Dim producedTags = From hint In typeHints
                                   Select hint.DisplayParts.GetFullText() + ":" + hint.Span.ToString()

                ValidateSpans(hostDocument, producedTags)
            End Using
        End Function
    End Class
End Namespace
