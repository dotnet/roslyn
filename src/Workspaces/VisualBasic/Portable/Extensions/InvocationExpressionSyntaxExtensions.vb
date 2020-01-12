' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module InvocationExpressionExtensions

        <Extension>
        Public Function CanRemoveEmptyArgumentList(invocationExpression As InvocationExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            If invocationExpression.ArgumentList Is Nothing Then
                Return False
            End If

            If invocationExpression.ArgumentList.Arguments.Count > 0 Then
                Return False
            End If

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

                If nextTrivia.IsKind(SyntaxKind.ColonTrivia) AndAlso invocationExpression.GetFirstToken().IsFirstTokenOnLine(cancellationToken) Then
                    Return False
                End If
            End If

            Dim ancestor = invocationExpression.GetAncestor(Of ExecutableStatementSyntax)
            If ancestor Is Nothing Then Return True ' TODO: Cover cases other than executable statement

            ' Drawbacks:
            ' * Only covers cases where the invocation is nested within an executable statement
            ' * Performs abysmally due to creating a whole speculative semantic model for the vast majority of invocations, and additionally using GetOperation which is quite slow in general
            ' 
            ' At some point amongst all the code that runs there is presumably a crucial function which has the rules for binding a name-type syntax into an invocation.
            ' Ideally we'd find a way to call just that function here on invocationExpression.Expression
            Dim expressionWithoutArgList = ancestor.ReplaceNode(invocationExpression, invocationExpression.Expression)
            Dim speculativeSemanticModel As SemanticModel = Nothing
            Return semanticModel.TryGetSpeculativeSemanticModel(invocationExpression.SpanStart, expressionWithoutArgList, speculativeSemanticModel) AndAlso _
                speculativeSemanticModel.GetOperation(expressionWithoutArgList).Kind = operationkind.Invocation
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
