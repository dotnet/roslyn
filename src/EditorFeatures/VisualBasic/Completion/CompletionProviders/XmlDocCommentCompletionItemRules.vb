' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.CompletionProviders.XmlDocCommentCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Completion.CompletionProviders
    Friend Class XmlDocCommentCompletionItemRules
        Inherits AbstractXmlDocCommentCompletionItemRules

        Public Shared ReadOnly Property Instance As New XmlDocCommentCompletionItemRules

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean?
            If (ch = """"c OrElse ch = " "c) AndAlso
                completionItem.DisplayText.Contains(ch) Then
                Return False
            End If

            Return MyBase.IsCommitCharacter(completionItem, ch, textTypedSoFar)
        End Function

    End Class
End Namespace
