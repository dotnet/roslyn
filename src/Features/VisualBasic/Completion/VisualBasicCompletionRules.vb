' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Completion.Rules
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion
    Friend Class VisualBasicCompletionRules
        Implements ICompletionRules

        Private _baseRules As ICompletionRules
        Private ReadOnly patternMatcher As PatternMatcher = New PatternMatcher()

        Public Sub New(baseRules As ICompletionRules)
            _baseRules = baseRules
        End Sub

        Public Sub CompletionItemComitted(item As CompletionItem) Implements ICompletionRules.CompletionItemComitted
            _baseRules.CompletionItemComitted(item)
        End Sub

        Public Function IsBetterFilterMatch(item1 As CompletionItem, item2 As CompletionItem, filterText As String, triggerInfo As CompletionTriggerInfo, filterReason As CompletionFilterReason) As Boolean? Implements ICompletionRules.IsBetterFilterMatch
            If filterReason = CompletionFilterReason.BackspaceOrDelete Then
                Dim prefixLength1 = GetPrefixLength(item1.FilterText, filterText)
                Dim prefixLength2 = GetPrefixLength(item2.FilterText, filterText)

                Return prefixLength1 > prefixLength2 OrElse ((item1.Preselect AndAlso Not item2.Preselect) AndAlso TypeOf item1.CompletionProvider IsNot EnumCompletionProvider)
            End If

            If TypeOf item2.CompletionProvider Is EnumCompletionProvider Then
                Dim match1 = patternMatcher.MatchPatternFirstOrNullable(item1.FilterText, filterText)
                Dim match2 = patternMatcher.MatchPatternFirstOrNullable(item2.FilterText, filterText)

                If match1.HasValue AndAlso match2.HasValue Then
                    If match1.Value.Kind = PatternMatchKind.Prefix AndAlso match2.Value.Kind = PatternMatchKind.Substring Then
                        ' If an item from Enum completion is an equally good match apart from
                        ' being a substring rather than prefix match, take it.

                        If TypeOf item2.CompletionProvider Is EnumCompletionProvider AndAlso
                            match1.Value.CamelCaseWeight.GetValueOrDefault() = match2.Value.CamelCaseWeight.GetValueOrDefault() AndAlso
                            match1.Value.IsCaseSensitive = match2.Value.IsCaseSensitive Then
                            Return False
                        End If
                    End If
                End If
            End If

            Return _baseRules.IsBetterFilterMatch(item1, item2, filterText, triggerInfo, filterReason)
        End Function

        Private Function GetPrefixLength(text As String, pattern As String) As Integer
            Dim x As Integer = 0
            While x < text.Length AndAlso x < pattern.Length AndAlso Char.ToUpper(text(x)) = Char.ToUpper(pattern(x))
                x += 1
            End While

            Return x
        End Function

        Public Function MatchesFilterText(item As CompletionItem, filterText As String, triggerInfo As CompletionTriggerInfo, filterReason As CompletionFilterReason) As Boolean? Implements ICompletionRules.MatchesFilterText
            ' If this is a session started on backspace, we use a much looser prefix match check
            ' to see if an item matches
            If filterReason = CompletionFilterReason.BackspaceOrDelete AndAlso triggerInfo.TriggerReason = CompletionTriggerReason.BackspaceOrDeleteCommand Then
                Return GetPrefixLength(item.FilterText, filterText) > 0
            End If

            Return _baseRules.MatchesFilterText(item, filterText, triggerInfo, filterReason)
        End Function

        Public Function ShouldSoftSelectItem(item As CompletionItem, filterText As String, triggerInfo As CompletionTriggerInfo) As Boolean? Implements ICompletionRules.ShouldSoftSelectItem
            ' VB has additional specialized logic for soft selecting an item in completion when the only filter text is "_"
            If (filterText.Length = 0 OrElse filterText = "_") Then
                ' Object Creation hard selects even with no selected item
                Return Not TypeOf item.CompletionProvider Is ObjectCreationCompletionProvider
            End If

            Return False
        End Function

        Public Function ItemsMatch(item1 As CompletionItem, item2 As CompletionItem) As Boolean? Implements ICompletionRules.ItemsMatch
            Return _baseRules.ItemsMatch(item1, item2) AndAlso MatchGlyph(item1, item2)
        End Function

        Private Function MatchGlyph(item1 As CompletionItem, item2 As CompletionItem) As Boolean?
            ' DevDiv 957450: Normally, we want to show items with the same display text and
            ' different glyphs. That way, the we won't hide user-defined symbols that happen
            ' to match a keyword (like Select). However, we want to avoid showing the keyword
            ' for an intrinsic right next to the item for the corresponding symbol. 
            ' Therefore, if a keyword claims to represent an "intrinsic" item, we'll ignore
            ' the glyph when matching.
            Dim keywordCompletionItem = If(TryCast(item2, KeywordCompletionItem), TryCast(item1, KeywordCompletionItem))
            If keywordCompletionItem IsNot Nothing AndAlso
                keywordCompletionItem.IsIntrinsic Then

                Dim otherItem = If(keywordCompletionItem Is item1, item2, item1)
                If otherItem.CompletionProvider.GetTextChange(otherItem).NewText = keywordCompletionItem.DisplayText Then
                    Return True
                End If

                Return False
            End If

            Return item1.Glyph = item2.Glyph
        End Function
    End Class
End Namespace
