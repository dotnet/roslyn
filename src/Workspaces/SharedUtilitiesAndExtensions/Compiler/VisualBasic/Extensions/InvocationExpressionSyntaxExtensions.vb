' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module InvocationExpressionExtensions

        <Extension>
        Public Function CanRemoveEmptyArgumentList(invocationExpression As InvocationExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Return invocationExpression.ArgumentList IsNot Nothing AndAlso invocationExpression.ArgumentList.Arguments.Count = 0 AndAlso
                CanHaveOmittedArgumentList(invocationExpression, semanticModel)
        End Function

        Private Function CanHaveOmittedArgumentList(invocationExpression As InvocationExpressionSyntax, semanticModel As SemanticModel) As Boolean
            Dim nextToken = invocationExpression.GetLastToken().GetNextToken()

            If nextToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken) Then
                Return False
            End If

            ' If the inner expression ends in a paren that is an argument list,
            ' we won't remove the outer parens. For example: x()()
            Dim lastExpressionToken = invocationExpression.Expression.GetLastToken()
            If lastExpressionToken.IsKindOrHasMatchingText(SyntaxKind.CloseParenToken) AndAlso
               lastExpressionToken.Parent.IsKind(SyntaxKind.ArgumentList) Then
                Return False
            End If

            ' Check to see if the invocation would become a label if the argument
            ' list is removed, e.g. x() : Console.WriteLine()
            If TypeOf invocationExpression.Expression Is IdentifierNameSyntax Then
                Dim nextTrivia = invocationExpression _
                    .GetTrailingTrivia() _
                    .SkipWhile(Function(t) t.IsKind(SyntaxKind.WhitespaceTrivia)) _
                    .FirstOrDefault()

                If nextTrivia.IsKind(SyntaxKind.ColonTrivia) AndAlso invocationExpression.GetFirstToken().IsFirstTokenOnLine() Then
                    Return False
                End If
            End If

            If invocationExpression.IsParentKind(SyntaxKind.CallStatement) OrElse invocationExpression.IsParentKind(SyntaxKind.ExpressionStatement) Then
                Return True
            End If

            Dim symbol As ISymbol = semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol
            Return symbol IsNot Nothing AndAlso symbol.MatchesKind(SymbolKind.Property, SymbolKind.Method) AndAlso Not symbol.IsAnonymousFunction
        End Function

        <Extension>
        Public Function GetExpression(invocationExpression As InvocationExpressionSyntax) As ExpressionSyntax
            If invocationExpression.Expression IsNot Nothing Then
                Return invocationExpression.Expression
            End If

            If invocationExpression.IsParentKind(SyntaxKind.ConditionalAccessExpression) Then
                Return DirectCast(invocationExpression.Parent, ConditionalAccessExpressionSyntax).Expression
            End If

            Return Nothing
        End Function

    End Module
End Namespace
