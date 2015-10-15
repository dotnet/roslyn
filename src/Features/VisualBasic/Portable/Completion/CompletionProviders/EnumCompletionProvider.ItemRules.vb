' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class EnumCompletionProvider

        Private Class ItemRules
            Inherits AbstractSymbolCompletionItemRules

            Public Shared ReadOnly Property Instance As New ItemRules()

            Public Overrides Function GetTextChange(selectedItem As CompletionItem, Optional ch As Char? = Nothing, Optional textTypedSoFar As String = Nothing) As TextChange?
                Return New TextChange(selectedItem.FilterSpan, DirectCast(selectedItem, SymbolCompletionItem).InsertionText)
            End Function

            Protected Overrides Function GetInsertionText(symbol As ISymbol, context As AbstractSyntaxContext, ch As Char) As String
                Return CompletionUtilities.GetInsertionTextAtInsertionTime(symbol, context, ch)
            End Function
        End Class

    End Class
End Namespace
