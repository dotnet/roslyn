Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Partial Friend Class ConvertToInterpolatedStringRefactoringProvider
    Private Class InterpolatedStringRewriter
        Inherits VisualBasicSyntaxRewriter

        Private ReadOnly expandedArguments As ImmutableArray(Of ExpressionSyntax)

        Private Sub New(expandedArguments As ImmutableArray(Of ExpressionSyntax))
            Me.expandedArguments = expandedArguments
        End Sub

        Public Overrides Function VisitInterpolation(node As InterpolationSyntax) As SyntaxNode
            Dim literalExpression = CType(node.Expression, LiteralExpressionSyntax)
            If literalExpression IsNot Nothing AndAlso literalExpression.IsKind(SyntaxKind.NumericLiteralExpression) Then
                Dim index = CType(literalExpression.Token.Value, Integer)
                If index >= 0 AndAlso index < expandedArguments.Length Then
                    Return node.WithExpression(expandedArguments(index))
                End If
            End If

            Return MyBase.VisitInterpolation(node)
        End Function

        Public Overloads Shared Function Visit(interpolatedString As InterpolatedStringExpressionSyntax, expandedArguments As ImmutableArray(Of ExpressionSyntax)) As InterpolatedStringExpressionSyntax
            Return CType(New InterpolatedStringRewriter(expandedArguments).Visit(interpolatedString), InterpolatedStringExpressionSyntax)
        End Function

    End Class
End Class
