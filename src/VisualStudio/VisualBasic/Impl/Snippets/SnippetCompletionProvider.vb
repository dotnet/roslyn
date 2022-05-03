' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    <ExportCompletionProviderMef1("SnippetCompletionProvider", LanguageNames.VisualBasic)>
    Partial Friend Class SnippetCompletionProvider
        Inherits LSPCompletionProvider
        Implements ICustomCommitCompletionProvider

        Private ReadOnly _threadingContext As IThreadingContext
        Private ReadOnly _signatureHelpControllerProvider As SignatureHelpControllerProvider
        Private ReadOnly _editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory
        Private ReadOnly _editorAdaptersFactoryService As IVsEditorAdaptersFactoryService
        Private ReadOnly _argumentProviders As ImmutableArray(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata))
        Private ReadOnly _globalOptions As IGlobalOptionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            threadingContext As IThreadingContext,
            signatureHelpControllerProvider As SignatureHelpControllerProvider,
            editorCommandHandlerServiceFactory As IEditorCommandHandlerServiceFactory,
            editorAdaptersFactoryService As IVsEditorAdaptersFactoryService,
            <ImportMany> argumentProviders As IEnumerable(Of Lazy(Of ArgumentProvider, OrderableLanguageMetadata)),
            globalOptions As IGlobalOptionService)

            _threadingContext = threadingContext
            _signatureHelpControllerProvider = signatureHelpControllerProvider
            _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory
            _editorAdaptersFactoryService = editorAdaptersFactoryService
            _argumentProviders = argumentProviders.ToImmutableArray()
            _globalOptions = globalOptions
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

            Dim semanticModel = Await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(False)
            Dim syntaxContext = VisualBasicSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken)
            If syntaxContext.IsInTaskLikeTypeContext Then
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
                          textView As ITextView,
                          subjectBuffer As ITextBuffer,
                          triggerSnapshot As ITextSnapshot,
                          commitChar As Char?) Implements ICustomCommitCompletionProvider.Commit
            Dim snippetClient = SnippetExpansionClient.GetSnippetExpansionClient(
                _threadingContext,
                textView,
                subjectBuffer,
                _signatureHelpControllerProvider,
                _editorCommandHandlerServiceFactory,
                _editorAdaptersFactoryService,
                _argumentProviders,
                _globalOptions)

            Dim trackingSpan = triggerSnapshot.CreateTrackingSpan(completionItem.Span.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Dim currentSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)

            subjectBuffer.Replace(currentSpan, completionItem.DisplayText)

            Dim updatedSpan = trackingSpan.GetSpan(subjectBuffer.CurrentSnapshot)
            snippetClient.TryInsertExpansion(updatedSpan.Start, updatedSpan.Start + completionItem.DisplayText.Length, CancellationToken.None)
        End Sub
    End Class
End Namespace
