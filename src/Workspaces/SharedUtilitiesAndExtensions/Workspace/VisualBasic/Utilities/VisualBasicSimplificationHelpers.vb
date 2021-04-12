' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Friend Module VisualBasicSimplificationHelpers
        Public Function TryEscapeIdentifierToken(identifierToken As SyntaxToken) As SyntaxToken
            If identifierToken.Kind <> SyntaxKind.IdentifierToken OrElse identifierToken.ValueText.Length = 0 Then
                Return identifierToken
            End If

            If identifierToken.IsBracketed Then
                Return identifierToken
            End If

            If identifierToken.GetTypeCharacter() <> TypeCharacter.None Then
                Return identifierToken
            End If

            Dim unescapedIdentifier = identifierToken.ValueText
            If SyntaxFacts.GetKeywordKind(unescapedIdentifier) = SyntaxKind.None AndAlso SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier) = SyntaxKind.None Then
                Return identifierToken
            End If

            Return identifierToken.CopyAnnotationsTo(
                        SyntaxFactory.BracketedIdentifier(identifierToken.LeadingTrivia, identifierToken.ValueText, identifierToken.TrailingTrivia) _
                            .WithAdditionalAnnotations(Simplifier.Annotation))
        End Function
    End Module
End Namespace
