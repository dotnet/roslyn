' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Shared.Extensions

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
    Partial Friend Class SnippetCompletionProvider

        Private Class ItemRules
            Inherits CompletionItemRules

            Private Shared ReadOnly s_commitChars As Char() = {" "c, ";"c, "("c, ")"c, "["c, "]"c, "{"c, "}"c, "."c, ","c, ":"c, "+"c, "-"c, "*"c, "/"c, "\"c, "^"c, "<"c, ">"c, "'"c, "="c}

            Public Shared ReadOnly Property Instance As ItemRules = New ItemRules()

            Public Overrides Function IsCommitCharacter(completionItem As CompletionItem, ch As Char, textTypedSoFar As String) As Boolean?
                Return s_commitChars.Contains(ch)
            End Function

        End Class

    End Class
End Namespace
