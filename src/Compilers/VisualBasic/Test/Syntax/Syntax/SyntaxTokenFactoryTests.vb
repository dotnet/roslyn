' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyntaxTokenFactoryTests

        <Fact>
        Public Sub TestKeywordToken()

            ' test if keyword tokens can be created
            Dim keywordToken = SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword)
            Assert.Equal(keywordToken.ToString(), SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword))
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            keywordToken = SyntaxFactory.Token(SyntaxKind.XmlKeyword)
            Assert.Equal(keywordToken.ToString(), SyntaxFacts.GetText(SyntaxKind.XmlKeyword))
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            ' check that trivia works
            keywordToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.AddHandlerKeyword)
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            keywordToken = SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword, trailing:=New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")))
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            keywordToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.AddHandlerKeyword, trailing:=New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")))
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            ' check that trivia works
            keywordToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.AddHandlerKeyword, SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword).ToUpperInvariant)
            Assert.Equal(keywordToken.ToString(), SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword).ToUpperInvariant)
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            keywordToken = SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword, New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword).ToUpperInvariant)
            Assert.Equal(keywordToken.ToString(), SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword).ToUpperInvariant)
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)

            keywordToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.AddHandlerKeyword, New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword).ToUpperInvariant)
            Assert.Equal(keywordToken.ToString(), SyntaxFacts.GetText(SyntaxKind.AddHandlerKeyword).ToUpperInvariant)
            Assert.Equal(keywordToken.LeadingTrivia.Count, 1)
            Assert.Equal(keywordToken.TrailingTrivia.Count, 1)
        End Sub

        <Fact>
        Public Sub TestPunctuationToken()

            ' test if keyword tokens can be created
            Dim punctuationToken = SyntaxFactory.Token(SyntaxKind.ExclamationToken)
            Assert.Equal(punctuationToken.ToString(), SyntaxFacts.GetText(SyntaxKind.ExclamationToken))
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            punctuationToken = SyntaxFactory.Token(SyntaxKind.XmlKeyword)
            Assert.Equal(punctuationToken.ToString(), SyntaxFacts.GetText(SyntaxKind.XmlKeyword))
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            ' check that trivia works
            punctuationToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.ExclamationToken)
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            punctuationToken = SyntaxFactory.Token(SyntaxKind.ExclamationToken, trailing:=New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")))
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            punctuationToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.ExclamationToken, trailing:=New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")))
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            ' check that trivia works
            punctuationToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.ExclamationToken, SyntaxFacts.GetText(SyntaxKind.ExclamationToken).ToUpperInvariant)
            Assert.Equal(punctuationToken.ToString(), SyntaxFacts.GetText(SyntaxKind.ExclamationToken).ToUpperInvariant)
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            punctuationToken = SyntaxFactory.Token(SyntaxKind.ExclamationToken, New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxFacts.GetText(SyntaxKind.ExclamationToken).ToUpperInvariant)
            Assert.Equal(punctuationToken.ToString(), SyntaxFacts.GetText(SyntaxKind.ExclamationToken).ToUpperInvariant)
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)

            punctuationToken = SyntaxFactory.Token(New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxKind.ExclamationToken, New SyntaxTriviaList(SyntaxFactory.WhitespaceTrivia(" ")), SyntaxFacts.GetText(SyntaxKind.ExclamationToken).ToUpperInvariant)
            Assert.Equal(punctuationToken.ToString(), SyntaxFacts.GetText(SyntaxKind.ExclamationToken).ToUpperInvariant)
            Assert.Equal(punctuationToken.LeadingTrivia.Count, 1)
            Assert.Equal(punctuationToken.TrailingTrivia.Count, 1)
        End Sub

        <Fact>
        Public Sub TestAllFactoryCalls()

            Dim token As SyntaxToken

            For k = CInt(SyntaxKind.None) To CInt(SyntaxKind.BadDirectiveTrivia)

                ' keywords or punctuation
                If (k >= CInt(SyntaxKind.AddHandlerKeyword) AndAlso k <= CInt(SyntaxKind.YieldKeyword)) OrElse
                   (k >= CInt(SyntaxKind.ExclamationToken) AndAlso k <= CInt(SyntaxKind.EndOfXmlToken)) OrElse
                   k = SyntaxKind.NameOfKeyword OrElse
                   k = SyntaxKind.DollarSignDoubleQuoteToken OrElse
                   k = SyntaxKind.EndOfInterpolatedStringToken _
                Then

                    token = SyntaxFactory.Token(CType(k, SyntaxKind))
                    ' no exception during execution
                    Assert.Equal(token.Kind, CType(k, SyntaxKind))

                    token = SyntaxFactory.Token(CType(k, SyntaxKind), text:=Nothing)
                    ' check default text is there
                    Assert.Equal(token.ToString(), SyntaxFacts.GetText(CType(k, SyntaxKind)))

                    token = SyntaxFactory.Token(CType(k, SyntaxKind), SyntaxFacts.GetText(CType(k, SyntaxKind)).ToUpperInvariant)
                    ' check default text is there
                    Assert.Equal(token.ToString(), SyntaxFacts.GetText(CType(k, SyntaxKind)).ToUpperInvariant)

                    token = SyntaxFactory.Token(CType(k, SyntaxKind), String.Empty)
                    ' check default text is there
                    Assert.Equal(token.ToString(), String.Empty)
                Else
                    Dim localKind = k
                    Assert.Throws(Of ArgumentOutOfRangeException)(Function() SyntaxFactory.Token(CType(localKind, SyntaxKind)))
                End If
            Next

        End Sub

        <Fact()>
        Public Sub TestReplaceTriviaDeep()
            ' The parser for VB will stop when it sees the #end if directive which is a different 
            ' behavior from the C# compiler.  That said the whitespace trivia was only turned to double for the
            ' DirectiveTrivia and not the whitespace between the identifier and operators.
            ' Added for parity of scenario with Directives but capturing difference in behavior
            Dim SourceText = "#if true then" & Environment.NewLine & "a + " & Environment.NewLine & "#end if" & Environment.NewLine & " + b"
            Dim expr As ExpressionSyntax = SyntaxFactory.ParseExpression(SourceText, consumeFullText:=False)

            ' get whitespace trivia inside structured directive trivia
            Dim deepTrivia = From d In expr.GetDirectives().SelectMany(Function(d)
                                                                           Return d.DescendantTrivia.Where(Function(tr) tr.Kind = SyntaxKind.WhitespaceTrivia)
                                                                       End Function).ToList

            ' replace deep trivia with double-whitespace trivia
            Dim twoSpace = SyntaxFactory.Whitespace("  ")
            Dim expr2 = expr.ReplaceTrivia(deepTrivia, Function(tr, tr2) twoSpace)

            Assert.Equal("#if  true  then" & Environment.NewLine & "a + " & Environment.NewLine, expr2.ToFullString())
        End Sub

        <Fact()>
        Public Sub TestReplaceSingleTriviaInNode()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Assert.NotNull(expr)
            Assert.Equal(SyntaxKind.AddExpression, expr.Kind)
            Dim bex = CType(expr, BinaryExpressionSyntax)
            Assert.Equal(SyntaxKind.IdentifierName, bex.Left.Kind)
            Dim id = (CType(bex.Left, IdentifierNameSyntax)).Identifier
            Assert.Equal("a", id.ValueText)
            Assert.Equal(1, id.TrailingTrivia.Count)
            Dim trivia = id.TrailingTrivia(0)
            Assert.Equal(1, trivia.Width)
            Dim bex2 = bex.ReplaceTrivia(trivia, SyntaxFactory.Whitespace("  "))
            Assert.Equal(SyntaxKind.AddExpression, bex2.Kind)
            Assert.Equal("a  + b", bex2.ToFullString())
            Assert.Equal("a + b", bex.ToFullString())
        End Sub

        <Fact()>
        Public Sub TestReplaceMultipleTriviaInNode()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Dim twoSpaces = SyntaxFactory.Whitespace("  ")
            Dim trivia = (From tr In expr.DescendantTrivia()
                          Where tr.Kind = SyntaxKind.WhitespaceTrivia
                          Select tr).ToList

            Dim replaced As ExpressionSyntax = expr.ReplaceTrivia(trivia, Function(tr, tr2) twoSpaces)
            Dim rtext = replaced.ToFullString()
            Assert.Equal("a  +  b", rtext)
        End Sub

        <Fact()>
        Public Sub TestReplaceMultipleTriviaInNodeWithNestedExpression()
            Dim expr = SyntaxFactory.ParseExpression("a + (c - b)")
            Dim twoSpaces = SyntaxFactory.Whitespace("  ")
            Dim trivia = (From tr In expr.DescendantTrivia()
                          Where tr.Kind = SyntaxKind.WhitespaceTrivia
                          Select tr).ToList

            Dim replaced As ExpressionSyntax = expr.ReplaceTrivia(trivia, Function(tr, tr2) twoSpaces)
            Dim rtext = replaced.ToFullString()
            Assert.Equal("a  +  (c  -  b)", rtext)
        End Sub

        <Fact()>
        Public Sub TestReplaceSingleTriviaForMultipleTriviaInNode()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Dim tr = expr.DescendantTrivia().First()
            Dim replaced = expr.ReplaceTrivia(tr, {SyntaxFactory.Space, SyntaxFactory.CommentTrivia("' goo "), SyntaxFactory.Space})
            Dim rtext = replaced.ToFullString()
            Assert.Equal("a ' goo  + b", rtext)
        End Sub

        <Fact()>
        Public Sub TestReplaceSingleTriviaInToken()
            Dim id = SyntaxFactory.ParseToken("a ")
            Assert.Equal(SyntaxKind.IdentifierToken, id.Kind)
            Dim trivia = id.TrailingTrivia(0)
            Assert.Equal(1, trivia.Width)
            Dim id2 = id.ReplaceTrivia(trivia, SyntaxFactory.Whitespace("  "))
            Assert.Equal("a  ", id2.ToFullString())
            Assert.Equal("a ", id.ToFullString())
        End Sub

        <Fact()>
        Public Sub TestReplaceMultipleTriviaInToken()
            Dim id = SyntaxFactory.ParseToken("a ' goo " & Environment.NewLine)

            ' replace each trivia with a single space
            Dim id2 = id.ReplaceTrivia(id.GetAllTrivia(), Function(tr, tr2) SyntaxFactory.Space)

            Assert.Equal("a   ", id2.ToFullString())
        End Sub
    End Class

End Namespace
