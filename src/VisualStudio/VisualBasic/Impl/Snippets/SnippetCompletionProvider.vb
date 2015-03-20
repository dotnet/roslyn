' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Extensibility.Completion
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Options
Imports System.ComponentModel.Composition

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    <ExportCompletionProvider("SnippetCompletionProvider", LanguageNames.VisualBasic)>
    Friend Class SnippetCompletionProvider
        Inherits AbstractCompletionProvider
        Implements ISnippetCompletionProvider

        Private ReadOnly _editorAdaptersFactoryService As IVsEditorAdaptersFactoryService

        <ImportingConstructor>
        Public Sub New(editorAdaptersFactoryService As IVsEditorAdaptersFactoryService)
            Me._editorAdaptersFactoryService = editorAdaptersFactoryService
        End Sub

        Protected Overrides Function GetItemsWorkerAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of CompletionItem))
            Dim snippetInfoService = document.GetLanguageService(Of ISnippetInfoService)()

            If snippetInfoService Is Nothing Then
                Return SpecializedTasks.EmptyEnumerable(Of CompletionItem)()
            End If

            Dim snippets = snippetInfoService.GetSnippetsIfAvailable()

            Dim textChangeSpan = CommonCompletionUtilities.GetTextChangeSpan(
                document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken),
                position,
                AddressOf Char.IsLetterOrDigit,
                AddressOf Char.IsLetterOrDigit)

            Return Task.FromResult(CreateCompletionItems(snippets, textChangeSpan))
        End Function

        Private Function CreateCompletionItems(snippets As IEnumerable(Of SnippetInfo), span As TextSpan) As IEnumerable(Of CompletionItem)

            Return snippets.Select(Function(s) New CompletionItem(Me,
                                                                  s.Shortcut,
                                                                  span,
                                                                  description:=s.Description.ToSymbolDisplayParts(),
                                                                  glyph:=Glyph.Snippet))
        End Function

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean
            Dim commitChars = {" "c, ";"c, "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c, ","c, ":"c, "+"c, "-"c, "*"c, "/"c, "\"c, "^"c, "<"c, ">"c, "'"c, "="c}

            Return commitChars.Contains(ch)
        End Function

        Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return Char.IsLetterOrDigit(text(characterPosition)) AndAlso
                options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.VisualBasic)
        End Function

        Public Overrides Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String) As Boolean
            Return True
        End Function

        Protected Overrides Function IsExclusiveAsync(document As Document, position As Integer, triggerInfo As CompletionTriggerInfo, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return SpecializedTasks.True
        End Function

        Public Sub Commit(completionItem As CompletionItem, textView As ITextView, subjectBuffer As ITextBuffer, triggerSnapshot As ITextSnapshot, commitChar As Char?) Implements ICustomCommitCompletionProvider.Commit
            Dim snippetClient = SnippetExpansionClient.GetSnippetExpansionClient(textView, subjectBuffer, _editorAdaptersFactoryService)

            Dim caretPoint = textView.GetCaretPoint(subjectBuffer)

            Dim trackingSpan = triggerSnapshot.CreateTrackingSpan(completionItem.FilterSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Dim currentSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)

            subjectBuffer.Replace(currentSpan, completionItem.DisplayText)

            Dim updatedSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)
            snippetClient.TryInsertExpansion(updatedSpan.Start, updatedSpan.Start + completionItem.DisplayText.Length)
        End Sub
    End Class
End Namespace
