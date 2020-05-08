' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function IdentifierName(text As String) As IdentifierNameSyntax
            Return SyntaxFactory.IdentifierName(Identifier(text))
        End Function

        Private Function Identifier(text As String) As SyntaxToken
            Return If(SyntaxFacts.GetKeywordKind(text) <> SyntaxKind.None OrElse SyntaxFacts.GetContextualKeywordKind(text) <> SyntaxKind.None,
                      SyntaxFactory.Identifier($"[{text}]"),
                      SyntaxFactory.Identifier(text))
        End Function

        Private Function ModifiedIdentifier(text As String) As ModifiedIdentifierSyntax
            Return SyntaxFactory.ModifiedIdentifier(Identifier(text))
        End Function
    End Module
End Namespace
