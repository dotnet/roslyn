' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class HandlesClauseCompletionProvider

        Private Class ItemRules
            Inherits AbstractSymbolCompletionItemRules

            Public Shared ReadOnly Property Instance As New ItemRules()

            Protected Overrides Function GetInsertionText(symbol As ISymbol, context As AbstractSyntaxContext, ch As Char) As String
                Return CompletionUtilities.GetInsertionTextAtInsertionTime(symbol, context, ch)
            End Function
        End Class

    End Class
End Namespace
