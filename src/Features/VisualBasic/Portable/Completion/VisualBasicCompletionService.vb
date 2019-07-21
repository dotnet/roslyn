' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.SuggestionMode

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion
    <ExportLanguageServiceFactory(GetType(CompletionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCompletionServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCompletionService(languageServices.WorkspaceServices.Workspace)
        End Function
    End Class

    Partial Friend Class VisualBasicCompletionService
        Inherits CommonCompletionService

        Private ReadOnly _workspace As Workspace
        Private ReadOnly _completionProviders As ImmutableArray(Of CompletionProvider)

        Public Sub New(workspace As Workspace,
                       Optional exclusiveProviders As ImmutableArray(Of CompletionProvider)? = Nothing)
            MyBase.New(workspace, exclusiveProviders)
            _workspace = workspace

            Dim completionProviders = ImmutableArray.Create(Of CompletionProvider)(
                New KeywordCompletionProvider(),
                New SymbolCompletionProvider(),
                New ObjectInitializerCompletionProvider(),
                New ObjectCreationCompletionProvider(),
                New EnumCompletionProvider(),
                New NamedParameterCompletionProvider(),
                New VisualBasicSuggestionModeCompletionProvider(),
                New ImplementsClauseCompletionProvider(),
                New HandlesClauseCompletionProvider(),
                New PartialTypeCompletionProvider(),
                New CrefCompletionProvider(),
                New CompletionListTagCompletionProvider(),
                New OverrideCompletionProvider(),
                New XmlDocCommentCompletionProvider(),
                New InternalsVisibleToCompletionProvider())

            Dim languageServices = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic)
            Dim languagesProvider = languageServices.GetService(Of IEmbeddedLanguagesProvider)()
            If languagesProvider IsNot Nothing Then
                completionProviders = completionProviders.Add(New EmbeddedLanguageCompletionProvider(languagesProvider))
            End If

            completionProviders = completionProviders.Add(New TypeImportCompletionProvider())

            _completionProviders = completionProviders
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Private _latestRules As CompletionRules = CompletionRules.Create(
            dismissIfEmpty:=True,
            dismissIfLastCharacterDeleted:=True,
            defaultCommitCharacters:=CompletionRules.Default.DefaultCommitCharacters,
            defaultEnterKeyRule:=EnterKeyRule.Always)

        Public Overrides Function GetRules() As CompletionRules
            Dim options = _workspace.Options

            ' Although EnterKeyBehavior is a per-language setting, the meaning of an unset setting (Default) differs between C# And VB
            ' In VB the default means Always to maintain previous behavior
            Dim enterRule = options.GetOption(CompletionOptions.EnterKeyBehavior, LanguageNames.VisualBasic)
            Dim snippetsRule = options.GetOption(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic)

            If enterRule = EnterKeyRule.Default Then
                enterRule = EnterKeyRule.Always
            End If

            If snippetsRule = SnippetsRule.Default Then
                snippetsRule = SnippetsRule.IncludeAfterTypingIdentifierQuestionTab
            End If

            Dim newRules = _latestRules.WithDefaultEnterKeyRule(enterRule).
                                        WithSnippetsRule(snippetsRule)

            Interlocked.Exchange(_latestRules, newRules)

            Return newRules
        End Function


        Protected Overrides Function GetBuiltInProviders() As ImmutableArray(Of CompletionProvider)
            Return _completionProviders
        End Function

        Protected Overrides Function GetBetterItem(item As CompletionItem, existingItem As CompletionItem) As CompletionItem
            ' If one Is a keyword, And the other Is some other item that inserts the same text as the keyword,
            ' keep the keyword (VB only), unless the other item is preselected
            If IsKeywordItem(existingItem) AndAlso existingItem.Rules.MatchPriority >= item.Rules.MatchPriority Then
                Return existingItem
            End If

            Return MyBase.GetBetterItem(item, existingItem)
        End Function

        Protected Overrides Function ItemsMatch(item As CompletionItem, existingItem As CompletionItem) As Boolean
            If Not MyBase.ItemsMatch(item, existingItem) Then
                Return False
            End If

            ' DevDiv 957450 Normally, we want to show items with the same display text And
            ' different glyphs. That way, the we won't hide user - defined symbols that happen
            ' to match a keyword (Like Select). However, we want to avoid showing the keyword
            ' for an intrinsic right next to the item for the corresponding symbol. 
            ' Therefore, if a keyword claims to represent an "intrinsic" item, we'll ignore
            ' the glyph when matching.

            Dim keywordCompletionItem = If(IsKeywordItem(existingItem), existingItem, If(IsKeywordItem(item), item, Nothing))
            If keywordCompletionItem IsNot Nothing AndAlso keywordCompletionItem.Tags.Contains(WellKnownTags.Intrinsic) Then
                Dim otherItem = If(keywordCompletionItem Is item, existingItem, item)
                Dim changeText = GetChangeText(otherItem)
                If changeText = keywordCompletionItem.DisplayText Then
                    Return True
                Else
                    Return False
                End If
            End If

            Return item.Tags = existingItem.Tags OrElse Enumerable.SequenceEqual(item.Tags, existingItem.Tags)
        End Function

        Private Function GetChangeText(item As CompletionItem) As String
            Dim provider = TryCast(GetProvider(item), CommonCompletionProvider)
            If provider IsNot Nothing Then
                ' TODO: Document Is Not available in this code path.. what about providers that need to reconstruct information before producing text?
                Dim result = provider.GetTextChangeAsync(Nothing, item, Nothing, CancellationToken.None).Result
                If result IsNot Nothing Then
                    Return result.Value.NewText
                End If
            End If

            Return item.DisplayText
        End Function

        Public Overrides Function GetDefaultCompletionListSpan(text As SourceText, caretPosition As Integer) As TextSpan
            Return CompletionUtilities.GetCompletionItemSpan(text, caretPosition)
        End Function

        Friend Overrides Function SupportsTriggerOnDeletion(options As OptionSet) As Boolean
            ' If the option is null (i.e. default) or 'true', then we want to trigger completion.
            ' Only if the option is false do we not want to trigger.
            Dim opt = options.GetOption(CompletionOptions.TriggerOnDeletion, Me.Language)
            Return If(opt = False, False, True)
        End Function
    End Class
End Namespace
