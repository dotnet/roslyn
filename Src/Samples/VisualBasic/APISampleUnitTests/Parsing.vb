' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<TestClass()>
Public Class Parsing

    <TestMethod()>
    Sub TextParseTreeRoundtrip()
        Dim code =
<code>
Class C
    Sub M()
    End Sub
End Class ' exact text round trip, including comments and whitespace
</code>.GetCode()

        Dim tree = SyntaxFactory.ParseSyntaxTree(code)
        Assert.AreEqual(code, tree.GetText().ToString())
    End Sub

    <TestMethod()>
    Sub DetermineValidIdentifierName()
        ValidIdentifier("[Class]", True)
        ValidIdentifier("Class", False)
    End Sub

    Sub ValidIdentifier(identifier As String, expectedValid As Boolean)
        Dim token = SyntaxFactory.ParseToken(identifier)
        Assert.AreEqual(expectedValid, token.VBKind() = SyntaxKind.IdentifierToken AndAlso token.Span.Length = identifier.Length)
    End Sub

    <TestMethod()>
    Sub SyntaxFactsMethods()
        Assert.AreEqual("Protected Friend", SyntaxFacts.GetText(Accessibility.ProtectedOrFriend))
        Assert.AreEqual("Me", SyntaxFacts.GetText(SyntaxKind.MeKeyword))
        Assert.AreEqual(SyntaxKind.CharacterLiteralExpression, SyntaxFacts.GetLiteralExpression(SyntaxKind.CharacterLiteralToken))
        Assert.AreEqual(False, SyntaxFacts.IsPunctuation(SyntaxKind.StringLiteralToken))
    End Sub

    <TestMethod()>
    Sub ParseTokens()
        Dim tokens = SyntaxFactory.ParseTokens("Class C ' trivia")
        Dim fullTexts = tokens.Select(Function(token) token.ToFullString())
        Assert.IsTrue(fullTexts.SequenceEqual({"Class ", "C ' trivia", ""}))
    End Sub

    <TestMethod()>
    Sub ParseExpression()
        Dim expression = SyntaxFactory.ParseExpression("1 + 2")
        If expression.VBKind() = SyntaxKind.AddExpression Then
            Dim binaryExpression = CType(expression, BinaryExpressionSyntax)
            Dim operatorToken = binaryExpression.OperatorToken
            Assert.AreEqual("+", operatorToken.ToString())
            Dim left = binaryExpression.Left
            Assert.AreEqual(SyntaxKind.NumericLiteralExpression, left.VBKind)
        End If
    End Sub

    <TestMethod()>
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

        Assert.AreEqual(newText.ToString(), newTree.ToString())
    End Sub

    <TestMethod()>
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
        Assert.AreEqual(True, eof.HasLeadingTrivia)
        Assert.AreEqual(False, eof.HasTrailingTrivia)
        Assert.AreEqual(True, eof.ContainsDirectives)

        Dim trivia = eof.LeadingTrivia
        Assert.AreEqual(3, trivia.Count)
        Assert.AreEqual("#Else" & vbCrLf, trivia.ElementAt(0).ToFullString())
        Assert.AreEqual(SyntaxKind.DisabledTextTrivia, trivia.ElementAt(1).VBKind)
        Assert.AreEqual("#End If", trivia.ElementAt(2).ToString())

        Dim directive = tree.GetRoot().GetLastDirective()
        Assert.AreEqual("#End If", directive.ToString())

        directive = directive.GetPreviousDirective()
        Assert.AreEqual("#Else" & vbCrLf, directive.ToFullString())
    End Sub
End Class
