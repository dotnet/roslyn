Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class NonRecurSyntaxRewriterTests

        Private Class NonRecursiveIdentityRewriter
            Inherits VisualBasicNonRecursiveSyntaxRewriter
        End Class

        Private Class SkippedNonRecursiveRewriter
            Inherits VisualBasicNonRecursiveSyntaxRewriter

            Protected Overrides Function ShouldRewriteChildren(nodeOrToken As SyntaxNodeOrToken, ByRef rewritten As SyntaxNodeOrToken) As Boolean
                rewritten = nodeOrToken
                Return Not (nodeOrToken.IsNode AndAlso TypeOf nodeOrToken.AsNode() Is LiteralExpressionSyntax)
            End Function

            Public Property VisitNodeCallsCount As Integer

            Public Overrides Function VisitNode(original As SyntaxNode, rewritten As SyntaxNode) As SyntaxNode
                Me.VisitNodeCallsCount += 1
                Return MyBase.VisitNode(original, rewritten)
            End Function
        End Class

        Private Class SkippedAndTransformedNonRecursiveRewriter
            Inherits VisualBasicNonRecursiveSyntaxRewriter

            Private _skipRewriter As New SkipLiteralNonRecursiveRewriter()

            Protected Overrides Function ShouldRewriteChildren(nodeOrToken As SyntaxNodeOrToken, ByRef rewritten As SyntaxNodeOrToken) As Boolean
                If nodeOrToken.IsNode Then
                    Dim value = _skipRewriter.Visit(nodeOrToken.AsNode())
                    rewritten = _skipRewriter.Rewriten
                    Return Not value
                End If

                rewritten = nodeOrToken
                Return True
            End Function

            Public Property VisitNodeCallsCount As Integer

            Public Overrides Function VisitNode(original As SyntaxNode, rewritten As SyntaxNode) As SyntaxNode
                Me.VisitNodeCallsCount += 1
                Return MyBase.VisitNode(original, rewritten)
            End Function

            Private Class SkipLiteralNonRecursiveRewriter
                Inherits VisualBasicSyntaxVisitor(Of Boolean)

                Public Property Rewriten As SyntaxNode

                Public Overrides Function VisitLiteralExpression(node As LiteralExpressionSyntax) As Boolean
                    Rewriten = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))
                    Return True
                End Function
            End Class
        End Class


        <Fact>
        Public Sub TestLongExpressionWithNonRecursiveRewriter()
            Dim longExpression = Enumerable.Range(0, 1000000).Select(Function(i) DirectCast(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i)), ExpressionSyntax)).Aggregate(Function(i, j) SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, i, SyntaxFactory.Token(SyntaxKind.PlusToken), j))

            Dim exp = New NonRecursiveIdentityRewriter().Visit(longExpression)
            Dim sb = New StringBuilder()
            For i = 0 To 1000000 - 1
                sb.Append(i)
                sb.Append("+")
            Next
            sb.Length -= 1
            Assert.Equal(sb.ToString(), exp.ToString())
        End Sub

        <Fact>
        Public Sub TestNonRecursiveRewriterSkip()
            Dim code = "1 + 2 + 3"
            Dim expression = SyntaxFactory.ParseExpression(code)
            Dim skippedNonRecursiveRewriter = New SkippedNonRecursiveRewriter()
            Dim newExpression = skippedNonRecursiveRewriter.Visit(expression)
            Assert.Equal(2, skippedNonRecursiveRewriter.VisitNodeCallsCount)
            Assert.Equal(expression, newExpression)
        End Sub

        <Fact>
        Public Sub TestNonRecursiveRewriterSkipAndTransform()
            Dim code = "1 + 2 + 3"
            Dim expression = SyntaxFactory.ParseExpression(code)
            Dim skippedAndTransformedNonRecursiveRewriter = New SkippedAndTransformedNonRecursiveRewriter()
            Dim transformedExpr = skippedAndTransformedNonRecursiveRewriter.Visit(expression)
            Assert.Equal(2, skippedAndTransformedNonRecursiveRewriter.VisitNodeCallsCount)
            Assert.Equal("0+ 0+ 0", transformedExpr.ToString())
        End Sub
    End Class
End Namespace