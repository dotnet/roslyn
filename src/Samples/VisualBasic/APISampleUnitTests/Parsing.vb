' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Public Class Parsing

    <Fact>
    Sub TextParseTreeRoundtrip()
        Dim code =
<code>
Class C
    Sub M()
    End Sub
End Class ' exact text round trip, including comments and whitespace
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Assert.Equal(code, tree.GetText().ToString())
    End Sub

    <Fact>
    Sub DetermineValidIdentifierName()
        ValidIdentifier("[Class]", True)
        ValidIdentifier("Class", False)
    End Sub

    Sub ValidIdentifier(identifier As String, expectedValid As Boolean)
        Dim token = SyntaxFactory.ParseToken(identifier)
        Assert.Equal(expectedValid, token.Kind() = SyntaxKind.IdentifierToken AndAlso token.Span.Length = identifier.Length)
    End Sub

    <Fact>
    Sub SyntaxFactsMethods()
        Assert.Equal("Protected Friend", SyntaxFacts.GetText(Accessibility.ProtectedOrFriend))
        Assert.Equal("Me", SyntaxFacts.GetText(SyntaxKind.MeKeyword))
        Assert.Equal(SyntaxKind.CharacterLiteralExpression, SyntaxFacts.GetLiteralExpression(SyntaxKind.CharacterLiteralToken))
        Assert.Equal(False, SyntaxFacts.IsPunctuation(SyntaxKind.StringLiteralToken))
    End Sub

    <Fact>
    Sub ParseTokens()
        Dim tokens = SyntaxFactory.ParseTokens("Class C ' trivia")
        Dim fullTexts = tokens.Select(Function(token) token.ToFullString())
        Assert.True(fullTexts.SequenceEqual({"Class ", "C ' trivia", ""}))
    End Sub

    <Fact>
    Sub ParseExpression()
        Dim expression = SyntaxFactory.ParseExpression("1 + 2")
        If expression.Kind() = SyntaxKind.AddExpression Then
            Dim binaryExpression = CType(expression, BinaryExpressionSyntax)
            Dim operatorToken = binaryExpression.OperatorToken
            Assert.Equal("+", operatorToken.ToString())
            Dim left = binaryExpression.Left
            Assert.Equal(SyntaxKind.NumericLiteralExpression, left.Kind)
        End If
    End Sub

    <Fact>
    Sub IncrementalParse()
        Dim oldCode =
<code>
Class C
End Class
</code>.GetCode()
        Dim newCode =
<code>
    Sub M()
    End Sub
</code>.GetCode()

        Dim oldText = SourceText.From(oldCode)
        Dim newText = oldText.WithChanges(New TextChange(New TextSpan(oldCode.IndexOf("End"), 0), newCode))

        Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText)
        Dim newTree = oldTree.WithChangedText(newText)

        Assert.Equal(newText.ToString(), newTree.ToString())
    End Sub

    <Fact>
    Sub PreprocessorDirectives()
        Dim code =
<code>
#If True
Class A
End Class
#Else
Class B
End Class
#End If
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)

        Dim eof = tree.GetRoot().FindToken(tree.GetText().Length, False)
        Assert.Equal(True, eof.HasLeadingTrivia)
        Assert.Equal(False, eof.HasTrailingTrivia)
        Assert.Equal(True, eof.ContainsDirectives)

        Dim trivia = eof.LeadingTrivia
        Assert.Equal(3, trivia.Count)
        Assert.Equal("#Else" & vbCrLf, trivia.ElementAt(0).ToFullString())
        Assert.Equal(SyntaxKind.DisabledTextTrivia, trivia.ElementAt(1).Kind)
        Assert.Equal("#End If", trivia.ElementAt(2).ToString())

        Dim directive = tree.GetRoot().GetLastDirective()
        Assert.Equal("#End If", directive.ToString())

        directive = directive.GetPreviousDirective()
        Assert.Equal("#Else" & vbCrLf, directive.ToFullString())
    End Sub
End Class
