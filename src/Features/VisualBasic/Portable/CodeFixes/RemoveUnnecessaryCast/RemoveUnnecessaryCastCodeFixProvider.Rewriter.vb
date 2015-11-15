' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryCast
    Partial Friend Class RemoveUnnecessaryCastCodeFixProvider
        Partial Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _castExpression As ExpressionSyntax

            Public Sub New(castExpression As ExpressionSyntax)
                _castExpression = castExpression
            End Sub

            Private Shared Function GetExpression(expression As ExpressionSyntax) As ExpressionSyntax
                If TypeOf expression Is ParenthesizedExpressionSyntax Then
                    Return expression.WithAdditionalAnnotations(Simplifier.Annotation)
                Else
                    Return expression
                End If
            End Function

            Public Overrides Function VisitCTypeExpression(node As CTypeExpressionSyntax) As SyntaxNode
                If node Is _castExpression Then
                    Return node.WithExpression(GetExpression(node.Expression)) _
                               .WithAdditionalAnnotations(Simplifier.Annotation) _
                               .Parenthesize()
                End If

                Return MyBase.VisitCTypeExpression(node)
            End Function

            Public Overrides Function VisitDirectCastExpression(node As DirectCastExpressionSyntax) As SyntaxNode
                If node Is _castExpression Then
                    Return node.WithExpression(GetExpression(node.Expression)) _
                               .WithAdditionalAnnotations(Simplifier.Annotation) _
                               .Parenthesize()
                End If

                Return MyBase.VisitDirectCastExpression(node)
            End Function

            Public Overrides Function VisitTryCastExpression(node As TryCastExpressionSyntax) As SyntaxNode
                If node Is _castExpression Then
                    Return node.WithExpression(GetExpression(node.Expression)) _
                               .WithAdditionalAnnotations(Simplifier.Annotation) _
                               .Parenthesize()
                End If

                Return MyBase.VisitTryCastExpression(node)
            End Function

            Public Overrides Function VisitPredefinedCastExpression(node As PredefinedCastExpressionSyntax) As SyntaxNode
                If node Is _castExpression Then
                    Return node.WithExpression(GetExpression(node.Expression)) _
                               .WithAdditionalAnnotations(Simplifier.Annotation) _
                               .Parenthesize()
                End If

                Return MyBase.VisitPredefinedCastExpression(node)
            End Function
        End Class
    End Class
End Namespace
