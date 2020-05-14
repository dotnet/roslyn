' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module MemberAccessExpressionSyntaxExtensions
        <Extension()>
        Public Function GetNameWithTriviaMoved(memberAccess As MemberAccessExpressionSyntax) As SimpleNameSyntax
            Dim replacementNode = memberAccess.Name
            replacementNode = DirectCast(replacementNode, SimpleNameSyntax) _
                .WithIdentifier(TryEscapeIdentifierToken(memberAccess.Name.Identifier)) _
                .WithLeadingTrivia(GetLeadingTriviaForSimplifiedMemberAccess(memberAccess)) _
                .WithTrailingTrivia(memberAccess.GetTrailingTrivia())

            Return replacementNode
        End Function

        Private Function GetLeadingTriviaForSimplifiedMemberAccess(memberAccess As MemberAccessExpressionSyntax) As SyntaxTriviaList
            ' We want to include any user-typed trivia that may be present between the 'Expression', 'OperatorToken' and 'Identifier' of the MemberAccessExpression.
            ' However, we don't want to include any elastic trivia that may have been introduced by the expander in these locations. This is to avoid triggering
            ' aggressive formatting. Otherwise, formatter will see this elastic trivia added by the expander And use that as a cue to introduce unnecessary blank lines
            ' etc. around the user's original code.
            Return SyntaxFactory.TriviaList(WithoutElasticTrivia(
                memberAccess.GetLeadingTrivia().
                    AddRange(memberAccess.Expression.GetTrailingTrivia()).
                    AddRange(memberAccess.OperatorToken.LeadingTrivia).
                    AddRange(memberAccess.OperatorToken.TrailingTrivia).
                    AddRange(memberAccess.Name.GetLeadingTrivia())))
        End Function

        Private Function WithoutElasticTrivia(list As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
            Return list.Where(Function(t) Not t.IsElastic())
        End Function
    End Module
End Namespace
