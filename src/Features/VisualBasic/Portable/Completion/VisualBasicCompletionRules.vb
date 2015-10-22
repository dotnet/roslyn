' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion
    Partial Friend Class VisualBasicCompletionService
        Private NotInheritable Class VisualBasicCompletionRules
            Inherits CompletionRules

            Public Sub New(service As AbstractCompletionService)
                MyBase.New(service)
            End Sub

            Protected Overrides Function CompareMatches(leftMatch As PatternMatch, rightMatch As PatternMatch, leftItem As CompletionItem, rightItem As CompletionItem) As Integer
                Dim diff As Integer
                diff = PatternMatch.CompareType(leftMatch, rightMatch)
                If diff <> 0 Then
                    Return diff
                End If

                diff = PatternMatch.CompareCamelCase(leftMatch, rightMatch)
                If diff <> 0 Then
                    Return diff
                End If

                ' More important than the case sensitivity is that "left" isn't an argument name
                Dim leftIsNamedArgument = TypeOf leftItem.CompletionProvider Is NamedParameterCompletionProvider
                Dim rightIsNamedArgument = TypeOf rightItem.CompletionProvider Is NamedParameterCompletionProvider
                If leftIsNamedArgument AndAlso Not rightIsNamedArgument Then
                    Return 1
                End If
                If rightIsNamedArgument AndAlso Not leftIsNamedArgument Then
                    Return -1
                End If

                diff = PatternMatch.CompareCase(leftMatch, rightMatch)
                If diff <> 0 Then
                    Return diff
                End If

                diff = PatternMatch.ComparePunctuation(leftMatch, rightMatch)
                If diff <> 0 Then
                    Return diff
                End If

                Return 0
            End Function

            Public Overrides Function IsBetterFilterMatch(item1 As CompletionItem, item2 As CompletionItem, filterText As String, triggerInfo As CompletionTriggerInfo, filterReason As CompletionFilterReason) As Boolean
                If filterReason = CompletionFilterReason.BackspaceOrDelete Then
                    Dim prefixLength1 = GetPrefixLength(item1.FilterText, filterText)
                    Dim prefixLength2 = GetPrefixLength(item2.FilterText, filterText)

                    Return prefixLength1 > prefixLength2 OrElse ((item1.Preselect AndAlso Not item2.Preselect) AndAlso TypeOf item1.CompletionProvider IsNot EnumCompletionProvider)
                End If

                If TypeOf item2.CompletionProvider Is EnumCompletionProvider Then
                    Dim match1 = GetMatch(item1, filterText)
                    Dim match2 = GetMatch(item2, filterText)

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

                Return MyBase.IsBetterFilterMatch(item1, item2, filterText, triggerInfo, filterReason)
            End Function

            Private Function GetPrefixLength(text As String, pattern As String) As Integer
                Dim x As Integer = 0
                While x < text.Length AndAlso x < pattern.Length AndAlso Char.ToUpper(text(x)) = Char.ToUpper(pattern(x))
                    x += 1
                End While

                Return x
            End Function

            Public Overrides Function MatchesFilterText(item As CompletionItem, filterText As String, triggerInfo As CompletionTriggerInfo, filterReason As CompletionFilterReason) As Boolean
                ' If this is a session started on backspace, we use a much looser prefix match check
                ' to see if an item matches
                If filterReason = CompletionFilterReason.BackspaceOrDelete AndAlso triggerInfo.TriggerReason = CompletionTriggerReason.BackspaceOrDeleteCommand Then
                    Return GetPrefixLength(item.FilterText, filterText) > 0
                End If

                Return MyBase.MatchesFilterText(item, filterText, triggerInfo, filterReason)
            End Function

            Public Overrides Function ShouldSoftSelectItem(item As CompletionItem, filterText As String, triggerInfo As CompletionTriggerInfo) As Boolean
                ' VB has additional specialized logic for soft selecting an item in completion when the only filter text is "_"
                If (filterText.Length = 0 OrElse filterText = "_") Then
                    ' Object Creation hard selects even with no selected item
                    Return Not TypeOf item.CompletionProvider Is ObjectCreationCompletionProvider
                End If

                Return False
            End Function

            Public Overrides Function ItemsMatch(item1 As CompletionItem, item2 As CompletionItem) As Boolean
                Return MyBase.ItemsMatch(item1, item2) AndAlso MatchGlyph(item1, item2)
            End Function

            Private Function MatchGlyph(item1 As CompletionItem, item2 As CompletionItem) As Boolean
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
                    If GetTextChange(otherItem).NewText = keywordCompletionItem.DisplayText Then
                        Return True
                    End If

                    Return False
                End If

                Return (item1.Glyph IsNot Nothing AndAlso item2.Glyph IsNot Nothing) AndAlso
                       (item1.Glyph.Value = item2.Glyph.Value)
            End Function

            Protected Overrides Function SendEnterThroughToEditorCore(completionItem As CompletionItem, textTypedSoFar As String, options As OptionSet) As Boolean
                ' In VB we always send enter through to the editor.
                Return True
            End Function

        End Class
    End Class
End Namespace
