' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class NamedParameterCompletionProvider

        Private Class ItemRules
            Inherits CompletionItemRules

            Public Shared ReadOnly Property Instance As New ItemRules()

            Public Overrides Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange?
                Dim symbolItem = DirectCast(selectedItem, SymbolCompletionItem)
                If ch.HasValue AndAlso ch.Value = ":"c Then
                    Return New TextChange(symbolItem.FilterSpan,
                                          symbolItem.InsertionText.Substring(0, symbolItem.InsertionText.Length - s_colonEquals.Length))
                ElseIf ch.HasValue AndAlso ch.Value = "="c Then
                    Return New TextChange(selectedItem.FilterSpan,
                                          symbolItem.InsertionText.Substring(0, symbolItem.InsertionText.Length - (s_colonEquals.Length - 1)))
                Else
                    Return New TextChange(symbolItem.FilterSpan, symbolItem.InsertionText)
                End If
            End Function

        End Class

    End Class
End Namespace