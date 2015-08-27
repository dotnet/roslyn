' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    Partial Friend Class VisualBasicIntroduceVariableService

        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _replacementAnnotation As New SyntaxAnnotation
            Private ReadOnly _replacementNode As SyntaxNode
            Private ReadOnly _matches As ISet(Of ExpressionSyntax)

            Private Sub New(replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax))
                _replacementNode = replacementNode
                _matches = matches
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim expression = TryCast(node, ExpressionSyntax)
                If expression IsNot Nothing AndAlso _matches.Contains(expression) Then
                    Return _replacementNode _
                        .WithLeadingTrivia(expression.GetLeadingTrivia()) _
                        .WithTrailingTrivia(expression.GetTrailingTrivia()) _
                        .WithAdditionalAnnotations(_replacementAnnotation)
                End If

                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitParenthesizedExpression(node As ParenthesizedExpressionSyntax) As SyntaxNode
                Dim newNode = MyBase.VisitParenthesizedExpression(node)
                If node IsNot newNode AndAlso newNode.IsKind(SyntaxKind.ParenthesizedExpression) Then
                    Dim parenthesizedExpression = DirectCast(newNode, ParenthesizedExpressionSyntax)
                    Dim innerExpression = DirectCast(parenthesizedExpression.OpenParenToken.GetNextToken().Parent, ExpressionSyntax)
                    If innerExpression.HasAnnotation(_replacementAnnotation) AndAlso innerExpression.Equals(parenthesizedExpression.Expression) Then
                        Return newNode.WithAdditionalAnnotations(Simplifier.Annotation)
                    End If
                End If

                Return newNode
            End Function

            Public Overloads Shared Function Visit(node As SyntaxNode, replacementNode As SyntaxNode, matches As ISet(Of ExpressionSyntax)) As SyntaxNode
                Return New Rewriter(replacementNode, matches).Visit(node)
            End Function

        End Class
    End Class
End Namespace
