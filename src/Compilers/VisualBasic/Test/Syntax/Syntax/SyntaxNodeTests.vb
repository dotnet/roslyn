Imports Microsoft.CodeAnalysis.VisualBasic
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SyntaxNodeTests
        <Fact>
        Public Sub SyntaxTree_WithOperatorTokenUpdatesOperatorToken()

            Dim expression = SyntaxFactory.AddExpression(SyntaxFactory.ParseExpression("5"),
                                                         SyntaxFactory.ParseToken("+"),
                                                         SyntaxFactory.ParseExpression("3"))

            Dim newOperatorToken = SyntaxFactory.Token(SyntaxKind.MinusToken)
            
            Dim newExpression = expression.WithOperatorToken(newOperatorToken)

            Assert.Equal(newExpression.Kind, SyntaxKind.SubtractExpression)
            Assert.Equal(newExpression.OperatorToken.Kind, SyntaxKind.MinusToken)
        End Sub
    End Class
End NameSpace