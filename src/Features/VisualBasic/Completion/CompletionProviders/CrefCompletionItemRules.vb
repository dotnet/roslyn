' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    Friend Class CrefCompletionItemRules
        Inherits CompletionItemRules

        Public Shared ReadOnly Property Instance As CrefCompletionItemRules = New CrefCompletionItemRules()

        Public Overrides Function SendEnterThroughToEditor(completionItem As CompletionItem, textTypedSoFar As String, options As OptionSet) As Result(Of Boolean)
            Return False
        End Function
    End Class
End Namespace