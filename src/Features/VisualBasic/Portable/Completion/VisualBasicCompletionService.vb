' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion
    Partial Friend Class VisualBasicCompletionService
        Inherits CommonCompletionService

        <ExportLanguageServiceFactory(GetType(CompletionService), LanguageNames.VisualBasic), [Shared]>
        Friend Class Factory
            Implements ILanguageServiceFactory

            Private ReadOnly _listenerProvider As IAsynchronousOperationListenerProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New(listenerProvider As IAsynchronousOperationListenerProvider)
                _listenerProvider = listenerProvider
            End Sub

            Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
                Return New VisualBasicCompletionService(languageServices.LanguageServices.SolutionServices, _listenerProvider)
            End Function
        End Class

        Private _latestRules As CompletionRules = CompletionRules.Create(
            dismissIfEmpty:=True,
            dismissIfLastCharacterDeleted:=True,
            defaultCommitCharacters:=CompletionRules.Default.DefaultCommitCharacters,
            defaultEnterKeyRule:=EnterKeyRule.Always)

        Private Sub New(services As SolutionServices, listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(services, listenerProvider)
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Function GetRules(options As CompletionOptions) As CompletionRules
            ' Although EnterKeyBehavior is a per-language setting, the meaning of an unset setting (Default) differs between C# And VB
            ' In VB the default means Always to maintain previous behavior
            Dim enterRule = options.EnterKeyBehavior
            Dim snippetsRule = options.SnippetsBehavior

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
            Dim provider = TryCast(GetProvider(item, project:=Nothing), CommonCompletionProvider)
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

        Friend Overrides Function SupportsTriggerOnDeletion(options As CompletionOptions) As Boolean
            ' If the option is null (i.e. default) or 'true', then we want to trigger completion.
            ' Only if the option is false do we not want to trigger.
            Return If(options.TriggerOnDeletion = False, False, True)
        End Function
    End Class
End Namespace
