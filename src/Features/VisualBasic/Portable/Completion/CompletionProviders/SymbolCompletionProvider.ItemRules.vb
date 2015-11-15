' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class SymbolCompletionProvider

        Private Class ItemRules
            Inherits AbstractSymbolCompletionItemRules

            Public Shared ReadOnly Property Instance As New ItemRules()

            Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean?
                Dim symbolItem = TryCast(completionItem, SymbolCompletionItem)
                If symbolItem IsNot Nothing AndAlso symbolItem.Context.IsInImportsDirective Then
                    ' If the user is writing "Imports S" then the only commit character is <dot>
                    ' as they might be typing an Imports alias.
                    Return ch = "."c
                End If

                Return MyBase.IsCommitCharacter(completionItem, ch, textTypedSoFar)
            End Function

            Protected Overrides Function GetInsertionText(symbol As ISymbol, context As AbstractSyntaxContext, ch As Char) As String
                Return CompletionUtilities.GetInsertionTextAtInsertionTime(symbol, context, ch)
            End Function
        End Class

    End Class
End Namespace
