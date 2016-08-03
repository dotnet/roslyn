' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class NonRecursiveSyntaxWalkerTests
        Private Class ToStringWalker
            Inherits VisualBasicNonRecursiveSyntaxWalker

            Private _sb As StringBuilder

            Public Overloads Function Visit(node As SyntaxNode) As String
                Me._sb = New StringBuilder()
                MyBase.Visit(node)
                Return Me._sb.ToString()
            End Function

            Public Overrides Sub VisitToken(token As SyntaxToken)
                _sb.Append(token.ToFullString())
            End Sub
        End Class

        Private Class CountingWalker
            Inherits VisualBasicNonRecursiveSyntaxWalker

            Public Property NodesCount As Integer
            Public Property TokensCount As Integer

            Public Overrides Sub VisitNode(node As SyntaxNode)
                NodesCount += 1
                MyBase.VisitNode(node)
            End Sub

            Public Overrides Sub VisitToken(token As SyntaxToken)
                TokensCount += 1
                MyBase.VisitToken(token)
            End Sub
        End Class

        Private Class SkippedCountingWalker
            Inherits CountingWalker
            Protected Overrides Function ShouldVisitChildren(node As SyntaxNode) As Boolean
                Return Not (TypeOf node Is LiteralExpressionSyntax)
            End Function
        End Class

        <Fact>
        Public Sub TestNonRecursiveWalker()
            Dim code = "if (a)\r\n  b .  Foo();"
            Dim statement = SyntaxFactory.ParseExecutableStatement(code)
            Assert.Equal(code, New ToStringWalker().Visit(statement))
        End Sub

        <Fact>
        Public Sub TestNonRecursiveWalkerCount()
            Dim code = "1 + 2 + 3"
            Dim expression = SyntaxFactory.ParseExpression(code)
            Dim countingWalker = New CountingWalker()
            countingWalker.Visit(expression)
            Assert.Equal(5, countingWalker.NodesCount)
            Assert.Equal(5, countingWalker.TokensCount)
        End Sub

        <Fact>
        Public Sub TestSkippedNonRecursiveWalkerCount()
            Dim code = "1 + 2 + a"
            Dim expression = SyntaxFactory.ParseExpression(code)
            Dim countingWalker = New SkippedCountingWalker()
            countingWalker.Visit(expression)
            Assert.Equal(3, countingWalker.NodesCount)
            Assert.Equal(3, countingWalker.TokensCount)
        End Sub
    End Class
End Namespace