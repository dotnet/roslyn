' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.InlineHints
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.InlineHints
    <[UseExportProvider]>
    Public MustInherit Class AbstractInlineHintsTests
        Protected Async Function VerifyParamHints(test As XElement, output As XElement, Optional optionIsEnabled As Boolean = True) As Task
            Using workspace = EditorTestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineHintsTests)}.{NameOf(Me.VerifyParamHints)} creates asynchronous taggers")

                Dim options = New InlineParameterHintsOptions() With
                {
                    .EnabledForParameters = optionIsEnabled
                }

                Dim displayOptions = New SymbolDescriptionOptions()

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineParameterNameHintsService)

                Dim span = If(hostDocument.SelectedSpans.Any(), hostDocument.SelectedSpans.Single(), New TextSpan(0, snapshot.Length))
                Dim inlineHints = Await tagService.GetInlineHintsAsync(
                    document, span, options, displayOptions, displayAllOverride:=False, CancellationToken.None)

                Dim producedTags = From hint In inlineHints
                                   Select hint.DisplayParts.GetFullText().TrimEnd() + hint.Span.ToString

                ValidateSpans(hostDocument, producedTags)

                Dim outWorkspace = EditorTestWorkspace.Create(output)
                Dim expectedDocument = outWorkspace.CurrentSolution.GetDocument(outWorkspace.Documents.Single().Id)
                Await ValidateDoubleClick(document, expectedDocument, inlineHints)
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

        Private Shared Async Function ValidateDoubleClick(document As Document, expectedDocument As Document, inlineHints As ImmutableArray(Of InlineHint)) As Task
            Dim textChanges = New List(Of TextChange)
            For Each inlineHint In inlineHints
                If inlineHint.ReplacementTextChange IsNot Nothing Then
                    textChanges.Add(inlineHint.ReplacementTextChange.Value)
                End If
            Next

            Dim value = Await document.GetTextAsync().ConfigureAwait(False)
            Dim newText = value.WithChanges(textChanges).ToString()
            Dim expectedText = Await expectedDocument.GetTextAsync().ConfigureAwait(False)

            AssertEx.Equal(expectedText.ToString(), newText)
        End Function

        Protected Async Function VerifyTypeHints(test As XElement, output As XElement, Optional optionIsEnabled As Boolean = True, Optional ephemeral As Boolean = False) As Task
            Using workspace = EditorTestWorkspace.Create(test)
                WpfTestRunner.RequireWpfFact($"{NameOf(AbstractInlineHintsTests)}.{NameOf(Me.VerifyTypeHints)} creates asynchronous taggers")

                Dim options = New InlineTypeHintsOptions() With
                {
                    .EnabledForTypes = optionIsEnabled AndAlso Not ephemeral
                }

                Dim displayOptions = New SymbolDescriptionOptions()

                Dim hostDocument = workspace.Documents.Single()
                Dim snapshot = hostDocument.GetTextBuffer().CurrentSnapshot
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim tagService = document.GetRequiredLanguageService(Of IInlineTypeHintsService)

                Dim span = If(hostDocument.SelectedSpans.Any(), hostDocument.SelectedSpans.Single(), New TextSpan(0, snapshot.Length))
                Dim typeHints = Await tagService.GetInlineHintsAsync(
                    document, span, options, displayOptions, displayAllOverride:=ephemeral, CancellationToken.None)

                Dim producedTags = From hint In typeHints
                                   Select hint.DisplayParts.GetFullText() + ":" + hint.Span.ToString()

                ValidateSpans(hostDocument, producedTags)

                Dim outWorkspace = EditorTestWorkspace.Create(output)
                Dim expectedDocument = outWorkspace.CurrentSolution.GetDocument(outWorkspace.Documents.Single().Id)
                Await ValidateDoubleClick(document, expectedDocument, typeHints)
            End Using
        End Function
    End Class
End Namespace
