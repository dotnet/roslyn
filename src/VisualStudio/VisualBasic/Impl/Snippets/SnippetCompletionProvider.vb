' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    <ExportCompletionProviderMef1("SnippetCompletionProvider", LanguageNames.VisualBasic)>
    Partial Friend Class SnippetCompletionProvider
        Inherits LSPCompletionProvider
        Implements ICustomCommitCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides ReadOnly Property IsSnippetProvider As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
            Dim document = context.Document
            Dim position = context.Position
            Dim cancellationToken = context.CancellationToken

            Dim snippetInfoService = document.GetLanguageService(Of ISnippetInfoService)()

            If snippetInfoService Is Nothing Then
                Return
            End If

            Dim snippets = snippetInfoService.GetSnippetsIfAvailable()

            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
            Dim targetToken = leftToken.GetPreviousTokenIfTouchingWord(position)

            If syntaxTree.IsPossibleTupleContext(leftToken, position) Then
                Return
            End If

            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)()
            If IsInNonUserCode(syntaxTree, position, cancellationToken) Then
                Return
            End If

            context.IsExclusive = context.CompletionOptions.SnippetsBehavior = SnippetsRule.IncludeAfterTypingIdentifierQuestionTab
            context.AddItems(CreateCompletionItems(snippets))
        End Function

        Private Shared ReadOnly s_commitChars As Char() = {" "c, ";"c, "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c, ","c, ":"c, "+"c, "-"c, "*"c, "/"c, "\"c, "^"c, "<"c, ">"c, "'"c, "="c}
        Private Shared ReadOnly s_rules As CompletionItemRules = CompletionItemRules.Create(
            commitCharacterRules:=ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, s_commitChars)))

        Private Shared Function CreateCompletionItems(snippets As IEnumerable(Of SnippetInfo)) As IEnumerable(Of CompletionItem)

            Return snippets.Select(Function(s) CommonCompletionItem.Create(
                                       s.Shortcut,
                                       displayTextSuffix:="",
                                       description:=s.Description.ToSymbolDisplayParts(),
                                       glyph:=Glyph.Snippet,
                                       rules:=s_rules))
        End Function

        Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
            Return Char.IsLetterOrDigit(text(characterPosition)) AndAlso options.TriggerOnTypingLetters
        End Function

        Public Overrides ReadOnly Property TriggerCharacters As ImmutableHashSet(Of Char) = ImmutableHashSet(Of Char).Empty

        Public Sub Commit(completionItem As CompletionItem,
                          document As Document,
                          textView As ITextView,
                          subjectBuffer As ITextBuffer,
                          triggerSnapshot As ITextSnapshot,
                          commitChar As Char?) Implements ICustomCommitCompletionProvider.Commit
            Dim expansionClientFactory = document.Project.Services.SolutionServices.GetRequiredService(Of ISnippetExpansionClientFactory)()
            Dim snippetClient = expansionClientFactory.GetOrCreateSnippetExpansionClient(document, textView, subjectBuffer)

            Dim trackingSpan = triggerSnapshot.CreateTrackingSpan(completionItem.Span.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Dim currentSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)

            subjectBuffer.Replace(currentSpan, completionItem.DisplayText)

            Dim updatedSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)
            snippetClient.TryInsertExpansion(updatedSpan.Start, updatedSpan.Start + completionItem.DisplayText.Length, CancellationToken.None)
        End Sub
    End Class
End Namespace
