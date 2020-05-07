' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

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
    End Module
End Namespace
