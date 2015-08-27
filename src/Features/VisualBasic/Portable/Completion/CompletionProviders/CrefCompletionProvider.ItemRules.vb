' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Partial Friend Class CrefCompletionProvider

        Friend Class ItemRules
            Inherits CompletionItemRules

            Public Shared ReadOnly Property Instance As New ItemRules()

            Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean?
                If ch = "("c AndAlso completionItem.DisplayText.IndexOf("("c) <> -1 Then
                    Return False
                End If

                If ch = " " Then
                    Dim textSoFar = textTypedSoFar.TrimEnd()
                    Return Not (textSoFar.Length >= 2 AndAlso Char.ToUpper(textSoFar(textSoFar.Length - 2)) = "O"c AndAlso Char.ToUpper(textSoFar(textSoFar.Length - 1)) = "F"c)
                End If

                Return MyBase.IsCommitCharacter(completionItem, ch, textTypedSoFar)
            End Function

            Public Overrides Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String, options As OptionSet) As Boolean?
                Return False
            End Function
        End Class

    End Class
End Namespace