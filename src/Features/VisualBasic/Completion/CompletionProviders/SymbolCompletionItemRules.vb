' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class SymbolCompletionItemRules
        Inherits CompletionItemRules

        Public Shared ReadOnly Property Instance As New SymbolCompletionItemRules()

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Result(Of Boolean)
            Dim symbolItem = TryCast(completionItem, SymbolCompletionItem)
            If symbolItem IsNot Nothing AndAlso symbolItem.Context.IsInImportsDirective Then
                ' If the user is writing "Imports S" then the only commit character is <dot>
                ' as they might be typing an Imports alias.
                Return ch = "."c
            End If

            Return MyBase.IsCommitCharacter(completionItem, ch, textTypedSoFar)
        End Function

    End Class
End Namespace
