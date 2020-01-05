' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    <ExportCompletionProviderMef1("SnippetCompletionProvider", LanguageNames.VisualBasic)>
    Partial Friend Class SnippetCompletionProvider
        Inherits CommonCompletionProvider
        Implements ICustomCommitCompletionProvider

        Private ReadOnly _threadingContext As IThreadingContext
        Private ReadOnly _editorAdaptersFactoryService As IVsEditorAdaptersFactoryService

        <ImportingConstructor>
        Public Sub New(threadingContext As IThreadingContext, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService)
            _threadingContext = threadingContext
            Me._editorAdaptersFactoryService = editorAdaptersFactoryService
        End Sub

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
            Dim syntaxFacts = document.GetLanguageService(Of ISyntaxFactsService)()
            Dim isPossibleTupleContext = syntaxFacts.IsPossibleTupleContext(syntaxTree, position, cancellationToken)

            If (IsInNonUserCode(syntaxTree, position, cancellationToken)) Then
                Return
            End If

            context.IsExclusive = ShouldBeExclusive(context.Options)
            context.AddItems(CreateCompletionItems(snippets, isPossibleTupleContext))
        End Function

        Private Function ShouldBeExclusive(options As OptionSet) As Boolean
            Return options.GetOption(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic) = SnippetsRule.IncludeAfterTypingIdentifierQuestionTab
        End Function

        Private Shared ReadOnly s_commitChars As Char() = {" "c, ";"c, "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c, ","c, ":"c, "+"c, "-"c, "*"c, "/"c, "\"c, "^"c, "<"c, ">"c, "'"c, "="c}
        Private Shared ReadOnly s_rules As CompletionItemRules = CompletionItemRules.Create(
            commitCharacterRules:=ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, s_commitChars)))

        Private Shared ReadOnly s_tupleRules As CompletionItemRules = s_rules.
            WithCommitCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ":"c))

        Private Function CreateCompletionItems(snippets As IEnumerable(Of SnippetInfo), isTupleContext As Boolean) As IEnumerable(Of CompletionItem)

            Return snippets.Select(Function(s) CommonCompletionItem.Create(
                                       s.Shortcut,
                                       displayTextSuffix:="",
                                       description:=s.Description.ToSymbolDisplayParts(),
                                       glyph:=Glyph.Snippet,
                                       rules:=If(isTupleContext, s_tupleRules, s_rules)))
        End Function

        Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
            Return Char.IsLetterOrDigit(text(characterPosition)) AndAlso
                options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.VisualBasic)
        End Function

        Public Sub Commit(completionItem As CompletionItem,
                          textView As ITextView,
                          subjectBuffer As ITextBuffer,
                          triggerSnapshot As ITextSnapshot,
                          commitChar As Char?) Implements ICustomCommitCompletionProvider.Commit
            Dim snippetClient = SnippetExpansionClient.GetSnippetExpansionClient(_threadingContext, textView, subjectBuffer, _editorAdaptersFactoryService)

            Dim trackingSpan = triggerSnapshot.CreateTrackingSpan(completionItem.Span.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Dim currentSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)

            subjectBuffer.Replace(currentSpan, completionItem.DisplayText)

            Dim updatedSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)
            snippetClient.TryInsertExpansion(updatedSpan.Start, updatedSpan.Start + completionItem.DisplayText.Length)
        End Sub
    End Class
End Namespace
