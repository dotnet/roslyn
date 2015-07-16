' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class ObjectCreationCompletionItemRules
        Inherits CompletionItemRules

        Public Shared ReadOnly Property Instance As New ObjectCreationCompletionItemRules()

        Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Result(Of Boolean)
            Return ch = " "c OrElse ch = "("c
        End Function

    End Class
End Namespace
