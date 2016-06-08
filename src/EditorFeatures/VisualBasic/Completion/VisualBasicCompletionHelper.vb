Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion

    <ExportLanguageServiceFactory(GetType(CompletionHelperFactory), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCompletionHelperFactoryFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCompletionHelperFactory()
        End Function

        Private Class VisualBasicCompletionHelperFactory
            Inherits CompletionHelperFactory

            Public Sub New()
            End Sub

            Public Overrides Function CreateCompletionHelper(service As CompletionService) As CompletionHelper
                Return New VisualBasicCompletionHelper(service)
            End Function
        End Class
    End Class

    Friend Class VisualBasicCompletionHelper
        Inherits CompletionHelper

        Public Sub New(completionService As CompletionService)
            MyBase.New(completionService)
        End Sub

        Public Overrides ReadOnly Property QuestionTabInvokesSnippetCompletion As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function MatchesFilterText(item As CompletionItem, filterText As String, trigger As CompletionTrigger, filterReason As CompletionFilterReason, Optional recentItems As ImmutableArray(Of String) = Nothing) As Boolean
            ' If this Is a session started on backspace, we use a much looser prefix match check
            ' to see if an item matches

            If filterReason = CompletionFilterReason.BackspaceOrDelete AndAlso trigger.Kind = CompletionTriggerKind.Deletion Then
                Return GetPrefixLength(item.FilterText, filterText) > 0
            End If

            Return MyBase.MatchesFilterText(item, filterText, trigger, filterReason, recentItems)
        End Function

        Public Overrides Function IsBetterFilterMatch(item1 As CompletionItem, item2 As CompletionItem, filterText As String, trigger As CompletionTrigger, filterReason As CompletionFilterReason, Optional recentItems As ImmutableArray(Of String) = Nothing) As Boolean

            If filterReason = CompletionFilterReason.BackspaceOrDelete Then
                Dim prefixLength1 = GetPrefixLength(item1.FilterText, filterText)
                Dim prefixLength2 = GetPrefixLength(item2.FilterText, filterText)
                Return prefixLength1 > prefixLength2 OrElse ((item1.Rules.Preselect AndAlso Not item2.Rules.Preselect) AndAlso Not IsEnumMemberItem(item1))
            End If

            If IsEnumMemberItem(item2) Then
                Dim match1 = GetMatch(item1, filterText)
                Dim match2 = GetMatch(item2, filterText)

                If match1.HasValue AndAlso match2.HasValue Then
                    If match1.Value.Kind = PatternMatchKind.Prefix AndAlso match2.Value.Kind = PatternMatchKind.Substring Then
                        ' If an item from Enum completion Is an equally good match apart from
                        ' being a substring rather than prefix match, take it.

                        If IsEnumMemberItem(item1) AndAlso
                            match1.Value.CamelCaseWeight.GetValueOrDefault() = match2.Value.CamelCaseWeight.GetValueOrDefault() AndAlso
                            match1.Value.IsCaseSensitive = match2.Value.IsCaseSensitive Then
                            Return False
                        End If
                    End If
                End If
            End If

            Return MyBase.IsBetterFilterMatch(item1, item2, filterText, trigger, filterReason, recentItems)
        End Function

        Public Overrides Function ShouldSoftSelectItem(item As CompletionItem, filterText As String, trigger As CompletionTrigger) As Boolean

            ' VB has additional specialized logic for soft selecting an item in completion when the only filter text Is "_"
            If filterText.Length = 0 OrElse filterText = "_" Then
                ' Object Creation hard selects even with no selected item
                Return Not IsObjectCreationItem(item)
            End If

            Return MyBase.ShouldSoftSelectItem(item, filterText, trigger)
        End Function
    End Class

End Namespace