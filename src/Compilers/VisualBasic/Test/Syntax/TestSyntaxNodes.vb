' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Syntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class TestSyntaxNodes
        Inherits BasicTestBase

        Private ReadOnly _spaceTrivia As SyntaxTrivia = SyntaxFactory.WhitespaceTrivia(" ")
        Private ReadOnly _newlineTrivia As SyntaxTriviaList = SyntaxTriviaListBuilder.Create.Add(SyntaxFactory.WhitespaceTrivia(Environment.NewLine)).ToList

        Private Function CreateIntegerLiteral(value As ULong) As LiteralExpressionSyntax
            Return SyntaxFactory.NumericLiteralExpression(SyntaxFactory.IntegerLiteralToken(value.ToString(), LiteralBase.Decimal, TypeCharacter.None, value))
        End Function

        ' Creates "1- X( 3, 4+ 8, 9)"
        Private Function CreateSimpleTree() As BinaryExpressionSyntax
            Dim operandsx() As ArgumentSyntax = {SyntaxFactory.SimpleArgument(CreateIntegerLiteral(3)),
                                          SyntaxFactory.SimpleArgument(SyntaxFactory.AddExpression(CreateIntegerLiteral(4), SyntaxFactory.Token(SyntaxKind.PlusToken, trailing:=_spaceTrivia), CreateIntegerLiteral(8))),
                                          SyntaxFactory.SimpleArgument(CreateIntegerLiteral(9))}

            'Dim operands = New SeparatedSyntaxListBuilder(Of ArgumentSyntax)(8)
            'operands.Add(Syntax.SimpleArgument(CreateIntegerLiteral(3)))
            'operands.AddSeparator(Syntax.Token(SyntaxKind.CommaToken,spaceTrivia))
            'operands.Add(Syntax.SimpleArgument(Syntax.AddExpression(CreateIntegerLiteral(4),
            'Syntax.Token(SyntaxKind.PlusToken,spaceTrivia), CreateIntegerLiteral(8))))
            'operands.AddSeparator(Syntax.Token(SyntaxKind.CommaToken,spaceTrivia))
            'operands.Add(Syntax.SimpleArgument(CreateIntegerLiteral(9)))

            ' Use Syntax.SeparatedList factory method instead of builder
            Dim operands = SyntaxFactory.SeparatedList(Of ArgumentSyntax)({SyntaxFactory.SimpleArgument(CreateIntegerLiteral(3)),
                                                                     SyntaxFactory.SimpleArgument(SyntaxFactory.AddExpression(CreateIntegerLiteral(4), SyntaxFactory.Token(SyntaxKind.PlusToken, trailing:=_spaceTrivia), CreateIntegerLiteral(8))),
                                                                     SyntaxFactory.SimpleArgument(CreateIntegerLiteral(9))},
                                                                    {SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia),
                                                                     SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia)
                                                                    })

            Return SyntaxFactory.SubtractExpression(CreateIntegerLiteral(1), SyntaxFactory.Token(SyntaxKind.MinusToken, trailing:=_spaceTrivia),
                                             SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("X")),
                                                                    SyntaxFactory.ArgumentList(SyntaxFactory.Token(SyntaxKind.OpenParenToken, trailing:=_spaceTrivia),
                                                                                             operands,
                                                                                             SyntaxFactory.Token(SyntaxKind.CloseParenToken, trailing:=_spaceTrivia))))

        End Function

        <Fact>
        Public Sub TestParents()
            Dim simpleTree = CreateSimpleTree()

            Assert.Null(simpleTree.Parent)
            Assert.Equal(simpleTree, CType(simpleTree, BinaryExpressionSyntax).Left.Parent)
            Assert.Equal(simpleTree, CType(simpleTree, BinaryExpressionSyntax).Right.Parent)

            Dim x As InvocationExpressionSyntax = CType(CType(simpleTree, BinaryExpressionSyntax).Right, InvocationExpressionSyntax)
            Dim argList As ArgumentListSyntax = x.ArgumentList

            Assert.Equal(argList, argList.Arguments(0).Parent)
            Assert.Equal(argList, argList.Arguments(1).Parent)
            Assert.Equal(argList, argList.Arguments.GetWithSeparators(1).Parent)
            Assert.Equal(argList, argList.Arguments.GetWithSeparators(3).Parent)
        End Sub

        <Fact>
        Public Sub TestChildren()
            Dim simpleTree = CreateSimpleTree()
            Dim invocation As InvocationExpressionSyntax = CType(CType(simpleTree, BinaryExpressionSyntax).Right, InvocationExpressionSyntax)
            Dim argList As ArgumentListSyntax = invocation.ArgumentList
            Dim children = argList.ChildNodesAndTokens()

            Assert.Equal(7, children.Count)
            Assert.Equal(children(0), argList.OpenParenToken)
            Assert.Equal(children(1), argList.Arguments(0))
            Assert.Equal(children(2), argList.Arguments.GetWithSeparators(1))
            Assert.Equal(children(3), argList.Arguments(1))
            Assert.Equal(children(4), argList.Arguments.GetWithSeparators(3))
            Assert.Equal(children(5), argList.Arguments(2))
            Assert.Equal(children(6), argList.CloseParenToken)

            children = simpleTree.ChildNodesAndTokens()
            Dim binop = DirectCast(simpleTree, BinaryExpressionSyntax)
            Assert.Equal(3, children.Count)
            Assert.Equal(children(0), binop.Left)
            Assert.Equal(children(1), binop.OperatorToken)
            Assert.Equal(children(2), binop.Right)


            Dim ItemList As New List(Of String)
            Dim ItemListRev As New List(Of String)

            Dim VB1 = children.GetEnumerator
            Do While VB1.MoveNext
                ItemList.Add(VB1.Current.ToString)
            Loop

            Dim i As SyntaxNodeOrToken
            For Each i In children.Reverse
                ItemListRev.Add(i.ToString)
            Next

            Assert.Equal(ItemList.Count, ItemListRev.Count)
            If (ItemList.Count > 0) Then
                Dim L0 As Integer = (ItemList.Count - 1)
                Dim I0 As Integer = 0
                Do While (I0 <= L0)
                    Assert.Equal(ItemList.Item(I0), ItemListRev.Item(((ItemList.Count - 1) - I0)))
                    I0 += 1
                Loop
            End If

            Dim b1 As Integer = 0
            Dim enumerator = children.GetEnumerator
            enumerator.Reset()
            Do While enumerator.MoveNext
                Dim item1 As SyntaxNodeOrToken = enumerator.Current
                b1 += 1
            Loop
            Dim b2 As Integer = 0
            Dim enumeratorr = children.Reverse.GetEnumerator
            enumeratorr.Reset()
            Do While enumeratorr.MoveNext
                Dim item1 As SyntaxNodeOrToken = enumeratorr.Current
                b2 += 1
            Loop
            Assert.Equal(b1, b2)
            Assert.Throws(Of ArgumentOutOfRangeException)((Sub()
                                                               Dim i1 As SyntaxNodeOrToken = children.Item(-1)
                                                           End Sub
                                                       ))
            Assert.Equal(ItemList.Item((ItemList.Count - 1)), children.Last.ToString)
            Assert.Equal(ItemList.Item(0), Enumerable.First(Of SyntaxNodeOrToken)(DirectCast(children, IEnumerable(Of SyntaxNodeOrToken))).ToString)

            'Comparison operators = and <>   
            Dim xc As ChildSyntaxList = children
            Assert.Equal(xc, children)
            Assert.NotEqual(xc, New ChildSyntaxList)

            'Explicitly calling the <> operator as a double negative
            Assert.False(xc <> children, "Verifying <> operator for ChildSyntaxList items - This should return false as xc was assigned from Children")
        End Sub

        <Fact>
        <WorkItem(21812, "https://github.com/dotnet/roslyn/issues/21812")>
        Public Sub TestTupleTypeInSyntaxFactory()
            Dim int = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword))
            Dim tuple = SyntaxFactory.TupleType(SyntaxFactory.TypedTupleElement(int), SyntaxFactory.TypedTupleElement(int))

            ' Array
            Dim intArraySyntax = Parse("
Class C
    Dim x As (Integer, Integer)()
End Class")
            intArraySyntax.AssertTheseDiagnostics(<errors></errors>)
            SyntaxFactory.ArrayType(tuple) ' no exception

            ' Object creation
            Dim objectCreationSyntax = Parse("
Class C
    Dim x = New (Integer, Integer)(1)
End Class")
            objectCreationSyntax.AssertTheseDiagnostics(<errors></errors>)
            SyntaxFactory.ObjectCreationExpression(tuple) ' no exception

            ' Array creation
            Dim arrayCreationSyntax = Parse("
Class C
    Dim x = New (Integer, Integer)(1) { }
End Class")
            arrayCreationSyntax.AssertTheseDiagnostics(<errors></errors>)
            SyntaxFactory.ArrayCreationExpression(tuple, SyntaxFactory.CollectionInitializer()) ' no exception

            ' Nullable
            Dim nullableSyntax = Parse("
Class C
    Dim x As (Integer, Integer)?
End Class")
            nullableSyntax.AssertTheseDiagnostics(<errors></errors>)
            SyntaxFactory.NullableType(tuple) ' no exception

            ' Attribute (cannot parse)
            Dim attributeSyntax = Parse("
<(Integer, Integer)>
")
            attributeSyntax.AssertTheseDiagnostics(<errors><![CDATA[
BC30203: Identifier expected.
<(Integer, Integer)>
 ~
                                                   ]]></errors>)
            Assert.Throws(Of ArgumentException)(Sub() SyntaxFactory.Attribute(tuple))

            ' Inherits
            Dim inheritsSyntax = Parse("
Class C
    Inherits (Integer, Integer)
End Class
")
            inheritsSyntax.AssertTheseDiagnostics(<errors></errors>)
            SyntaxFactory.InheritsStatement(tuple)

            ' Implements
            Dim implementsSyntax = Parse("
Class C
    Implements (Integer, Integer)
End Class
")
            implementsSyntax.AssertTheseDiagnostics(<errors></errors>)
            SyntaxFactory.ImplementsStatement(tuple)

        End Sub

        ' Verify spans within a list of consecutive nodes are all consistent.
        Private Sub VerifyListSpans(Of T As VisualBasicSyntaxNode)(list As SyntaxList(Of T), expectedFullSpan As TextSpan)
            If list.Count > 0 Then
                ' List should fill up the full span.
                Assert.Equal(expectedFullSpan.Start, list(0).FullSpan.Start)
                Assert.Equal(expectedFullSpan.End, list(list.Count - 1).FullSpan.End)

                For i = 0 To list.Count - 1
                    ' Make sure children's full spans are adjacent
                    If i > 0 Then
                        Assert.Equal(list(i - 1).FullSpan.End, list(i).FullSpan.Start)
                    End If
                    If i < list.Count - 1 Then
                        Assert.Equal(list(i).FullSpan.End, list(i + 1).FullSpan.Start)
                    End If

                    ' Recursively verify
                    VerifyAllSpans(list(i))
                Next
            End If
        End Sub

        ' Verify spans within a list of consecutive nodes are all consistent.
        Private Sub VerifyListSpans(list As ChildSyntaxList, expectedFullSpan As TextSpan)
            If list.Count > 0 Then
                ' List should fill up the full span.
                Assert.Equal(expectedFullSpan.Start, list(0).FullSpan.Start)
                Assert.Equal(expectedFullSpan.End, list(list.Count - 1).FullSpan.End)

                For i = 0 To list.Count - 2
                    ' Make sure children's full spans are adjacent

                    Assert.Equal(list(i).FullSpan.End, list(i + 1).FullSpan.Start)

                    ' Recursively verify
                    Dim node = list(i)
                    If node.IsNode Then
                        VerifyAllSpans(node.AsNode)
                    End If

                Next
            End If
        End Sub

        ' Verify spans within a list of consecutive nodes are all consistent.
        Private Sub VerifyListSpans(list As SyntaxNodeOrTokenList, expectedFullSpan As TextSpan)
            If list.Count > 0 Then
                ' List should fill up the full span.
                Assert.Equal(expectedFullSpan.Start, list(0).FullSpan.Start)
                Assert.Equal(expectedFullSpan.End, list(list.Count - 1).FullSpan.End)

                For i = 0 To list.Count - 1
                    ' Make sure children's full spans are adjacent
                    If i > 0 Then
                        Assert.Equal(list(i - 1).FullSpan.End, list(i).FullSpan.Start)
                    End If
                    If i < list.Count - 1 Then
                        Assert.Equal(list(i).FullSpan.End, list(i + 1).FullSpan.Start)
                    End If

                    ' Recursively verify
                    If list(i).IsNode Then
                        VerifyAllSpans(list(i).AsNode)
                    End If
                Next
            End If
        End Sub

        ' Verify spans within a list of consecutive nodes are all consistent.
        Private Sub VerifyListSpans(list As SyntaxTokenList, expectedFullSpan As TextSpan)
            If list.Count > 0 Then
                ' List should fill up the full span.
                Assert.Equal(expectedFullSpan.Start, list(0).FullSpan.Start)
                Assert.Equal(expectedFullSpan.End, list(list.Count - 1).FullSpan.End)

                For i = 0 To list.Count - 1
                    ' Make sure children's full spans are adjacent
                    If i > 0 Then
                        Assert.Equal(list(i - 1).FullSpan.End, list(i).FullSpan.Start)
                    End If
                    If i < list.Count - 1 Then
                        Assert.Equal(list(i).FullSpan.End, list(i + 1).FullSpan.Start)
                    End If
                Next
            End If
        End Sub

        ' Verify spans within a list of consecutive nodes are all consistent.
        Private Sub VerifyListSpans(list As SyntaxTriviaList, expectedFullSpan As TextSpan)
            If list.Count > 0 Then
                ' List should fill up the full span.
                Assert.Equal(expectedFullSpan.Start, list(0).FullSpan.Start)
                Assert.Equal(expectedFullSpan.End, list(list.Count - 1).FullSpan.End)

                For i = 0 To list.Count - 2
                    ' Make sure children's full spans are adjacent

                    Assert.Equal(list(i).FullSpan.End, list(i + 1).FullSpan.Start)

                Next
            End If
        End Sub


        ' Check that spans within a given tree are all consistent. Makes sure the children's spans all
        ' line up correctly.
        Private Sub VerifyAllSpans(tree As SyntaxNode)
            Assert.True(tree.FullSpan.Contains(tree.Span))

            If tree.IsStructuredTrivia Then
                ' For trivia, the full span and regular span must be equal.
                Assert.Equal(tree.Span, tree.FullSpan)
            Else
                ' For tokens and non-terminals, validate the trivia spans.
                Dim precedingTrivia = tree.GetLeadingTrivia(), followingTrivia = tree.GetTrailingTrivia()

                If precedingTrivia.Count = 0 Then
                    Assert.Equal(tree.SpanStart, tree.FullSpan.Start)
                Else
                    VerifyListSpans(precedingTrivia, New TextSpan(tree.FullSpan.Start, (tree.SpanStart - tree.FullSpan.Start)))
                End If

                If followingTrivia.Count = 0 Then
                    Assert.Equal(tree.Span.End, tree.FullSpan.End)
                Else
                    VerifyListSpans(followingTrivia, New TextSpan(tree.Span.End, (tree.FullSpan.End - tree.Span.End)))
                End If
            End If

            ' Validate the children.
            VerifyListSpans(tree.ChildNodesAndTokens(), tree.FullSpan)
        End Sub

        ' Check that spans within a given tree are all consistent. Makes sure the children's spans all
        ' line up correctly.
        Private Sub VerifyAllSpans(tree As SyntaxToken)
            Assert.True(tree.FullSpan.Contains(tree.Span))

            ' For tokens and non-terminals, validate the trivia spans.
            Dim precedingTrivia = tree.LeadingTrivia(), followingTrivia = tree.TrailingTrivia()

            If precedingTrivia.Count = 0 Then
                Assert.Equal(tree.SpanStart, tree.FullSpan.Start)
            Else
                VerifyListSpans(precedingTrivia, New TextSpan(tree.FullSpan.Start, (tree.SpanStart - tree.FullSpan.Start)))
            End If

            If followingTrivia.Count = 0 Then
                Assert.Equal(tree.Span.End, tree.FullSpan.End)
            Else
                VerifyListSpans(followingTrivia, New TextSpan(tree.Span.End, (tree.FullSpan.End - tree.Span.End)))
            End If

        End Sub

        <Fact>
        Public Sub TestSpans()
            Dim dig1 = CreateIntegerLiteral(3)
            Assert.Equal(New TextSpan(0, 1), dig1.Span)
            Assert.Equal(New TextSpan(0, 1), dig1.FullSpan)
            Dim binop = SyntaxFactory.AddExpression(
                                         CreateIntegerLiteral(4),
                                         SyntaxFactory.Token(SyntaxKind.PlusToken, trailing:=_spaceTrivia),
                                         CreateIntegerLiteral(8))
            Assert.Equal(New TextSpan(0, 4), binop.Span)
            Assert.Equal(New TextSpan(1, 1), binop.OperatorToken.Span)
            Assert.Equal(New TextSpan(1, 2), binop.OperatorToken.FullSpan)
            Assert.Equal(New TextSpan(3, 1), binop.Right.Span)
            Assert.Equal(New TextSpan(3, 1), binop.Right.FullSpan)


            Dim simpleTree = CreateSimpleTree()
            Assert.Equal(New TextSpan(0, 17), simpleTree.Span)
            Assert.Equal(New TextSpan(0, 18), simpleTree.FullSpan)
            Assert.Equal(New TextSpan(3, 14), DirectCast(simpleTree, BinaryExpressionSyntax).Right.Span)

            Dim argList = DirectCast(DirectCast(simpleTree, BinaryExpressionSyntax).Right, InvocationExpressionSyntax).ArgumentList
            Assert.Equal(New TextSpan(6, 1), argList.Arguments(0).Span)
            Assert.Equal(New TextSpan(7, 1), argList.Arguments.GetWithSeparators(1).Span)
            Assert.Equal(New TextSpan(9, 4), argList.Arguments(1).Span)
            Assert.Equal(New TextSpan(13, 1), argList.Arguments.GetWithSeparators(3).Span)
            Assert.Equal(New TextSpan(15, 1), argList.Arguments(2).Span)
        End Sub

        <Fact>
        Public Sub TestSpans2()
            Dim stmt1 = SyntaxFactory.ReturnStatement(SyntaxFactory.Token(SyntaxKind.ReturnKeyword, trailing:=_spaceTrivia), CreateIntegerLiteral(5))
            Dim stmt2 = SyntaxFactory.ReturnStatement(SyntaxFactory.Token(SyntaxKind.ReturnKeyword, trailing:=_spaceTrivia), CreateIntegerLiteral(178))
            Dim listBldr = SyntaxNodeOrTokenListBuilder.Create()
            listBldr.Add(stmt1)
            listBldr.Add(SyntaxFactory.Token(SyntaxKind.StatementTerminatorToken))
            listBldr.Add(stmt2)
            listBldr.Add(SyntaxFactory.Token(SyntaxKind.StatementTerminatorToken))

            Dim statements = listBldr.ToList
            VerifyListSpans(statements, TextSpan.FromBounds(statements(0).FullSpan.Start, statements(statements.Count - 1).FullSpan.End))

            Dim item1 = SyntaxFactory.HandlesClauseItem(SyntaxFactory.KeywordEventContainer(SyntaxFactory.Token(SyntaxKind.MeKeyword, trailing:=_spaceTrivia)), SyntaxFactory.Token(SyntaxKind.DotToken, trailing:=_spaceTrivia), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia)))
            Dim item2 = SyntaxFactory.HandlesClauseItem(SyntaxFactory.KeywordEventContainer(SyntaxFactory.Token(SyntaxKind.MeKeyword, trailing:=_spaceTrivia)), SyntaxFactory.Token(SyntaxKind.DotToken, trailing:=_spaceTrivia), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "banana", _spaceTrivia)))

            listBldr.Clear()
            listBldr.Add(item1)
            listBldr.Add(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia))
            listBldr.Add(item2)

            Dim handlesClause = SyntaxFactory.HandlesClause(SyntaxFactory.Token(SyntaxKind.HandlesKeyword, trailing:=_spaceTrivia), New SeparatedSyntaxList(Of HandlesClauseItemSyntax)(listBldr.ToList))
            VerifyAllSpans(handlesClause)

            Dim modifiedIdent1 = SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.Token(SyntaxKind.OpenParenToken, trailing:=_spaceTrivia), Nothing, SyntaxFactory.Token(SyntaxKind.CloseParenToken, trailing:=_spaceTrivia))))
            Dim modifiedIdent2 = SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "banana", _spaceTrivia), Nothing, Nothing, SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.Token(SyntaxKind.OpenParenToken, trailing:=_spaceTrivia), Nothing, SyntaxFactory.Token(SyntaxKind.CloseParenToken, trailing:=_spaceTrivia))))

            listBldr.Clear()
            listBldr.Add(modifiedIdent1)
            listBldr.Add(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia))
            listBldr.Add(modifiedIdent2)

            Dim declarator = SyntaxFactory.VariableDeclarator(New SeparatedSyntaxList(Of ModifiedIdentifierSyntax)(listBldr.ToList), Nothing, Nothing)
            VerifyAllSpans(declarator)

        End Sub

        <Fact>
        Public Sub TestSpans2_Invalid()
            ' Validate the exceptions being generated when Invalid arguments are used for a TextSpan Constructor
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                              Dim x As New TextSpan(-1, 0)
                                                          End Sub)

            Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                              Dim x As New TextSpan(0, -1)
                                                          End Sub)


            Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                              Dim x As New TextSpan(-1, -1)
                                                          End Sub)

            Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                              Dim x As New TextSpan(2, -4)
                                                          End Sub)
        End Sub


        ' Test that a list with 0 items works correctly.
        <Fact>
        Public Sub TestEmptyList()
            Dim l = New SyntaxTokenList
            Assert.Equal(0, l.Count)

            Dim attrBlock = SyntaxFactory.AttributeList(SyntaxFactory.Token(SyntaxKind.LessThanToken, trailing:=_spaceTrivia), Nothing, SyntaxFactory.Token(SyntaxKind.GreaterThanToken, trailing:=_spaceTrivia))
            Dim param = SyntaxFactory.Parameter(SyntaxFactory.SingletonList(attrBlock),
                                              l,
                                              SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia),
                                                                             Nothing, Nothing, Nothing),
                                              Nothing,
                                              Nothing)
            Assert.NotNull(param.Modifiers)
            Assert.Equal(0, param.Modifiers.Count)

            param = SyntaxFactory.Parameter(SyntaxFactory.SingletonList(attrBlock), Nothing, SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, Nothing), Nothing, Nothing)
            Assert.NotNull(param.Modifiers)
            Assert.Equal(0, param.Modifiers.Count)

        End Sub

        ' Test that list with 1 item works correctly.
        <Fact>
        Public Sub TestSingletonList()
            Dim l = New SyntaxTokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia))
            Assert.NotNull(l)
            Assert.Equal(1, l.Count)
            Assert.Equal("ByVal", l(0).ToString())
            Assert.Equal(0, l(0).SpanStart)
            Assert.Equal(5, l(0).Span.End)
            VerifyListSpans(l, New TextSpan(0, 6))

            Dim attrBlock = SyntaxFactory.AttributeList(SyntaxFactory.Token(SyntaxKind.LessThanToken, trailing:=_spaceTrivia), Nothing, SyntaxFactory.Token(SyntaxKind.GreaterThanToken, trailing:=_spaceTrivia))
            Dim param = SyntaxFactory.Parameter(SyntaxFactory.SingletonList(attrBlock), l, SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, Nothing), Nothing, Nothing)
            Assert.NotNull(param.Modifiers)
            Assert.Equal(1, param.Modifiers.Count)
            Assert.Equal("ByVal", l(0).ToString())
            Assert.Equal(4, param.Modifiers(0).SpanStart)
            Assert.Equal(9, param.Modifiers(0).Span.End)
            VerifyAllSpans(param)

            param = SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia)), SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, Nothing), Nothing, Nothing)
            Assert.NotNull(param.Modifiers)
            Assert.Equal(1, param.Modifiers.Count)
            Assert.Equal("ByVal", l(0).ToString())
            Assert.Equal(0, param.Modifiers(0).SpanStart)
            Assert.Equal(5, param.Modifiers(0).Span.End)
            VerifyAllSpans(param)
        End Sub

        ' Test list with 3 items.
        <Fact>
        Public Sub TestList()
            Dim bldr = New SyntaxTokenListBuilder(8)
            bldr.Add(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia))
            bldr.Add(SyntaxFactory.Token(SyntaxKind.OptionalKeyword, trailing:=_spaceTrivia))
            bldr.Add(SyntaxFactory.Token(SyntaxKind.ByRefKeyword, trailing:=_spaceTrivia))
            Dim l = bldr.ToList
            Assert.NotNull(l)
            Assert.Equal(3, l.Count)
            Assert.Equal("ByVal", l(0).ToString())
            Assert.Equal("Optional", l(1).ToString())
            Assert.Equal("ByRef", l(2).ToString())
            Assert.Equal(0, l(0).SpanStart)
            Assert.Equal(5, l(0).Span.End)
            Assert.Equal(6, l(1).SpanStart)
            Assert.Equal(14, l(1).Span.End)
            Assert.Equal(15, l(2).SpanStart)
            Assert.Equal(20, l(2).Span.End)
            VerifyListSpans(l, New TextSpan(0, 21))

            Dim attrBlock = SyntaxFactory.AttributeList(SyntaxFactory.Token(SyntaxKind.LessThanToken, trailing:=_spaceTrivia), Nothing, SyntaxFactory.Token(SyntaxKind.GreaterThanToken, trailing:=_spaceTrivia))
            Dim param = SyntaxFactory.Parameter(SyntaxFactory.SingletonList(attrBlock), l, SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, Nothing), Nothing, Nothing)
            Assert.NotNull(param.Modifiers)
            Assert.Equal(3, param.Modifiers.Count)
            Assert.Equal("ByVal", param.Modifiers(0).ToString())
            Assert.Equal("Optional", param.Modifiers(1).ToString())
            Assert.Equal("ByRef", param.Modifiers(2).ToString())
            Assert.Equal(4, param.Modifiers(0).SpanStart)
            Assert.Equal(9, param.Modifiers(0).Span.End)
            Assert.Equal(10, param.Modifiers(1).SpanStart)
            Assert.Equal(18, param.Modifiers(1).Span.End)
            Assert.Equal(19, param.Modifiers(2).SpanStart)
            Assert.Equal(24, param.Modifiers(2).Span.End)
            VerifyAllSpans(param)

            param = SyntaxFactory.Parameter(SyntaxFactory.SingletonList(attrBlock), SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia), SyntaxFactory.Token(SyntaxKind.OptionalKeyword, trailing:=_spaceTrivia), SyntaxFactory.Token(SyntaxKind.ByRefKeyword, trailing:=_spaceTrivia)), SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, Nothing), Nothing, Nothing)
            Assert.NotNull(param.Modifiers)
            Assert.Equal(3, param.Modifiers.Count)
            Assert.Equal("ByVal", param.Modifiers(0).ToString())
            Assert.Equal("Optional", param.Modifiers(1).ToString())
            Assert.Equal("ByRef", param.Modifiers(2).ToString())
            Assert.Equal(4, param.Modifiers(0).SpanStart)
            Assert.Equal(9, param.Modifiers(0).Span.End)
            Assert.Equal(10, param.Modifiers(1).SpanStart)
            Assert.Equal(18, param.Modifiers(1).Span.End)
            Assert.Equal(19, param.Modifiers(2).SpanStart)
            Assert.Equal(24, param.Modifiers(2).Span.End)
            VerifyAllSpans(param)

            param = SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia), SyntaxFactory.Token(SyntaxKind.OptionalKeyword, trailing:=_spaceTrivia), SyntaxFactory.Token(SyntaxKind.ByRefKeyword, trailing:=_spaceTrivia)), SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "goo", _spaceTrivia), Nothing, Nothing, Nothing), Nothing, Nothing)
            VerifyAllSpans(param)
        End Sub

        ' Helper to create a type name from a simple string.
        Private Function CreateSimpleTypeName(id As String) As TypeSyntax
            Return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, id, SyntaxFactory.WhitespaceTrivia(" ")))
        End Function


        'helper to check an empty separated list
        Private Sub CheckEmptySeparatedList(seplist As SeparatedSyntaxList(Of TypeSyntax))
            Assert.Equal(0, seplist.Count)
            Assert.Equal(0, seplist.SeparatorCount)
        End Sub

        ' Check that empty separated list works.
        <Fact>
        Public Sub TestEmptySeparatedList()
            CheckEmptySeparatedList(New SeparatedSyntaxList(Of TypeSyntax)(DirectCast(Nothing, VisualBasicSyntaxNode), 0))
            Dim statement = SyntaxFactory.InheritsStatement(SyntaxFactory.Token(SyntaxKind.InheritsKeyword, trailing:=_spaceTrivia), (New SeparatedSyntaxListBuilder(Of TypeSyntax)).ToList)
            CheckEmptySeparatedList(statement.Types)

            Dim arglist = SyntaxFactory.ArgumentList(SyntaxFactory.Token(SyntaxKind.OpenParenToken, trailing:=_spaceTrivia), Nothing, SyntaxFactory.Token(SyntaxKind.CloseParenToken, trailing:=_spaceTrivia))
            Assert.NotNull(arglist.Arguments)
            Assert.Equal(0, arglist.Arguments.Count)
            Assert.Equal(0, arglist.Arguments.SeparatorCount)
            Assert.Equal("( )", arglist.ToString)
            Assert.Equal("( ) ", arglist.ToFullString)
        End Sub

        'helper to check a singleton separated list of one type name "goo"
        Private Sub CheckSingletonSeparatedList(seplist As SeparatedSyntaxList(Of TypeSyntax), start As Integer)
            Assert.NotNull(seplist)
            Assert.Equal(1, seplist.Count)
            Assert.Equal("goo", seplist(0).ToString)
            Assert.Equal("goo ", seplist(0).ToFullString)
            Assert.Equal(start, seplist(0).SpanStart)
            Assert.Equal(start, seplist(0).FullSpan.Start)
            Assert.Equal(start + 3, seplist(0).Span.End)
            Assert.Equal(start + 4, seplist(0).FullSpan.End)

            Assert.Equal(0, seplist.SeparatorCount)
        End Sub

        ' Check that singleton separated list works
        <Fact>
        Public Sub TestSingletonSeparatedList()
            CheckSingletonSeparatedList(New SeparatedSyntaxList(Of TypeSyntax)(New SyntaxNodeOrTokenList(CreateSimpleTypeName("goo"), 0)), 0)
            Dim bldr = SeparatedSyntaxListBuilder(Of TypeSyntax).Create()
            bldr.Add(CreateSimpleTypeName("goo"))
            Dim statement = SyntaxFactory.InheritsStatement(SyntaxFactory.Token(SyntaxKind.InheritsKeyword, trailing:=_spaceTrivia), bldr.ToList)
            CheckSingletonSeparatedList(statement.Types, 9)
            Assert.Equal("Inherits goo", statement.ToString)
            Assert.Equal("Inherits goo ", statement.ToFullString)
        End Sub

        ' Check that separated list with separators in it works.
        <Fact>
        Public Sub TestSeparatedList()
            Dim bldr = SeparatedSyntaxListBuilder(Of TypeSyntax).Create()
            bldr.Add(CreateSimpleTypeName("aaa"))
            bldr.AddSeparator(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia))
            bldr.Add(CreateSimpleTypeName("bbb"))
            bldr.AddSeparator(SyntaxFactory.Token(SyntaxKind.SemicolonToken, trailing:=_spaceTrivia))
            bldr.Add(CreateSimpleTypeName("cc"))

            Dim sepList = bldr.ToList

            Assert.Equal(3, sepList.Count)
            Assert.Equal(2, sepList.SeparatorCount)

            Assert.Null(sepList(1).Parent)
            Assert.Null(sepList.GetWithSeparators(3).Parent)

            Assert.Equal("aaa", sepList(0).ToString)
            Assert.Equal("bbb", sepList(1).ToString)
            Assert.Equal("cc", sepList(2).ToString)
            Assert.Equal(",", sepList.GetWithSeparators(1).ToString)
            Assert.Equal(";", sepList.GetWithSeparators(3).ToString)

            Assert.Equal(0, sepList(0).SpanStart)
            Assert.Equal(4, sepList.GetWithSeparators(1).SpanStart)
            Assert.Equal(6, sepList(1).SpanStart)
            Assert.Equal(10, sepList.GetWithSeparators(3).SpanStart)
            Assert.Equal(12, sepList(2).SpanStart)

            Dim statement = SyntaxFactory.InheritsStatement(SyntaxFactory.Token(SyntaxKind.InheritsKeyword, trailing:=_spaceTrivia), sepList)
            Assert.Equal("Inherits aaa , bbb ; cc", statement.ToString)
            Assert.Equal("Inherits aaa , bbb ; cc ", statement.ToFullString)
            VerifyAllSpans(statement)

            sepList = statement.Types
            Assert.Equal(statement, sepList(1).Parent)
            Assert.Equal(statement, sepList.GetWithSeparators(3).Parent)

            Assert.Equal(3, sepList.Count)
            Assert.Equal(2, sepList.SeparatorCount)

            Assert.Equal("aaa", sepList(0).ToString)
            Assert.Equal("bbb", sepList(1).ToString)
            Assert.Equal("cc", sepList(2).ToString)
            Assert.Equal(",", sepList.GetWithSeparators(1).ToString)
            Assert.Equal(";", sepList.GetWithSeparators(3).ToString)

            Assert.Equal(9 + 0, sepList(0).SpanStart)
            Assert.Equal(9 + 4, sepList.GetWithSeparators(1).SpanStart)
            Assert.Equal(9 + 6, sepList(1).SpanStart)
            Assert.Equal(9 + 10, sepList.GetWithSeparators(3).SpanStart)
            Assert.Equal(9 + 12, sepList(2).SpanStart)
        End Sub

        ' Check that trivia seems to work.
        ' Note that whitespace constructor allows any text, so we leverage that in this test for simplicity.
        <Fact>
        Public Sub TestTrivia()
            Dim white_a = SyntaxFactory.WhitespaceTrivia("AAA")
            Dim white_b = SyntaxFactory.WhitespaceTrivia("B")
            Dim white_c = SyntaxFactory.WhitespaceTrivia("CCCC")
            Dim white_d = SyntaxFactory.WhitespaceTrivia("DD")

            Dim tok = SyntaxFactory.Token(SyntaxKind.PlusToken)
            Dim precTrivia = tok.LeadingTrivia()
            Dim follTrivia = tok.TrailingTrivia()
            Assert.NotNull(precTrivia)
            Assert.Equal(1, precTrivia.Count)
            Assert.NotNull(follTrivia)
            Assert.Equal(1, follTrivia.Count)
            Assert.Equal(0, tok.FullSpan.Start)
            Assert.Equal(0, tok.SpanStart)
            Assert.Equal(1, tok.FullSpan.End)
            Assert.Equal(1, tok.Span.End)
            VerifyAllSpans(tok)

            Dim bldr = SyntaxTriviaListBuilder.Create()
            bldr.Add(white_a)
            bldr.Add(white_b)
            tok = SyntaxFactory.Token(Nothing, SyntaxKind.PlusToken, trailing:=bldr.ToList)
            precTrivia = tok.LeadingTrivia()
            follTrivia = tok.TrailingTrivia()
            Assert.NotNull(precTrivia)
            Assert.Equal(0, precTrivia.Count)
            Assert.NotNull(follTrivia)
            Assert.Equal(2, follTrivia.Count)
            Assert.Equal(0, tok.FullSpan.Start)
            Assert.Equal(0, tok.SpanStart)
            Assert.Equal(5, tok.FullSpan.End)
            Assert.Equal(1, tok.Span.End)
            Assert.Equal("+AAAB", tok.ToFullString())
            Assert.Equal("AAA", follTrivia(0).ToString())
            Assert.Equal("AAA", follTrivia(0).ToFullString)
            Assert.Equal(1, follTrivia(0).SpanStart)
            Assert.Equal(1, follTrivia(0).FullSpan.Start)
            Assert.Equal(4, follTrivia(0).Span.End)
            Assert.Equal(4, follTrivia(0).FullSpan.End)
            Assert.Equal("B", follTrivia(1).ToString())
            Assert.Equal("B", follTrivia(1).ToFullString)
            Assert.Equal(4, follTrivia(1).SpanStart)
            Assert.Equal(4, follTrivia(1).FullSpan.Start)
            Assert.Equal(5, follTrivia(1).Span.End)
            Assert.Equal(5, follTrivia(1).FullSpan.End)
            VerifyAllSpans(tok)

            bldr.Clear()
            bldr.Add(white_c)
            bldr.Add(white_d)
            bldr.Add(white_a)
            Dim leading = bldr.ToList
            tok = SyntaxFactory.Token(bldr.ToList, SyntaxKind.PlusToken, trailing:=SyntaxTriviaList.Create(white_b))
            precTrivia = tok.LeadingTrivia()
            follTrivia = tok.TrailingTrivia()
            Assert.Equal(0, tok.FullSpan.Start)
            Assert.Equal(11, tok.FullSpan.End)
            Assert.Equal(9, tok.SpanStart)
            Assert.Equal(10, tok.Span.End)
            Assert.Equal("CCCCDDAAA+B", tok.ToFullString())
            VerifyAllSpans(tok)

            Assert.NotNull(precTrivia)
            Assert.Equal(3, precTrivia.Count)
            Assert.Equal("CCCC", precTrivia(0).ToString())
            Assert.Equal("CCCC", precTrivia(0).ToFullString)
            Assert.Equal(0, precTrivia(0).SpanStart)
            Assert.Equal(0, precTrivia(0).FullSpan.Start)
            Assert.Equal(4, precTrivia(0).Span.End)
            Assert.Equal(4, precTrivia(0).FullSpan.End)
            Assert.Equal("DD", precTrivia(1).ToString())
            Assert.Equal("DD", precTrivia(1).ToFullString)
            Assert.Equal(4, precTrivia(1).SpanStart)
            Assert.Equal(4, precTrivia(1).FullSpan.Start)
            Assert.Equal(6, precTrivia(1).Span.End)
            Assert.Equal(6, precTrivia(1).FullSpan.End)
            Assert.Equal("AAA", precTrivia(2).ToString())
            Assert.Equal("AAA", precTrivia(2).ToFullString)
            Assert.Equal(6, precTrivia(2).SpanStart)
            Assert.Equal(6, precTrivia(2).FullSpan.Start)
            Assert.Equal(9, precTrivia(2).Span.End)
            Assert.Equal(9, precTrivia(2).FullSpan.End)

            Assert.NotNull(follTrivia)
            Assert.Equal(1, follTrivia.Count)
            Assert.Equal("B", follTrivia(0).ToString())
            Assert.Equal("B", follTrivia(0).ToFullString)
            Assert.Equal(10, follTrivia(0).SpanStart)
            Assert.Equal(10, follTrivia(0).FullSpan.Start)
            Assert.Equal(11, follTrivia(0).Span.End)
            Assert.Equal(11, follTrivia(0).FullSpan.End)
        End Sub

        <Fact>
        Public Sub TestKeywordFactoryMethods()
            ' Check simple factory for keyword.
            Dim keyword = SyntaxFactory.Token(SyntaxKind.AliasKeyword, trailing:=_spaceTrivia)
            Assert.Equal("Alias", keyword.ToString())
            Assert.Equal(5, keyword.Span.Length)
            Assert.Equal(6, keyword.FullSpan.Length)
            Assert.Equal(1, keyword.LeadingTrivia().Count)
            Assert.Equal(1, keyword.TrailingTrivia().Count)
            Assert.Equal(" ", keyword.TrailingTrivia()(0).ToString)

            ' Check full factory for keyword
            Dim bldr = SyntaxTriviaListBuilder.Create()
            bldr.Add(SyntaxFactory.WhitespaceTrivia("   "))
            bldr.Add(SyntaxFactory.CommentTrivia("'goo"))
            keyword = SyntaxFactory.Token(bldr.ToList, SyntaxKind.AliasKeyword, Nothing, "ALIAs")
            Assert.Equal("ALIAs", keyword.ToString())
            Assert.Equal(5, keyword.Span.Length)
            Assert.Equal(12, keyword.FullSpan.Length)
            Assert.Equal(2, keyword.LeadingTrivia().Count)
            Assert.Equal("   ", keyword.LeadingTrivia()(0).ToString)
            Assert.Equal(0, keyword.TrailingTrivia().Count)

            ' Check factory methods giving the node kind
            keyword = SyntaxFactory.Token(Nothing, SyntaxKind.AndAlsoKeyword, _spaceTrivia, "ANDALSO")
            Assert.Equal("ANDALSO", keyword.ToString())
            Assert.Equal(7, keyword.Span.Length)
            Assert.Equal(8, keyword.FullSpan.Length)
            Assert.Equal(0, keyword.LeadingTrivia().Count)
            Assert.Equal(1, keyword.TrailingTrivia().Count)
            Assert.Equal(" ", keyword.TrailingTrivia()(0).ToString)

            bldr.Clear()
            bldr.Add(SyntaxFactory.WhitespaceTrivia("   "))
            bldr.Add(SyntaxFactory.CommentTrivia("'goo"))
            keyword = SyntaxFactory.Token(bldr.ToList, SyntaxKind.AndAlsoKeyword, SyntaxTriviaList.Create(SyntaxFactory.WhitespaceTrivia("  ")), "andalso")
            Assert.Equal("andalso", keyword.ToString())
            Assert.Equal(7, keyword.Span.Length)
            Assert.Equal(16, keyword.FullSpan.Length)
            Assert.Equal(2, keyword.LeadingTrivia().Count)
            Assert.Equal(1, keyword.TrailingTrivia().Count)
            Assert.Equal("  ", keyword.TrailingTrivia()(0).ToString)
            Assert.Equal("'goo", keyword.LeadingTrivia()(1).ToString)
        End Sub

        <Fact>
        Public Sub TestNonTerminalFactoryMethods()
            Dim endTry As EndBlockStatementSyntax

            endTry = SyntaxFactory.EndTryStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword, trailing:=_spaceTrivia), SyntaxFactory.Token(SyntaxKind.TryKeyword, trailing:=_spaceTrivia))
            Assert.Equal(7, endTry.Span.Length)
            Assert.Equal(8, endTry.FullSpan.Length)
            Assert.Equal(SyntaxKind.EndKeyword, endTry.EndKeyword.Kind)
            Assert.Equal("End", endTry.EndKeyword.ToString())
            Assert.Equal(SyntaxKind.TryKeyword, endTry.BlockKeyword.Kind)
            Assert.Equal("Try", endTry.BlockKeyword.ToString())
            Assert.Equal(1, endTry.GetTrailingTrivia().Count)
            Assert.Equal(" ", endTry.GetTrailingTrivia()(0).ToString)
        End Sub

        ' Check that IsToken, IsTrivia, IsTerminal properties returns correct thing.
        <Fact>
        Public Sub TestTokenTriviaClassification()
            Dim endIfStmt = SyntaxFactory.EndIfStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword, trailing:=_spaceTrivia), SyntaxFactory.Token(SyntaxKind.IfKeyword, trailing:=_spaceTrivia))
            Dim plusToken = SyntaxFactory.Token(SyntaxKind.PlusToken, trailing:=_spaceTrivia)
            Dim comment = SyntaxFactory.CommentTrivia("'hello")

            Assert.True(plusToken.Node.IsToken)
            Assert.False(comment.UnderlyingNode.IsToken)

            Assert.False(endIfStmt.IsStructuredTrivia)
            Assert.Equal(SyntaxKind.CommentTrivia, comment.Kind)
        End Sub

        ' Check that ToString, ToFullString, and ValueText are correct on a token.
        <Fact>
        Public Sub TestTokenText()
            ' keyword without trivia
            Dim keyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword, "ParTIAL")
            Assert.Equal("ParTIAL", keyword.ToString())
            Assert.Equal("ParTIAL", keyword.ToFullString())
            Assert.Equal("ParTIAL", keyword.ValueText())

            ' identifier with trivia
            Dim identifier = SyntaxFactory.Identifier(SyntaxFactory.WhitespaceTrivia("   "), "[goo]", True, "goo", TypeCharacter.None,
                                                   SyntaxFactory.CommentTrivia("'hi"))

            Assert.Equal("[goo]", identifier.ToString())
            Assert.Equal("   [goo]'hi", identifier.ToFullString())
            Assert.Equal("goo", identifier.ValueText)
        End Sub

        ' Create a sample method statement.
        Private Function CreateMethodStatement() As MethodStatementSyntax

            Dim bldr = SyntaxNodeOrTokenListBuilder.Create()
            bldr.Add(SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia)),
                                                                     SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "Param1", False, "Param1", TypeCharacter.None, _spaceTrivia), Nothing, Nothing, Nothing),
                                                                     SyntaxFactory.SimpleAsClause(SyntaxFactory.Token(SyntaxKind.AsKeyword, trailing:=_spaceTrivia), Nothing, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword, trailing:=_spaceTrivia))), Nothing))
            bldr.Add(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia))
            bldr.Add(SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia)),
                                                                     SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "Param2", False, "Param2", TypeCharacter.None, _spaceTrivia), Nothing, Nothing, Nothing),
                                                                     SyntaxFactory.SimpleAsClause(SyntaxFactory.Token(SyntaxKind.AsKeyword, trailing:=_spaceTrivia), Nothing, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword, trailing:=_spaceTrivia))), Nothing))
            bldr.Add(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia))
            bldr.Add(SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByRefKeyword, trailing:=_spaceTrivia)),
                                                                     SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "Param3", False, "Param3", TypeCharacter.None, _spaceTrivia), Nothing, Nothing, Nothing),
                                                                     SyntaxFactory.SimpleAsClause(SyntaxFactory.Token(SyntaxKind.AsKeyword, trailing:=_spaceTrivia), Nothing, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword, trailing:=_spaceTrivia))), Nothing))

            Return SyntaxFactory.SubStatement(Nothing,
                                      SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxFactory.WhitespaceTrivia("         "), SyntaxKind.PublicKeyword, SyntaxFactory.WhitespaceTrivia(" "), "public"),
                                          SyntaxFactory.Token(SyntaxKind.OverloadsKeyword, trailing:=_spaceTrivia)),
                                      SyntaxFactory.Token(SyntaxFactory.WhitespaceTrivia("     "), SyntaxKind.SubKeyword, SyntaxFactory.WhitespaceTrivia("   "), "SUB"),
                                      SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "MySub", False, "MySub", TypeCharacter.None, _spaceTrivia),
                                      Nothing,
                                      SyntaxFactory.ParameterList(SyntaxFactory.Token(SyntaxKind.OpenParenToken, trailing:=_spaceTrivia),
                                          New SeparatedSyntaxList(Of ParameterSyntax)(bldr.ToList()),
                                          SyntaxFactory.Token(SyntaxKind.CloseParenToken, trailing:=_spaceTrivia)),
                                      Nothing, Nothing, Nothing)
        End Function

        <Fact>
        Public Sub TestSpansOnMethodStatement()
            VerifyAllSpans(CreateMethodStatement())
        End Sub

        ' Check that the ToString and ToFullString are correct on a non-terminal that includes
        ' a list and a separated list.
        <Fact>
        Public Sub TestNonTerminalText()
            Dim methodStatement = CreateMethodStatement()

            Assert.Equal("public Overloads      SUB   MySub ( ByVal Param1 As Integer , ByVal Param2 As String , ByRef Param3 As Double )",
                            methodStatement.ToString())
            Assert.Equal("         public Overloads      SUB   MySub ( ByVal Param1 As Integer , ByVal Param2 As String , ByRef Param3 As Double ) ",
                            methodStatement.ToFullString)
        End Sub

        ' Check that IsMissing seems to do the right thing.
        <Fact>
        Public Sub IsMissing()
            Dim ident = SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "hello", _spaceTrivia)
            Assert.False(ident.IsMissing)
            ident = SyntaxFactory.MissingIdentifier()
            Assert.True(ident.IsMissing)

            Dim punc = SyntaxFactory.Token(SyntaxKind.PlusToken, trailing:=_spaceTrivia)
            Assert.False(punc.IsMissing)
            punc = SyntaxFactory.MissingPunctuation(SyntaxKind.PlusToken)
            Assert.True(punc.IsMissing)

            Dim kw = SyntaxFactory.Token(SyntaxKind.EndKeyword, trailing:=_spaceTrivia)
            Assert.False(kw.IsMissing)
            kw = SyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword)
            Assert.True(kw.IsMissing)

            Dim eof = SyntaxFactory.Token(SyntaxKind.EndOfFileToken)
            Assert.False(eof.IsMissing)  ' end of text token is never missing, even though it is zero length.
            Assert.Equal(0, eof.Span.Length)

            Dim endIfStmt = SyntaxFactory.EndIfStatement(SyntaxFactory.MissingKeyword(SyntaxKind.EndKeyword), SyntaxFactory.MissingKeyword(SyntaxKind.IfKeyword))
            Assert.True(endIfStmt.IsMissing)
            Assert.Equal(0, endIfStmt.Span.Length)
            VerifyAllSpans(endIfStmt)
        End Sub

        Private Function CompareDiagnostics(err1 As Diagnostic, err2 As Diagnostic) As Integer
            Dim span1 = err1.Location.SourceSpan
            Dim span2 = err2.Location.SourceSpan
            Dim i = span1.Start.CompareTo(span2.Start)
            If i = 0 Then
                Return err1.Code.CompareTo(err2.Code)
            End If
            Return i
        End Function

        ' Check that a given list of errors on a node matches the given set.
        Private Sub CheckErrorList(node As VisualBasicSyntaxNode, expectedErrorCodes As Integer(), expectedSpans As TextSpan())
            Dim errorList As New List(Of Diagnostic)
            errorList.AddRange(node.GetSyntaxErrorsNoTree())
            errorList.Sort(AddressOf CompareDiagnostics)

            For i = 0 To errorList.Count - 1
                Assert.True(expectedSpans(i) =
                            errorList(i).Location.SourceSpan, "Error " & i & " have different spans")
                Assert.True(expectedErrorCodes(i) = errorList(i).Code, "Error " & i & " have different codes")
            Next

            Assert.Equal(expectedErrorCodes.Length, errorList.Count)

            ' Has errors property should match expected errors.
            If expectedErrorCodes.Length > 0 Then
                Assert.True(node.ContainsDiagnostics)
            Else
                Assert.False(node.ContainsDiagnostics)
            End If
        End Sub

        ' Check that a given list of errors on a node matches the given set.
        Private Sub CheckErrorList(node As SyntaxToken, expectedErrorCodes As Integer(), expectedSpans As TextSpan())
            Dim errorList As New List(Of Diagnostic)
            errorList.AddRange(node.GetSyntaxErrorsNoTree())
            errorList.Sort(AddressOf CompareDiagnostics)

            For i = 0 To errorList.Count - 1
                Assert.True(expectedSpans(i) =
                            errorList(i).Location.SourceSpan, "Error " & i & " have different spans")
                Assert.True(expectedErrorCodes(i) = errorList(i).Code, "Error " & i & " have different codes")
            Next

            Assert.Equal(expectedErrorCodes.Length, errorList.Count)

            ' Has errors property should match expected errors.
            If expectedErrorCodes.Length > 0 Then
                Assert.True(node.Node.ContainsDiagnostics)
            Else
                Assert.False(node.Node.ContainsDiagnostics)
            End If
        End Sub

        ' Test simple errors on a token and its associated trivia.
        <Fact>
        Public Sub SimpleTokenErrors()
            Dim kwModule = SyntaxFactory.Token(SyntaxKind.ModuleKeyword, trailing:=_spaceTrivia)
            CheckErrorList(kwModule, {}, {})
            Assert.Equal(6, kwModule.Span.Length)
            Assert.Equal(7, kwModule.FullSpan.Length)

            ' Add an error.
            kwModule = New SyntaxToken(Nothing, CType(kwModule.Node.AddError(CreateDiagnosticInfo(17)), InternalSyntax.KeywordSyntax), 0, 0)
            CheckErrorList(kwModule, {17}, {New TextSpan(0, 6)})
            Assert.Equal(6, kwModule.Span.Length)
            Assert.Equal(7, kwModule.FullSpan.Length)

            ' Add another error.
            kwModule = New SyntaxToken(Nothing, CType(kwModule.Node.AddError(CreateDiagnosticInfo(42)), InternalSyntax.KeywordSyntax), 0, 0)
            CheckErrorList(kwModule, {17, 42}, {New TextSpan(0, 6), New TextSpan(0, 6)})

            ' Add another token and put together. Make sure the spans work.
            Dim trailing = New SyntaxTrivia(Nothing, CType(SyntaxFactory.WhitespaceTrivia("   ").UnderlyingNode.AddError(CreateDiagnosticInfo(101)), InternalSyntax.SyntaxTrivia), 0, 0)
            Dim kwEnd = SyntaxFactory.Token(Nothing, SyntaxKind.EndKeyword, trailing, "End")
            Dim endModule = SyntaxFactory.EndModuleStatement(kwEnd, kwModule)
            CheckErrorList(endModule, {101, 17, 42}, {New TextSpan(3, 3), New TextSpan(6, 6), New TextSpan(6, 6)})

            ' add error to the whole statement
            endModule = CType(endModule.AddError(CreateDiagnosticInfo(1)), EndBlockStatementSyntax)
            Assert.Equal("End   Module ", endModule.ToFullString)
            CheckErrorList(endModule, {1, 101, 17, 42}, {New TextSpan(0, 12), New TextSpan(3, 3), New TextSpan(6, 6), New TextSpan(6, 6)})

        End Sub

        ' Test a complex case with a few errors in it.
        <Fact>
        Public Sub ComplexErrors()
            Dim bldr = SyntaxNodeOrTokenListBuilder.Create()
            bldr.Add(SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia)),
                                                                     SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "Param1", False, "Param1", TypeCharacter.None, _spaceTrivia), Nothing, Nothing, Nothing),
                                                                     SyntaxFactory.SimpleAsClause(SyntaxFactory.Token(SyntaxKind.AsKeyword, trailing:=_spaceTrivia), Nothing, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword, trailing:=_spaceTrivia))), Nothing))
            bldr.Add(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia))
            bldr.Add(SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ByValKeyword, trailing:=_spaceTrivia)),
                                                                     SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "Param2", False, "Param2", TypeCharacter.None, _spaceTrivia), Nothing, Nothing, Nothing),
                                                                     SyntaxFactory.SimpleAsClause(SyntaxFactory.Token(SyntaxKind.AsKeyword, trailing:=_spaceTrivia), Nothing, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword, trailing:=_spaceTrivia))), Nothing).AddError(CreateDiagnosticInfo(101)))
            bldr.Add(DirectCast(SyntaxFactory.Token(SyntaxKind.CommaToken, trailing:=_spaceTrivia).Node.AddError(CreateDiagnosticInfo(33)), InternalSyntax.VisualBasicSyntaxNode))
            bldr.Add(SyntaxFactory.Parameter(Nothing, SyntaxFactory.TokenList(New SyntaxToken(Nothing, CType(SyntaxFactory.Token(SyntaxKind.ByRefKeyword, trailing:=_spaceTrivia).Node.AddError(CreateDiagnosticInfo(44)), InternalSyntax.KeywordSyntax), 0, 0)),
                                                                     SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "Param3", False, "Param3", TypeCharacter.None, _spaceTrivia), Nothing, Nothing, Nothing),
                                                                     SyntaxFactory.SimpleAsClause(SyntaxFactory.Token(SyntaxKind.AsKeyword, trailing:=_spaceTrivia), Nothing, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword, trailing:=_spaceTrivia))), Nothing))

            Dim methodDecl As MethodStatementSyntax =
                SyntaxFactory.SubStatement(Nothing,
                                      SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxFactory.WhitespaceTrivia("         "), SyntaxKind.PublicKeyword, SyntaxFactory.WhitespaceTrivia(" "), "public"),
                                          SyntaxFactory.Token(SyntaxKind.OverloadsKeyword, trailing:=_spaceTrivia)),
                                      SyntaxFactory.Token(New SyntaxTrivia(Nothing, CType(SyntaxFactory.WhitespaceTrivia("     ").UnderlyingNode.AddError(CreateDiagnosticInfo(22)).AddError(CreateDiagnosticInfo(23)), InternalSyntax.SyntaxTrivia), 0, 0), SyntaxKind.SubKeyword, SyntaxFactory.WhitespaceTrivia("   "), "SUB"),
                                      SyntaxFactory.Identifier(SyntaxFactory.ElasticMarker, "MySub", False, "MySyb", TypeCharacter.None, _spaceTrivia),
                                      Nothing,
                                      SyntaxFactory.ParameterList(SyntaxFactory.Token(SyntaxKind.OpenParenToken, trailing:=_spaceTrivia),
                                          New SeparatedSyntaxList(Of ParameterSyntax)(bldr.ToList),
                                          SyntaxFactory.Token(SyntaxKind.CloseParenToken, trailing:=_spaceTrivia)),
                                      Nothing, Nothing, Nothing)

            Assert.Equal("         public Overloads      SUB   MySub ( ByVal Param1 As Integer , ByVal Param2 As String , ByRef Param3 As Double ) ",
                            methodDecl.ToFullString)

            CheckErrorList(methodDecl, {22, 23, 101, 33, 44},
                           {New TextSpan(26, 5), New TextSpan(26, 5), New TextSpan(71, 22), New TextSpan(94, 1), New TextSpan(96, 5)})
        End Sub

        Private Shared ReadOnly s_messageProvider As New MockMessageProvider()

        Private Function CreateDiagnosticInfo(code As Integer) As DiagnosticInfo
            Return New DiagnosticInfo(s_messageProvider, code)
        End Function

        ' A mock message provider
        Private NotInheritable Class MockMessageProvider
            Inherits TestMessageProvider

            Public Overrides ReadOnly Property CodePrefix As String
                Get
                    Return "MOCK"
                End Get
            End Property

            Public Overrides Function GetSeverity(code As Integer) As DiagnosticSeverity
                Return DiagnosticSeverity.Error
            End Function

            Public Overrides Function LoadMessage(code As Integer, language As CultureInfo) As String
                Return String.Format("Error {0}", code)
            End Function

            Public Overrides Function GetDescription(code As Integer) As LocalizableString
                Return String.Empty
            End Function

            Public Overrides Function GetTitle(code As Integer) As LocalizableString
                Return String.Empty
            End Function

            Public Overrides Function GetMessageFormat(code As Integer) As LocalizableString
                Return String.Empty
            End Function

            Public Overrides Function GetHelpLink(code As Integer) As String
                Return String.Empty
            End Function

            Public Overrides Function GetCategory(code As Integer) As String
                Return String.Empty
            End Function

            Public Overrides Function GetWarningLevel(code As Integer) As Integer
                Return 0
            End Function

            Public Overrides Function GetErrorDisplayString(symbol As ISymbol) As String
                Return MessageProvider.Instance.GetErrorDisplayString(symbol)
            End Function

            Public Overrides Function GetIsEnabledByDefault(code As Integer) As Boolean
                Return True
            End Function

#If DEBUG Then
            Friend Overrides Function ShouldAssertExpectedMessageArgumentsLength(errorCode As Integer) As Boolean
                Return False
            End Function
#End If
        End Class

        ' A test rewriting visitor
        Private Class TestVisitor
            Inherits VisualBasicSyntaxRewriter

            ' Optional to control which rewritings we do
            Public IncrementInts As Boolean = False
            Public CapitalizeKeywords As Boolean = False
            Public CapitalizeIdentifiers As Boolean = False
            Public SwapParameters As Boolean = False

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Select Case token.Kind
                    Case SyntaxKind.IntegerLiteralToken
                        If IncrementInts Then
                            Dim node = DirectCast(token.Node, InternalSyntax.IntegerLiteralTokenSyntax)
                            Dim value = CULng(node.ObjectValue)
                            value = CULng(value + 1)
                            Return SyntaxFactory.IntegerLiteralToken(token.LeadingTrivia, value.ToString(), LiteralBase.Decimal, node.TypeSuffix, value, token.TrailingTrivia())
                        End If

                    Case SyntaxKind.IdentifierToken
                        If CapitalizeIdentifiers Then
                            Dim node = DirectCast(token.Node, InternalSyntax.IdentifierTokenSyntax)
                            Return SyntaxFactory.Identifier(token.LeadingTrivia(), node.Text.ToUpperInvariant(), node.IsBracketed, node.IdentifierText.ToUpperInvariant(),
                                                          node.TypeCharacter, token.TrailingTrivia())
                        End If

                    Case Else
                        If SyntaxFacts.IsKeywordKind(token.Kind) Then
                            If CapitalizeKeywords Then
                                Dim node = DirectCast(token.Node, InternalSyntax.KeywordSyntax)
                                Return SyntaxFactory.Token(token.LeadingTrivia(), node.Kind, token.TrailingTrivia(), node.Text.ToUpperInvariant())
                            End If
                        End If
                End Select

                Return MyBase.VisitToken(token)
            End Function

            Public Overrides Function VisitMethodStatement(node As MethodStatementSyntax) As SyntaxNode
                If SwapParameters Then
                    node = CType(MyBase.VisitMethodStatement(node), MethodStatementSyntax)
                    Return SyntaxFactory.MethodStatement(node.Kind,
                                                         node.AttributeLists,
                                                         node.Modifiers,
                                                         node.DeclarationKeyword,
                                                         node.Identifier,
                                                         node.TypeParameterList,
                                                         SyntaxFactory.ParameterList(
                                                             node.ParameterList.OpenParenToken,
                                                             SwitchParameters(node.ParameterList.Parameters),
                                                             node.ParameterList.CloseParenToken),
                                                         node.AsClause,
                                                         node.HandlesClause,
                                                         node.ImplementsClause)
                Else
                    Return MyBase.VisitMethodStatement(node)
                End If
            End Function

            Private Function SwitchParameters(params As SeparatedSyntaxList(Of ParameterSyntax)) As SeparatedSyntaxList(Of ParameterSyntax)
                If params.Count >= 2 Then
                    Dim bldr = SeparatedSyntaxListBuilder(Of ParameterSyntax).Create()
                    bldr.Add(params(1))
                    bldr.AddSeparator(params.GetSeparator(0))
                    bldr.Add(params(0))
                    bldr.AddSeparator(params.GetSeparator(1))

                    For i As Integer = 2 To params.Count - 1
                        bldr.Add(params(i))
                        If i < params.SeparatorCount Then
                            bldr.AddSeparator(params.GetSeparator(i))
                        End If
                    Next

                    Return bldr.ToList
                Else
                    Return params
                End If
            End Function

        End Class

        <Fact>
        Public Sub TestRewritingVisitor()
            Dim rewriter As VisualBasicSyntaxRewriter
            Dim simpleTree = CreateSimpleTree()
            Assert.Equal(simpleTree.ToString, "1- X( 3, 4+ 8, 9)")

            ' null rewriting should return exact same node.
            rewriter = New TestVisitor()
            Dim newTree = rewriter.Visit(simpleTree)
            Assert.Same(simpleTree, newTree)

            ' incremental all integers
            rewriter = New TestVisitor() With {.IncrementInts = True}
            newTree = rewriter.Visit(simpleTree)
            Assert.Equal(newTree.ToString, "2- X( 4, 5+ 9, 10)")

            Dim methodStmt = CreateMethodStatement()
            Assert.Equal("public Overloads      SUB   MySub ( ByVal Param1 As Integer , ByVal Param2 As String , ByRef Param3 As Double )", methodStmt.ToString)

            ' null rewriting should return exact same node.
            rewriter = New TestVisitor()
            Dim newMethod = rewriter.Visit(methodStmt)
            Assert.Same(methodStmt, newMethod)

            ' capitalize all keywords.
            rewriter = New TestVisitor() With {.CapitalizeKeywords = True}
            newMethod = rewriter.Visit(methodStmt)
            Assert.Equal("PUBLIC OVERLOADS      SUB   MySub ( BYVAL Param1 AS INTEGER , BYVAL Param2 AS STRING , BYREF Param3 AS DOUBLE )", newMethod.ToString)

            ' capitalize all identifiers.
            rewriter = New TestVisitor() With {.CapitalizeIdentifiers = True}
            newMethod = rewriter.Visit(methodStmt)
            Assert.Equal("public Overloads      SUB   MYSUB ( ByVal PARAM1 As Integer , ByVal PARAM2 As String , ByRef PARAM3 As Double )", newMethod.ToString)

            ' reorder parameters.
            rewriter = New TestVisitor() With {.SwapParameters = True}
            newMethod = rewriter.Visit(methodStmt)
            Assert.Equal("public Overloads      SUB   MySub ( ByVal Param2 As String , ByVal Param1 As Integer , ByRef Param3 As Double )", newMethod.ToString)

            'all 3
            rewriter = New TestVisitor() With {.SwapParameters = True, .CapitalizeIdentifiers = True, .CapitalizeKeywords = True}
            newMethod = rewriter.Visit(methodStmt)
            Assert.Equal("PUBLIC OVERLOADS      SUB   MYSUB ( BYVAL PARAM2 AS STRING , BYVAL PARAM1 AS INTEGER , BYREF PARAM3 AS DOUBLE )", newMethod.ToString)
        End Sub

        <Fact>
        Public Sub TestReplacer()
            Dim simpleTree = CreateSimpleTree()
            Assert.Equal("1- X( 3, 4+ 8, 9)", simpleTree.ToString)

            Dim firstOper = simpleTree.Left
            simpleTree = simpleTree.ReplaceNode(firstOper, SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken("""Hi""", "Hi")))
            Assert.Equal("""Hi""- X( 3, 4+ 8, 9)", simpleTree.ToString)

            ' if first arg is not in tree then returns same instance
            simpleTree = simpleTree.ReplaceNode(firstOper, SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken("""ha""", "ha")))
            Assert.Equal("""Hi""- X( 3, 4+ 8, 9)", simpleTree.ToString)

            Dim secondOper = simpleTree.Right
            simpleTree = simpleTree.ReplaceNode(secondOper, SyntaxFactory.StringLiteralExpression(SyntaxFactory.MissingStringLiteral()))
            Assert.Equal("""Hi""- ", simpleTree.ToFullString)

            Dim newSecondOper = simpleTree.Right
            simpleTree = simpleTree.ReplaceNode(newSecondOper, SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken("""Bye""", "Bye")))
            Assert.Equal("""Hi""- ""Bye""", simpleTree.ToFullString)

            Dim op = simpleTree.OperatorToken
            simpleTree = simpleTree.ReplaceToken(op, SyntaxFactory.MissingPunctuation(SyntaxKind.MinusToken))
            Assert.Equal("""Hi""""Bye""", simpleTree.ToFullString)

            op = simpleTree.OperatorToken
            simpleTree = simpleTree.ReplaceToken(op, SyntaxFactory.Token(SyntaxKind.EqualsToken, trailing:=_spaceTrivia))
            Assert.Equal("""Hi""= ""Bye""", simpleTree.ToFullString)
        End Sub

        <Fact>
        Public Sub TestReplaceNode()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Dim bex = DirectCast(expr, BinaryExpressionSyntax)
            Dim expr2 = expr.ReplaceNode(bex.Right, SyntaxFactory.ParseExpression("c"))
            Assert.Equal("a + c", expr2.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceNodes()
            Dim expr = SyntaxFactory.ParseExpression("a + b + c + d")

            ' replace each expression with a parenthesized expression
            Dim replaced = expr.ReplaceNodes(
                expr.DescendantNodes().OfType(Of ExpressionSyntax)(),
                Function(node, rewritten) SyntaxFactory.ParenthesizedExpression(rewritten))

            Dim replacedText = replaced.ToFullString()
            Assert.Equal("(((a )+ (b ))+ (c ))+ (d)", replacedText)
        End Sub

        <Fact>
        Public Sub TestReplaceNodesInListWithMultiple()
            Dim invocation = DirectCast(SyntaxFactory.ParseExpression("m(a, b)"), InvocationExpressionSyntax)
            Dim argC = SyntaxFactory.SimpleArgument(SyntaxFactory.ParseExpression("c"))
            Dim argD = SyntaxFactory.SimpleArgument(SyntaxFactory.ParseExpression("d"))

            ' replace first with multiple
            Dim newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments(0), {argC, argD})
            Assert.Equal("m(c,d, b)", newNode.ToFullString())

            ' replace last with multiple
            newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments(1), {argC, argD})
            Assert.Equal("m(a, c,d)", newNode.ToFullString())

            ' replace first with empty list
            newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments(0), New SyntaxNode() {})
            Assert.Equal("m(b)", newNode.ToFullString())

            ' replace last with empty list
            newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments(1), New SyntaxNode() {})
            Assert.Equal("m(a)", newNode.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceNonListNodeWithList()
            Dim invocation = DirectCast(SyntaxFactory.ParseExpression("m(a, b)"), InvocationExpressionSyntax)
            Dim expA = invocation.DescendantNodes().OfType(Of ExpressionSyntax).First(Function(n) n.ToString() = "a")
            Dim expC = SyntaxFactory.ParseExpression("c")
            Dim expD = SyntaxFactory.ParseExpression("d")

            ' cannot replace a node that is not in a list with multiple nodes
            Assert.Throws(Of InvalidOperationException)(Function() invocation.ReplaceNode(expA, {expC, expD}))

            ' cannot replace a node that is not in a list with and empty list of nodes
            Assert.Throws(Of InvalidOperationException)(Function() invocation.ReplaceNode(expA, New ExpressionSyntax() {}))
        End Sub

        <Fact>
        Public Sub TestInsertNodesInList()
            Dim invocation = DirectCast(SyntaxFactory.ParseExpression("m(a, b)"), InvocationExpressionSyntax)
            Dim argC = SyntaxFactory.SimpleArgument(SyntaxFactory.ParseExpression("c"))
            Dim argD = SyntaxFactory.SimpleArgument(SyntaxFactory.ParseExpression("d"))

            ' insert before first
            Dim newNode = invocation.InsertNodesBefore(invocation.ArgumentList.Arguments(0), {argC, argD})
            Assert.Equal("m(c,d,a, b)", newNode.ToFullString())

            '' insert after first
            newNode = invocation.InsertNodesAfter(invocation.ArgumentList.Arguments(0), {argC, argD})
            Assert.Equal("m(a,c,d, b)", newNode.ToFullString())

            ' insert before last
            newNode = invocation.InsertNodesBefore(invocation.ArgumentList.Arguments(1), {argC, argD})
            Assert.Equal("m(a,c,d, b)", newNode.ToFullString())

            ' insert after last
            newNode = invocation.InsertNodesAfter(invocation.ArgumentList.Arguments(1), {argC, argD})
            Assert.Equal("m(a, b,c,d)", newNode.ToFullString())
        End Sub

        <Fact>
        Public Sub TestInsertNodesRelativeToNonListNode()
            Dim invocation = DirectCast(SyntaxFactory.ParseExpression("m(a, b)"), InvocationExpressionSyntax)
            Dim expA = invocation.DescendantNodes().OfType(Of ExpressionSyntax).First(Function(n) n.ToString() = "a")
            Dim expC = SyntaxFactory.ParseExpression("c")
            Dim expD = SyntaxFactory.ParseExpression("d")

            ' cannot replace a node that is not in a list with multiple nodes
            Assert.Throws(Of InvalidOperationException)(Function() invocation.InsertNodesBefore(expA, {expC, expD}))

            ' cannot replace a node that is not in a list with and empty list of nodes
            Assert.Throws(Of InvalidOperationException)(Function() invocation.InsertNodesAfter(expA, {expC, expD}))
        End Sub

        <Fact>
        Public Sub TestReplaceStatementInListWithMultiple()
            Dim ifBlock = DirectCast(SyntaxFactory.ParseExecutableStatement(
<x>If a != b Then
Dim x = 10
Dim y = 20
End If</x>.Value), MultiLineIfBlockSyntax)

            Dim stmt1 = SyntaxFactory.ParseExecutableStatement(
<x>Dim z = 30
</x>.Value)
            Dim stmt2 = SyntaxFactory.ParseExecutableStatement(
<x>Dim q = 40
</x>.Value)

            '' replace first with multiple
            Dim newBlock = ifBlock.ReplaceNode(ifBlock.Statements(0), {stmt1, stmt2})
            Assert.Equal(
<x>If a != b Then
Dim z = 30
Dim q = 40
Dim y = 20
End If</x>.Value, newBlock.ToFullString())

            '' replace second with multiple
            newBlock = ifBlock.ReplaceNode(ifBlock.Statements(1), {stmt1, stmt2})
            Assert.Equal(
<x>If a != b Then
Dim x = 10
Dim z = 30
Dim q = 40
End If</x>.Value, newBlock.ToFullString())

            ' replace first with empty list
            newBlock = ifBlock.ReplaceNode(ifBlock.Statements(0), New SyntaxNode() {})
            Assert.Equal(
<x>If a != b Then
Dim y = 20
End If</x>.Value, newBlock.ToFullString())

            ' replace second with empty list
            newBlock = ifBlock.ReplaceNode(ifBlock.Statements(1), New SyntaxNode() {})
            Assert.Equal(
<x>If a != b Then
Dim x = 10
End If</x>.Value, newBlock.ToFullString())
        End Sub

        <Fact>
        Public Sub TestInsertStatementsInList()
            Dim ifBlock = DirectCast(SyntaxFactory.ParseExecutableStatement(
<x>If a != b Then
Dim x = 10
Dim y = 20
End If</x>.Value), MultiLineIfBlockSyntax)

            Dim stmt1 = SyntaxFactory.ParseExecutableStatement(
<x>Dim z = 30
</x>.Value)
            Dim stmt2 = SyntaxFactory.ParseExecutableStatement(
<x>Dim q = 40
</x>.Value)

            ' insert before first
            Dim newBlock = ifBlock.InsertNodesBefore(ifBlock.Statements(0), {stmt1, stmt2})
            Assert.Equal(
<x>If a != b Then
Dim z = 30
Dim q = 40
Dim x = 10
Dim y = 20
End If</x>.Value, newBlock.ToFullString())

            ' insert after first
            newBlock = ifBlock.InsertNodesAfter(ifBlock.Statements(0), {stmt1, stmt2})
            Assert.Equal(
<x>If a != b Then
Dim x = 10
Dim z = 30
Dim q = 40
Dim y = 20
End If</x>.Value, newBlock.ToFullString())

            ' insert before last
            newBlock = ifBlock.InsertNodesBefore(ifBlock.Statements(1), {stmt1, stmt2})
            Assert.Equal(
<x>If a != b Then
Dim x = 10
Dim z = 30
Dim q = 40
Dim y = 20
End If</x>.Value, newBlock.ToFullString())

            ' insert after last
            newBlock = ifBlock.InsertNodesAfter(ifBlock.Statements(1), {stmt1, stmt2})
            Assert.Equal(
<x>If a != b Then
Dim x = 10
Dim y = 20
Dim z = 30
Dim q = 40
End If</x>.Value, newBlock.ToFullString())

        End Sub

        <Fact>
        Public Sub TestReplaceToken()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Dim bToken = expr.DescendantTokens().First(Function(t) t.Text = "b")
            Dim expr2 = expr.ReplaceToken(bToken, SyntaxFactory.ParseToken("c"))
            Assert.Equal("a + c", expr2.ToString())
        End Sub

        <Fact>
        Public Sub TestReplaceMultipleTokens()
            Dim expr = SyntaxFactory.ParseExpression("a + b + c")
            Dim d = SyntaxFactory.ParseToken("d ")
            Dim tokens = expr.DescendantTokens().Where(Function(t) t.IsKind(SyntaxKind.IdentifierToken)).ToList()
            Dim replaced = expr.ReplaceTokens(tokens, Function(tok, tok2) d)
            Assert.Equal("d + d + d ", replaced.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceTokenWithMultipleTokens()
            Dim cu = SyntaxFactory.ParseCompilationUnit(
<x>Private Class C
End Class</x>.Value)

            Dim privateToken = DirectCast(cu.Members(0), ClassBlockSyntax).BlockStatement.Modifiers(0)
            Dim publicToken = SyntaxFactory.ParseToken("Public ")
            Dim partialToken = SyntaxFactory.ParseToken("Partial ")

            Dim cu1 = cu.ReplaceToken(privateToken, publicToken)
            Assert.Equal(
<x>Public Class C
End Class</x>.Value, cu1.ToFullString())

            Dim cu2 = cu.ReplaceToken(privateToken, {publicToken, partialToken})
            Assert.Equal(
<x>Public Partial Class C
End Class</x>.Value, cu2.ToFullString())

            Dim cu3 = cu.ReplaceToken(privateToken, New SyntaxToken() {})
            Assert.Equal(
<x>Class C
End Class</x>.Value, cu3.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceNonListTokenWithMultipleTokensFails()
            Dim cu = SyntaxFactory.ParseCompilationUnit(
<x>Private Class C
End Class</x>.Value)

            Dim identifierC = cu.DescendantTokens().First(Function(t) t.Text = "C")

            Dim identifierA = SyntaxFactory.ParseToken("A")
            Dim identifierB = SyntaxFactory.ParseToken("B")

            ' you cannot replace a token that Is a single token member with multiple tokens
            Assert.Throws(Of InvalidOperationException)(Function() cu.ReplaceToken(identifierC, {identifierA, identifierB}))

            ' you cannot replace a token that Is a single token member with an empty list of tokens
            Assert.Throws(Of InvalidOperationException)(Function() cu.ReplaceToken(identifierC, New SyntaxToken() {}))
        End Sub

        <Fact>
        Public Sub TestInsertTokensInList()
            Dim cu = SyntaxFactory.ParseCompilationUnit(
<x>Public Class C
End Class</x>.Value)
            Dim publicToken = DirectCast(cu.Members(0), ClassBlockSyntax).BlockStatement.Modifiers(0)
            Dim partialToken = SyntaxFactory.ParseToken("Partial ")
            Dim sharedToken = SyntaxFactory.ParseToken("Shared ")

            Dim cu1 = cu.InsertTokensBefore(publicToken, {sharedToken})
            Assert.Equal(
<x>Shared Public Class C
End Class</x>.Value, cu1.ToFullString())

            Dim cu2 = cu.InsertTokensAfter(publicToken, {sharedToken})
            Assert.Equal(
<x>Public Shared Class C
End Class</x>.Value, cu2.ToFullString())
        End Sub

        <Fact>
        Public Sub TestInsertTokensRelativeToNonListToken()
            Dim cu = SyntaxFactory.ParseCompilationUnit(
<x>Private Class C
End Class</x>.Value)

            Dim identifierC = cu.DescendantTokens().First(Function(t) t.Text = "C")

            Dim identifierA = SyntaxFactory.ParseToken("A")
            Dim identifierB = SyntaxFactory.ParseToken("B")

            ' you cannot insert tokens before/after a token that is not part of a list
            Assert.Throws(Of InvalidOperationException)(Function() cu.InsertTokensBefore(identifierC, {identifierA, identifierB}))

            ' you cannot insert tokens before/after a token that is not part of a list
            Assert.Throws(Of InvalidOperationException)(Function() cu.InsertTokensAfter(identifierC, New SyntaxToken() {}))
        End Sub


        <Fact>
        Public Sub TestReplaceSingleTriviaInNode()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Dim trivia = expr.DescendantTokens().First(Function(t) t.Text = "a").TrailingTrivia(0)
            Dim twoSpaces = SyntaxFactory.Whitespace("  ")
            Dim expr2 = expr.ReplaceTrivia(trivia, twoSpaces)
            Assert.Equal("a  + b", expr2.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceMultipleTriviaInNode()
            Dim expr = SyntaxFactory.ParseExpression("a + b")
            Dim twoSpaces = SyntaxFactory.Whitespace("  ")
            Dim trivia = expr.DescendantTrivia().Where(Function(tr) tr.IsKind(SyntaxKind.WhitespaceTrivia)).ToList()
            Dim replaced = expr.ReplaceTrivia(trivia, Function(tr, tr2) twoSpaces)
            Assert.Equal("a  +  b", replaced.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceSingleTriviaWithMultipleTriviaInNode()
            Dim ex = SyntaxFactory.ParseExpression("identifier 'c")
            Dim trivia = ex.GetTrailingTrivia()
            Assert.Equal(2, trivia.Count)
            Dim comment1 = trivia(1)
            Assert.Equal(SyntaxKind.CommentTrivia, Kind(comment1))

            Dim newComment1 = SyntaxFactory.ParseTrailingTrivia("'a")(0)
            Dim newComment2 = SyntaxFactory.ParseTrailingTrivia("'b")(0)

            Dim ex1 = ex.ReplaceTrivia(comment1, newComment1)
            Assert.Equal("identifier 'a", ex1.ToFullString())

            Dim ex2 = ex.ReplaceTrivia(comment1, {newComment1, newComment2})
            Assert.Equal("identifier 'a'b", ex2.ToFullString())

            Dim ex3 = ex.ReplaceTrivia(comment1, New SyntaxTrivia() {})
            Assert.Equal("identifier ", ex3.ToFullString())
        End Sub

        <Fact>
        Public Sub TestInsertTriviaInNode()
            Dim ex = SyntaxFactory.ParseExpression("identifier 'c")
            Dim trivia = ex.GetTrailingTrivia()
            Assert.Equal(2, trivia.Count)
            Dim comment1 = trivia(1)
            Assert.Equal(SyntaxKind.CommentTrivia, Kind(comment1))

            Dim newComment1 = SyntaxFactory.ParseTrailingTrivia("'a")(0)
            Dim newComment2 = SyntaxFactory.ParseTrailingTrivia("'b")(0)

            Dim ex1 = ex.InsertTriviaBefore(comment1, {newComment1, newComment2})
            Assert.Equal("identifier 'a'b'c", ex1.ToFullString())

            Dim ex2 = ex.InsertTriviaAfter(comment1, {newComment1, newComment2})
            Assert.Equal("identifier 'c'a'b", ex2.ToFullString())
        End Sub

        <Fact>
        Public Sub TestParseTrailingTrivia_SingleNewLine()
            Dim trivia = SyntaxFactory.ParseTrailingTrivia(vbCrLf)
            Assert.True(trivia.Count = 1)
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia(0).Kind())
        End Sub

        <Fact>
        Public Sub TestParseTrailingTrivia_MultipleNewLine()
            Dim trivia = SyntaxFactory.ParseTrailingTrivia(vbCrLf & vbCrLf)
            Assert.True(trivia.Count = 1)
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia(0).Kind())
        End Sub

        <Fact>
        Public Sub TestParseTrailingTrivia_CommentAndNewLine()
            Dim trivia = SyntaxFactory.ParseTrailingTrivia("'c" & vbCrLf)
            Assert.True(trivia.Count = 2)
            Assert.Equal(SyntaxKind.CommentTrivia, trivia(0).Kind())
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia(1).Kind())
        End Sub

        <Fact>
        Public Sub TestParseTrailingTrivia_CommentAndMultipleNewLine()
            Dim trivia = SyntaxFactory.ParseTrailingTrivia("'c" & vbCrLf & vbCrLf)
            Assert.True(trivia.Count = 2)
            Assert.Equal(SyntaxKind.CommentTrivia, trivia(0).Kind())
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia(1).Kind())
        End Sub

        <Fact>
        Public Sub TestParseTrailingTrivia_CommentAndNewLineAndDocComment()
            Dim trivia = SyntaxFactory.ParseTrailingTrivia("'c" & vbCrLf & "''' <summary/>")
            Assert.True(trivia.Count = 2)
            Assert.Equal(SyntaxKind.CommentTrivia, trivia(0).Kind())
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia(1).Kind())
        End Sub

        <Fact>
        Public Sub TestParseTrailingTrivia_CommentAndNewLineAndDirective()
            Dim trivia = SyntaxFactory.ParseTrailingTrivia("'c" & vbCrLf & "#If True Then")
            Assert.True(trivia.Count = 2)
            Assert.Equal(SyntaxKind.CommentTrivia, trivia(0).Kind())
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia(1).Kind())
        End Sub

        <Fact>
        Public Sub TestReplaceSingleTriviaInToken()
            Dim id = SyntaxFactory.ParseToken("a ")
            Dim trivia = id.TrailingTrivia(0)
            Dim twoSpace = SyntaxFactory.Whitespace("  ")
            Dim id2 = id.ReplaceTrivia(trivia, twoSpace)
            Assert.Equal("a  ", id2.ToFullString())
        End Sub

        <Fact>
        Public Sub TestReplaceMultipleTriviaInToken()
            Dim id = SyntaxFactory.ParseToken(
<x>a 'goo
</x>.Value)

            ' replace each trivia with a single space
            Dim id2 = id.ReplaceTrivia(id.GetAllTrivia(), Function(tr, tr2) SyntaxFactory.Space)

            ' should be 3 spaces (one for original space, comment and end-of-line)
            Assert.Equal("a   ", id2.ToFullString())
        End Sub

        <Fact>
        Public Sub TestTriviaExtensions()
            Dim simpleTree = CreateSimpleTree()
            Assert.Equal("1- X( 3, 4+ 8, 9) ", simpleTree.ToFullString())

            Dim tk = simpleTree.GetLastToken(includeZeroWidth:=True)
            Assert.Equal(") ", tk.ToFullString())

            tk = simpleTree.GetFirstToken(includeZeroWidth:=True)
            Assert.Equal("1", tk.ToFullString())

            tk = tk.WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia("   "))
            Assert.Equal("   1", tk.ToFullString())

            tk = tk.WithLeadingTrivia(SyntaxFactory.WhitespaceTrivia(" "))
            tk = tk.WithTrailingTrivia(SyntaxFactory.WhitespaceTrivia("      "))
            Assert.Equal(" 1      ", tk.ToFullString())
        End Sub

        <WorkItem(872867, "DevDiv/Personal")>
        <WorkItem(878887, "DevDiv/Personal")>
        <WorkItem(878902, "DevDiv/Personal")>
        <Fact>
        Public Sub TestCommonSyntaxNode()
            'Dim node As SyntaxNode = ParseAndVerify(" Module M1" & vbCrLf & "End Module")
            'Assert.False(node.Errors.Any())
            'Assert.Equal(0, node.GetTrailingTrivia.Count)
            'Assert.Equal(1, node.GetLeadingTrivia.Count)
            'Assert.False(node.IsTerminal)
            'Assert.Equal(3, node.ChildNodesAndTokens().Count)
            'Assert.Equal(1, node.SpanStart)
            'Assert.Equal(22, node.Span.End)
            'Assert.Equal(0, node.FullSpan.Start)
            'Assert.Equal(22, node.FullSpan.End)
            'Assert.Null(node.Parent)
            '
            ' When this breaks, uncomment above
            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText(SourceText.From(" Module M1" & vbCrLf & "End Module"))
            Dim node As SyntaxNode = tree.GetRoot()
            Assert.Equal(False, tree.GetDiagnostics(node).Any)
            Assert.Equal(0, tree.GetRoot().FindToken(node.FullSpan.Length - 1).TrailingTrivia.Count)
            Assert.Equal(1, tree.GetRoot().FindToken(0).LeadingTrivia.Count)
            Assert.Equal(False, node.ChildNodesAndTokens().Count = 0)
            Assert.Equal(2, node.ChildNodesAndTokens().Count)
            'Assert.Equal(1, tree.GetSpan(node).Start)
            'Assert.Equal(22, tree.GetSpan(node).End)
            'Assert.Equal(0, tree.GetFullSpan(node).Start)
            'Assert.Equal(22, tree.GetFullSpan(node).End)
            'Assert.Equal(Nothing, tree.GetParent(node))
        End Sub

        <WorkItem(879737, "DevDiv/Personal")>
        <Fact>
        Public Sub TestDiagnostic()
            'Dim node As SyntaxNode =
            '    ParseAndVerify(
            '        "Module M1" & vbCrLf & "End",
            '        <errors>
            '            <error id="30678"/>
            '            <error id="30625"/>
            '        </errors>)
            'Assert.True(node.Errors.Any)
            'Assert.Equal(2, node.Errors.Count)
            'Assert.Equal(DiagnosticSeverity.Error, node.Errors(0).Severity)
            'Assert.Contains(30678, From d In node.Errors Select d.Code)
            '
            ' When this breaks, uncomment above
            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText(SourceText.From("Module M1" & vbCrLf & "End"))
            Dim node As SyntaxNode = tree.GetRoot()
            Assert.Equal(True, tree.GetDiagnostics(node).Any)
            Assert.Equal(2, tree.GetDiagnostics(node).Count)
            Assert.Equal(DiagnosticSeverity.Error, tree.GetDiagnostics(node)(0).Severity)
            Assert.Equal(30625, tree.GetDiagnostics(node)(0).Code)
            Assert.Equal(30678, tree.GetDiagnostics(node)(1).Code)
        End Sub

        <Fact>
        Public Sub TestStructuredTrivia()
            Dim xmlStartElement = SyntaxFactory.XmlElementStartTag(
                SyntaxFactory.Token(_spaceTrivia, SyntaxKind.LessThanToken, trailing:=Nothing),
                SyntaxFactory.XmlName(Nothing,
                               SyntaxFactory.XmlNameToken("goo", SyntaxKind.XmlNameToken)),
                Nothing,
                SyntaxFactory.Token(SyntaxKind.GreaterThanToken, trailing:=_spaceTrivia))

            Dim xmlEndElement = SyntaxFactory.XmlElementEndTag(
                SyntaxFactory.Token(SyntaxKind.LessThanSlashToken, trailing:=_spaceTrivia),
                Nothing,
                SyntaxFactory.Token(Nothing, SyntaxKind.GreaterThanToken, trailing:=SyntaxTriviaList.Create(_spaceTrivia).Concat(_spaceTrivia).ToSyntaxTriviaList()))

            Dim xmlElement = SyntaxFactory.XmlElement(xmlStartElement, Nothing, xmlEndElement)
            Assert.Equal(" <goo> </ >  ", xmlElement.ToFullString)
            Assert.Equal("<goo> </ >", xmlElement.ToString)

            Dim docComment = SyntaxFactory.DocumentationCommentTrivia(SyntaxFactory.SingletonList(Of XmlNodeSyntax)(xmlElement))
            Assert.Equal(" <goo> </ >  ", docComment.ToFullString)
            Assert.Equal("<goo> </ >", docComment.ToString)
            Assert.Equal(" <goo> </ >  ", docComment.Content(0).ToFullString)
            Assert.Equal("<goo> </ >", docComment.Content(0).ToString)
            Assert.Equal(" <goo> ", DirectCast(docComment.Content(0), XmlElementSyntax).StartTag.ToFullString)
            Assert.Equal("<goo>", DirectCast(docComment.Content(0), XmlElementSyntax).StartTag.ToString)

            Dim sTrivia = SyntaxFactory.Trivia(docComment)
            Dim ident = SyntaxFactory.Identifier(sTrivia, "banana", _spaceTrivia)

            Assert.Equal(" <goo> </ >  banana ", ident.ToFullString())
            Assert.Equal("banana", ident.ToString())
            Assert.Equal(" <goo> </ >  ", ident.LeadingTrivia()(0).ToFullString)
            Assert.Equal("<goo> </ >", ident.LeadingTrivia()(0).ToString())


            Dim identExpr = SyntaxFactory.IdentifierName(ident)

            ' make sure FindLeaf digs into the structured trivia.
            Dim result = identExpr.FindToken(3, True)
            Assert.Equal(SyntaxKind.XmlNameToken, result.Kind)
            Assert.Equal("goo", result.ToString())

            Dim trResult = identExpr.FindTrivia(6, True)
            Assert.Equal(SyntaxKind.WhitespaceTrivia, trResult.Kind)
            Assert.Equal(" ", trResult.ToString())

            Dim foundDocComment = result.Parent.Parent.Parent.Parent
            Assert.Equal(Nothing, foundDocComment.Parent)

            Dim identTrivia = identExpr.GetLeadingTrivia(0)
            Dim foundTrivia = DirectCast(foundDocComment, DocumentationCommentTriviaSyntax).ParentTrivia
            Assert.Equal(identTrivia, foundTrivia)

            ' make sure FindLeafNodesOverlappingWithSpan does not dig into the structured trivia.
            Dim resultList = identExpr.DescendantTokens(New TextSpan(3, 18))
            Assert.Equal(1, resultList.Count)
        End Sub

        ' Check that the children list preserved identity.
        <Fact>
        Public Sub ChildrenListObjectIdentity()
            Dim tree = CreateSimpleTree()

            Dim children1 = tree.ChildNodesAndTokens()
            Dim children2 = tree.ChildNodesAndTokens()
            Assert.Equal(children1, children2)

            Dim child2_1 = tree.ChildNodesAndTokens()(1)
            Dim child2_2 = tree.ChildNodesAndTokens()(1)
            Assert.Equal(child2_1, child2_2)
        End Sub

        Private Function CreateNamespaceBlock() As NamespaceBlockSyntax

            Dim statementBuilder = SyntaxListBuilder(Of StatementSyntax).Create()
            statementBuilder.Add(SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxKind.EmptyToken)))
            statementBuilder.Add(SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxKind.EmptyToken)))
            statementBuilder.Add(SyntaxFactory.EmptyStatement(SyntaxFactory.Token(SyntaxKind.EmptyToken)))


            Return SyntaxFactory.NamespaceBlock(SyntaxFactory.NamespaceStatement(
                                            SyntaxFactory.Token(SyntaxKind.NamespaceKeyword, trailing:=_spaceTrivia), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("goo"))),
                                                statementBuilder.ToList,
                                            SyntaxFactory.EndNamespaceStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword), SyntaxFactory.Token(SyntaxKind.NamespaceKeyword)))
        End Function

        ' Check that specific children accessors preserve object identifier
        <Fact>
        Public Sub ChildAccessorObjectIdentity()
            Dim tree = CreateSimpleTree()

            Dim left1 = tree.Left
            Dim left2 = tree.Left
            Assert.Same(left1, left2)

            Dim nsBlock = CreateNamespaceBlock()

            Dim membs1 = nsBlock.Members
            Dim membs2 = nsBlock.Members
            Assert.Same(membs1.Node, membs2.Node)

            Dim begin1 = nsBlock.NamespaceStatement
            Dim begin2 = nsBlock.NamespaceStatement
            Assert.Same(begin1, begin2)

            Dim firstMember1 = nsBlock.Members(0)
            Dim firstMember2 = nsBlock.Members(0)
            Dim firstMember3 = nsBlock.ChildNodesAndTokens()(1)
            Assert.Same(firstMember1, firstMember2)
            Assert.Same(firstMember1, firstMember3.AsNode)

            nsBlock = CreateNamespaceBlock()

            firstMember3 = nsBlock.ChildNodesAndTokens()(1)
            firstMember1 = nsBlock.Members(0)
            firstMember2 = nsBlock.Members(0)
            Assert.Same(firstMember1, firstMember2)
            Assert.Same(firstMember1, firstMember3.AsNode)
        End Sub

        ' Check that specific children accessors preserve object identifier
        <Fact>
        Public Sub TestTreeIterator()
            Dim tree = CreateSimpleTree()

            Dim txt As String = ""
            Dim terminals = tree.DescendantTokens(tree.FullSpan)
            For Each n In terminals
                txt &= n.ToFullString()
            Next
            Assert.Equal(tree.ToFullString, txt)
            Assert.Equal(12, terminals.Count)

            Dim op = tree.Right
            Dim newOp = SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken("""Hi""", "Hi"))
            tree = tree.ReplaceNode(op, newOp)
            terminals = tree.DescendantTokens(tree.FullSpan)

            txt = ""
            For Each n In terminals
                txt &= n.ToFullString()
            Next
            Assert.Equal(tree.ToFullString, txt)
            Assert.Equal(3, terminals.Count)

        End Sub

        <Fact>
        Public Sub TestGetNextToken()
            Dim prog = ParseAndVerify(<![CDATA[Module Module1
    dim xxxx :: Dim yyyy
End Module
            ]]>)

            Dim tk0 = prog.GetRoot().FindToken(25)
            Assert.Equal("xxxx", tk0.ToString)

            Dim colons = tk0.TrailingTrivia().Where(Function(t) t.Kind = SyntaxKind.ColonTrivia).ToArray()
            Assert.Equal(colons.Length, 2)
            For Each colon In colons
                Assert.Equal(":", colon.ToString)
            Next

            Dim tk_nonzero1 = tk0.GetNextToken
            Assert.Equal("Dim", tk_nonzero1.ToString)

            Dim tk_nonzero2 = tk_nonzero1.GetNextToken
            Assert.Equal("yyyy", tk_nonzero2.ToString)

            Dim tk_zero1 = tk_nonzero1.GetNextToken(includeZeroWidth:=True)
            Assert.Equal("yyyy", tk_zero1.ToString)

            Dim newline = tk_zero1.TrailingTrivia.Where(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia).First
            Assert.Equal(vbLf, newline.ToString)
        End Sub

        <Fact, WorkItem(789824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789824"), WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestGetPreviousToken()
            Dim prog = ParseAndVerify(<![CDATA[
Module Module1
    dim xxxx :: Dim yyyy
End Module
            ]]>)

            Dim tk0 = prog.GetRoot().FindToken(32)
            Assert.Equal("Dim", tk0.ToString)

            Dim tk_nonzero1 = tk0.GetPreviousToken
            Assert.Equal("xxxx", tk_nonzero1.ToString)

            Dim trivia = tk_nonzero1.TrailingTrivia
            Assert.Equal(" ::", trivia.ToString)

            Dim tk_nonzero3 = tk_nonzero1.GetPreviousToken
            Assert.Equal("dim", tk_nonzero3.ToString)

            Dim tk_zero1 = tk_nonzero1.GetPreviousToken(includeZeroWidth:=True)
            Assert.Equal("dim", tk_zero1.ToString)

            Dim tk_zero2 = tk_zero1.GetPreviousToken(includeZeroWidth:=True)
            Assert.Equal("Module1", tk_zero2.ToString)

            Dim tk_zero3 = tk_zero2.GetPreviousToken(includeZeroWidth:=True)
            Assert.Equal(vbLf, tk_zero3.LeadingTrivia.ToString)
        End Sub

        <Fact>
        Public Sub TestCommonSyntaxTokenGetPreviousToken()
            Dim text =
                "Class C(Of T)" & vbCrLf &
                "    Dim l As List(Of T)" &
                "End Class"
            Dim tree = VisualBasicSyntaxTree.ParseText(text)

            Dim location = text.IndexOf("List(Of T)", StringComparison.Ordinal)
            Dim openParenToken = CType(tree.GetRoot().FindToken(location + "List".Length), SyntaxToken)

            Assert.Equal(SyntaxKind.OpenParenToken, openParenToken.Kind)

            Dim listToken = CType(openParenToken.GetPreviousToken(), SyntaxToken)

            Assert.Equal(SyntaxKind.IdentifierToken, listToken.Kind)
        End Sub

        <Fact, WorkItem(789824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789824"), WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestGetNextSibling()
            Dim prog = ParseAndVerify(<![CDATA[Module Module1
    dim xxxx ::: Dim yyyy
End Module
            ]]>)

            Dim trivia = prog.GetRoot().FindToken(28).TrailingTrivia
            Assert.Equal(" :::", trivia.ToString)

            Dim tk_nonzero1 = CType(trivia.First().Token.Parent.Parent.Parent, SyntaxNodeOrToken).GetNextSibling()
            Assert.Equal("Dim yyyy", tk_nonzero1.ToString)

            Dim tk_nonzero2 = tk_nonzero1.GetNextSibling
            Assert.Equal("End Module", tk_nonzero2.ToString)

        End Sub

        <Fact, WorkItem(789824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/789824"), WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestGetPreviousSibling()
            Dim prog = ParseAndVerify(<![CDATA[Module Module1
    dim xxxx ::: Dim yyyy
End Module
            ]]>)

            Dim tk0 As SyntaxNodeOrToken = prog.GetRoot().FindToken(31)
            Assert.Equal("Dim", tk0.ToString)

            Dim tk_nonzero1 = tk0.GetPreviousSibling
            Assert.Equal(Nothing, tk_nonzero1)

            tk0 = tk0.Parent
            tk_nonzero1 = tk0.GetPreviousSibling
            Assert.Equal("dim xxxx", tk_nonzero1.ToString)

            Dim trivia = tk_nonzero1.GetTrailingTrivia()
            Assert.Equal(" :::", trivia.ToString)

        End Sub


        <Fact>
        Public Sub TestFirstLastDirective()
            Dim prog = ParseAndVerify(<![CDATA[
#const x =1
#const y = 2
Module Module1
    dim xxxx ::: Dim yyyy
End Module
            ]]>)

            Dim tk0 As SyntaxNodeOrToken = prog.GetRoot().FindToken(35)
            Dim mDecl = tk0.Parent

            Dim fDir = mDecl.GetFirstDirective
            Assert.Equal("#const x =1", fDir.ToString)

            Dim lDir = mDecl.GetLastDirective
            Assert.Equal("#const y = 2", lDir.ToString)

            Dim fDir1 = mDecl.GetFirstDirective(Function(d) d.ToString = "#const y = 2")
            Assert.Equal("#const y = 2", fDir1.ToString)
            fDir1 = mDecl.GetFirstDirective(Function(d) d.ToString = "#const y = 42")
            Assert.Equal(Nothing, fDir1)

            Dim lDir1 = mDecl.GetLastDirective(Function(d) d.ToString = "#const x =1")
            Assert.Equal("#const x =1", lDir1.ToString)
            lDir1 = mDecl.GetLastDirective(Function(d) d.ToString = "#const x =42")
            Assert.Equal(Nothing, lDir1)
        End Sub

        <Fact>
        Public Sub TestNextPrevDirective()
            Dim prog = ParseAndVerify(<![CDATA[
#const x =1
#const y = 2
#const y = 3
Module Module1
    dim xxxx ::: Dim yyyy
End Module
            ]]>)

            Dim tk0 As SyntaxNodeOrToken = prog.GetRoot().FindToken(35)
            Dim mDecl = tk0.Parent

            Dim fDir = mDecl.GetFirstDirective
            Dim fDir1 = fDir.GetNextDirective
            Assert.Equal("#const y = 2", fDir1.ToString)

            Dim lDir = mDecl.GetLastDirective
            Dim lDir1 = fDir.GetNextDirective
            Assert.Equal("#const y = 2", lDir1.ToString)


            Dim fDir2 = fDir1.GetNextDirective(Function(d) d.ToString = "#const y = 3")
            Assert.Equal("#const y = 3", fDir2.ToString)
            fDir2 = fDir1.GetNextDirective(Function(d) d.ToString = "#const y = 42")
            Assert.Equal(Nothing, fDir2)

            Dim lDir2 = lDir1.GetPreviousDirective(Function(d) d.ToString = "#const x =1")
            Assert.Equal("#const x =1", lDir2.ToString)
            lDir2 = lDir1.GetPreviousDirective(Function(d) d.ToString = "#const x =42")
            Assert.Equal(Nothing, lDir2)

        End Sub

        <WorkItem(537404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537404")>
        <Fact>
        Public Sub TestNodeTokenConversion01()
            Dim prog = ParseAndVerify(<![CDATA[ Class A
    dim x As
End Class
            ]]>,
            <errors>
                <error id="30182"/>
            </errors>)

            Dim sN As SyntaxNodeOrToken = prog.GetRoot()
            Dim cS As SyntaxNodeOrToken = sN

            Assert.Equal(sN.IsNode, cS.IsNode)
            Assert.Equal(sN.IsToken, cS.IsToken)
            Assert.Equal(sN.IsMissing, cS.IsMissing)
            Assert.Equal(sN.ContainsDiagnostics, cS.ContainsDiagnostics)
            Assert.Equal(sN.ContainsDirectives, cS.ContainsDirectives)
            Assert.Equal(sN.HasLeadingTrivia, cS.HasLeadingTrivia)
            Assert.Equal(sN.HasTrailingTrivia, cS.HasTrailingTrivia)
            Assert.Equal(Kind(sN), Kind(cS))
            Assert.Equal(sN.FullWidth, cS.FullSpan.Length)
            Assert.Equal(sN.Width, cS.Span.Length)
        End Sub

        <WorkItem(537403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537403")>
        <Fact>
        Public Sub TestNodeTokenConversion02()

            Dim node As VisualBasicSyntaxNode = Nothing
            ' This should not throw - it should convert to a 'null' (default) struct 
            Dim sn As SyntaxNodeOrToken = node
            Assert.True(sn.IsToken)

        End Sub

        <WorkItem(537673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537673")>
        <Fact>
        Public Sub SyntaxTriviaDefaultIsDirective()
            Dim trivia As New SyntaxTrivia()
            Assert.False(trivia.IsDirective)
        End Sub

        <WorkItem(538362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538362")>
        <Fact, WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestGetNextTokenCommon()
            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText("public class goo : end class")

            Dim tokens As List(Of SyntaxToken) = tree.GetRoot().DescendantTokens().ToList()
            Dim list As List(Of SyntaxToken) = New List(Of SyntaxToken)()
            Dim token As SyntaxToken = tree.GetRoot().GetFirstToken()

            While token.Kind <> 0
                list.Add(token)
                token = token.GetNextToken()
            End While

            ' Descendant nodes contain EOF
            Assert.Equal(tokens.Count - 1, list.Count)
            For i = 0 To list.Count - 1
                Assert.Equal(list(i), tokens(i))
            Next

            ' Verify that EOF is returned when calling with Any predicate.
            list.Clear()
            token = tree.GetRoot().GetFirstToken()
            While token.Kind <> 0
                list.Add(token)
                token = token.GetNextToken(includeZeroWidth:=True)
            End While
            Debug.Assert(list(list.Count - 1).Kind = SyntaxKind.EndOfFileToken)

            Dim lastToken = tree.GetRoot().DescendantTokens().Last
            Debug.Assert(lastToken.Kind = SyntaxKind.EndOfFileToken)
        End Sub

        <WorkItem(755236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755236")>
        <WorkItem(9896, "https://github.com/dotnet/roslyn/issues/9896")>
        <Fact>
        Public Sub TestFindNode()
            Dim code = <code><![CDATA[
''' <see cref="Goo"/>
Class Goo
End Class

Class Bar
End Class]]>
                       </code>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(code)
            Dim root = tree.GetRoot()
            Assert.Equal(root, root.FindNode(root.Span, findInsideTrivia:=False))
            Assert.Equal(root, root.FindNode(root.Span, findInsideTrivia:=True))

            Dim classDecl = DirectCast(root.ChildNodes().First(), TypeBlockSyntax)
            Dim classStatement = classDecl.BlockStatement

            ' IdentifierNameSyntax in trivia.
            Dim identifier = root.DescendantNodes(descendIntoTrivia:=True).Single(Function(n) TypeOf n Is IdentifierNameSyntax)
            Dim position = identifier.Span.Start + 1

            Assert.Equal(classStatement, root.FindNode(identifier.Span, findInsideTrivia:=False))
            Assert.Equal(identifier.Parent, root.FindNode(identifier.Span, findInsideTrivia:=True))
            Assert.Equal(identifier.Parent.Span, identifier.Span)

            ' Token span.
            Assert.Equal(classStatement, root.FindNode(classStatement.Identifier.Span, findInsideTrivia:=False))

            Dim EOFSpan = New TextSpan(root.FullSpan.End, 0)
            Assert.Equal(root, root.FindNode(EOFSpan, findInsideTrivia:=False))
            Assert.Equal(root, root.FindNode(EOFSpan, findInsideTrivia:=True))

            ' EOF Invalid span for childnode
            Dim classDecl2 = DirectCast(root.ChildNodes().Last(), TypeBlockSyntax)
            Dim classStatement2 = classDecl2.BlockStatement
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() classDecl2.FindNode(EOFSpan))

            ' Check end position included in node span
            Dim nodeEndPositionSpan = New TextSpan(classDecl.FullSpan.End, 0)

            Assert.Equal(classStatement2, root.FindNode(nodeEndPositionSpan, findInsideTrivia:=False))
            Assert.Equal(classStatement2, root.FindNode(nodeEndPositionSpan, findInsideTrivia:=True))
            Assert.Equal(classStatement2, classDecl2.FindNode(nodeEndPositionSpan, findInsideTrivia:=False))
            Assert.Equal(classStatement2, classDecl2.FindNode(nodeEndPositionSpan, findInsideTrivia:=True))

            ' End position of node
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() classDecl.FindNode(nodeEndPositionSpan))

            ' Invalid spans.
            Dim invalidSpan = New TextSpan(100, 100)
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() root.FindNode(invalidSpan))
            invalidSpan = New TextSpan(root.FullSpan.End - 1, 2)
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() root.FindNode(invalidSpan))
            invalidSpan = New TextSpan(classDecl2.FullSpan.Start - 1, root.FullSpan.End)
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() classDecl2.FindNode(invalidSpan))
            invalidSpan = New TextSpan(classDecl.FullSpan.End, root.FullSpan.End)
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() classDecl2.FindNode(invalidSpan))
            ' Parent node's span.
            Assert.Throws(Of ArgumentOutOfRangeException)(Sub() classDecl.FindNode(root.FullSpan))
        End Sub

        <Fact>
        Public Sub TestFindTokenInLargeList()
            Dim identifier = SyntaxFactory.Identifier("x")
            Dim missingIdentifier = SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken)
            Dim name = SyntaxFactory.IdentifierName(identifier)
            Dim missingName = SyntaxFactory.IdentifierName(missingIdentifier)
            Dim comma = SyntaxFactory.Token(SyntaxKind.CommaToken)
            Dim missingComma = SyntaxFactory.MissingToken(SyntaxKind.CommaToken)
            Dim argument = SyntaxFactory.SimpleArgument(name)
            Dim missingArgument = SyntaxFactory.SimpleArgument(missingName)

            '' make a large list that has lots of zero-length nodes (that shouldn't be found)
            Dim nodesAndTokens = SyntaxFactory.NodeOrTokenList(
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                argument)

            Dim argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(Of ArgumentSyntax)(SyntaxFactory.NodeOrTokenList(nodesAndTokens)))
            Dim invocation = SyntaxFactory.InvocationExpression(name, argumentList)
            CheckFindToken(invocation)
        End Sub

        Private Sub CheckFindToken(node As SyntaxNode)
            For i As Integer = 1 To node.FullSpan.End - 1
                Dim token = node.FindToken(i)
                Assert.Equal(True, token.FullSpan.Contains(i))
            Next
        End Sub

        <WorkItem(539940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539940")>
        <Fact>
        Public Sub TestFindTriviaNoTriviaExistsAtPosition()
            Dim code = <code>Class Goo
    Sub Bar()
    End Sub
End Class</code>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(code)
            Dim position = tree.GetText().Lines(1).End
            'position points to the end of the line that has "Sub Bar()"
            'There should be end of line trivia there.
            Dim trivia = tree.GetRoot().FindTrivia(position)
            Assert.Equal(SyntaxKind.EndOfLineTrivia, trivia.Kind)
            Assert.Equal(23, trivia.SpanStart)
            Assert.Equal(24, trivia.Span.End)
        End Sub

        <Fact>
        Public Sub TestChildNodes()
            Dim text = "m(a,b,c)"
            Dim expression = SyntaxFactory.ParseExpression(text)
            Dim nodes = expression.ChildNodes().ToList()
            Assert.Equal(2, nodes.Count)
            Assert.Equal(SyntaxKind.IdentifierName, nodes(0).Kind)
            Assert.Equal(SyntaxKind.ArgumentList, nodes(1).Kind)
        End Sub

        <Fact>
        Public Sub TestAncestors()
            Dim text = "a + (b - (c * (d / e)))"
            Dim expression = SyntaxFactory.ParseExpression(text)
            Dim e = expression.DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(n) n.Identifier.ValueText = "e")

            Dim nodes = e.Ancestors().ToList()
            Assert.Equal(7, nodes.Count)
            Assert.Equal(SyntaxKind.DivideExpression, nodes(0).Kind)
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes(1).Kind)
            Assert.Equal(SyntaxKind.MultiplyExpression, nodes(2).Kind)
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes(3).Kind)
            Assert.Equal(SyntaxKind.SubtractExpression, nodes(4).Kind)
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes(5).Kind)
            Assert.Equal(SyntaxKind.AddExpression, nodes(6).Kind)
        End Sub

        <Fact>
        Public Sub TestAncestorsOrSelf()
            Dim text = "a + (b - (c * (d / e)))"
            Dim expression = SyntaxFactory.ParseExpression(text)
            Dim e = expression.DescendantNodes().OfType(Of IdentifierNameSyntax)().First(Function(n) n.Identifier.ValueText = "e")

            Dim nodes = e.AncestorsAndSelf().ToList()
            Assert.Equal(8, nodes.Count)
            Assert.Equal(SyntaxKind.IdentifierName, nodes(0).Kind)
            Assert.Equal(SyntaxKind.DivideExpression, nodes(1).Kind)
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes(2).Kind)
            Assert.Equal(SyntaxKind.MultiplyExpression, nodes(3).Kind)
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes(4).Kind)
            Assert.Equal(SyntaxKind.SubtractExpression, nodes(5).Kind)
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes(6).Kind)
            Assert.Equal(SyntaxKind.AddExpression, nodes(7).Kind)
        End Sub

        <Fact>
        Public Sub TestDescendantNodes()
            Dim text = <![CDATA[
    ''' Goo
    Return True
]]>.Value
            Dim statement = SyntaxFactory.ParseExecutableStatement(text)

            Dim nodes = statement.DescendantNodes().ToList()
            Assert.Equal(1, nodes.Count)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(0).Kind)

            nodes = statement.DescendantNodes(descendIntoTrivia:=True).ToList()
            Assert.Equal(4, nodes.Count)
            Assert.Equal(SyntaxKind.DocumentationCommentTrivia, nodes(0).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(1).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(2).Kind)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(3).Kind)

            ' again with spans

            nodes = statement.DescendantNodes(statement.FullSpan).ToList()
            Assert.Equal(1, nodes.Count)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(0).Kind)

            nodes = statement.DescendantNodes(statement.FullSpan, descendIntoTrivia:=True).ToList()
            Assert.Equal(4, nodes.Count)
            Assert.Equal(SyntaxKind.DocumentationCommentTrivia, nodes(0).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(1).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(2).Kind)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(3).Kind)
        End Sub

        <Fact>
        Public Sub TestDescendantNodesOrSelf()
            Dim text = <![CDATA[
    ''' Goo
    Return True
]]>.Value
            Dim statement = SyntaxFactory.ParseExecutableStatement(text)

            Dim nodes = statement.DescendantNodesAndSelf().ToList()
            Assert.Equal(2, nodes.Count)
            Assert.Equal(SyntaxKind.ReturnStatement, nodes(0).Kind)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(1).Kind)

            nodes = statement.DescendantNodesAndSelf(descendIntoTrivia:=True).ToList()
            Assert.Equal(5, nodes.Count)
            Assert.Equal(SyntaxKind.ReturnStatement, nodes(0).Kind)
            Assert.Equal(SyntaxKind.DocumentationCommentTrivia, nodes(1).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(2).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(3).Kind)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(4).Kind)

            ' again with spans

            nodes = statement.DescendantNodesAndSelf(statement.FullSpan).ToList()
            Assert.Equal(2, nodes.Count)
            Assert.Equal(SyntaxKind.ReturnStatement, nodes(0).Kind)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(1).Kind)

            nodes = statement.DescendantNodesAndSelf(statement.FullSpan, descendIntoTrivia:=True).ToList()
            Assert.Equal(5, nodes.Count)
            Assert.Equal(SyntaxKind.ReturnStatement, nodes(0).Kind)
            Assert.Equal(SyntaxKind.DocumentationCommentTrivia, nodes(1).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(2).Kind)
            Assert.Equal(SyntaxKind.XmlText, nodes(3).Kind)
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes(4).Kind)
        End Sub

        <Fact, WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestDescendantTrivia()
            Dim text = <![CDATA[' Goo
a + b
]]>.Value
            Dim expr = SyntaxFactory.ParseExpression(text)

            Dim list = expr.DescendantTrivia().ToList()
            Assert.Equal(6, list.Count)
            Assert.Equal(SyntaxKind.CommentTrivia, list(0).Kind)
            Assert.Equal(SyntaxKind.EndOfLineTrivia, list(1).Kind)
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list(2).Kind)
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list(3).Kind)
        End Sub

        <Fact, WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestDescendantTriviaIntoStructuredTrivia()
            Dim text = <![CDATA[
''' <goo >
''' </goo>
a + b
]]>.Value
            Dim expr = SyntaxFactory.ParseExpression(text)
            Dim list = expr.DescendantTrivia(descendIntoTrivia:=True).ToList()
            Assert.Equal(9, list.Count)
            Assert.Equal(SyntaxKind.EndOfLineTrivia, list(0).Kind)
            Assert.Equal(SyntaxKind.DocumentationCommentTrivia, list(1).Kind)
            Assert.Equal(SyntaxKind.DocumentationCommentExteriorTrivia, list(2).Kind)
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list(3).Kind)
            Assert.Equal(SyntaxKind.DocumentationCommentExteriorTrivia, list(4).Kind)
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list(5).Kind)
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list(6).Kind)
        End Sub

        <Fact>
        Public Sub SyntaxNodeToString()

            Dim text = "Imports System"
            Dim root = SyntaxFactory.ParseCompilationUnit(text)
            Dim children = root.DescendantNodesAndTokens()

            Dim nodeOrToken = children.First()
            'Assert.Equal("SyntaxNodeOrToken ImportsStatement Imports System", nodeOrToken.DebuggerDisplay)
            Assert.Equal(text, nodeOrToken.ToString())

            Dim node = children.First(Function(n) n.IsNode).AsNode()
            'Assert.Equal("ImportsStatementSyntax ImportsStatement Imports System", node.DebuggerDisplay)
            Assert.Equal(text, node.ToString())

            Dim token = children.First(Function(n) n.IsToken).AsToken()
            'Assert.Equal("SyntaxToken ImportsKeyword Imports", token.DebuggerDisplay)
            Assert.Equal("Imports ", token.ToFullString())
            Assert.Equal("Imports", token.ToString())

            Dim trivia = root.DescendantTrivia().First()
            'Assert.Equal("SyntaxTrivia WhitespaceTrivia  ", trivia.DebuggerDisplay)
            Assert.Equal(" ", trivia.ToFullString())
        End Sub

        <Fact>
        Public Sub TestRemoveNodeInSeparatedList_KeepExteriorTrivia()
            Dim expr = SyntaxFactory.ParseExpression("m(a, b, c)")

            Dim b = expr.DescendantTokens().Where(Function(t) t.ToString() = "b").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(b)

            Dim expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(a , c)", text)
        End Sub

        <Fact>
        Public Sub TestRemoveNodeInSeparatedList_KeepExteriorTrivia_2()
            Dim expr = SyntaxFactory.ParseExpression("m(a, b, ' comment
c)")

            Dim n = expr.DescendantTokens().Where(Function(t) t.ToString() = "b").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(n)

            Dim expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(a,  ' comment
c)", text)
        End Sub

        <Fact>
        Public Sub TestRemoveNodeInSeparatedList_KeepNoTrivia()
            Dim expr = SyntaxFactory.ParseExpression("m(a, b, c)")

            Dim b = expr.DescendantTokens().Where(Function(t) t.ToString() = "b").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(b)

            Dim expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepNoTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(a, c)", text)
        End Sub

        <Fact>
        Public Sub TestRemoveNodeInSeparatedList_KeepNoTrivia_2()
            Dim expr = SyntaxFactory.ParseExpression("m(a, b, ' comment
c)")

            Dim b = expr.DescendantTokens().Where(Function(t) t.ToString() = "b").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(b)

            Dim expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepNoTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(a, c)", text)
        End Sub

        <Fact>
        Public Sub TestRemoveOnlyNodeInSeparatedList_KeepExteriorTrivia()
            Dim expr = SyntaxFactory.ParseExpression("m(  a  )")

            Dim n = expr.DescendantTokens().Where(Function(t) t.ToString() = "a").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(n)

            Dim expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(    )", text)
        End Sub

        <Fact>
        Public Sub TestRemoveFirstNodeInSeparatedList_KeepExteriorTrivia()
            Dim expr = SyntaxFactory.ParseExpression("m(  a  , b, c)")

            Dim n = expr.DescendantTokens().Where(Function(t) t.ToString() = "a").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(n)

            Dim expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(     b, c)", text)
        End Sub

        <Fact>
        Public Sub TestRemoveLastNodeInSeparatedList_KeepExteriorTrivia()
            Dim expr = SyntaxFactory.ParseExpression("m(a, b , c )")

            Dim n = expr.DescendantTokens().Where(Function(t) t.ToString() = "c").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ArgumentSyntax)()).FirstOrDefault()
            Assert.NotNull(n)

            Dim expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim text = expr2.ToFullString()
            Assert.Equal("m(a, b   )", text)
        End Sub

        <Fact>
        Public Sub TestRemoveFirstNodeInList_KeepExteriorTrivia()
            Dim text = <![CDATA[
<A> <B> <C>
Class Goo
End Class
]]>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <![CDATA[
 <B> <C>
Class Goo
End Class
]]>.Value.Replace(vbLf, vbCrLf)

            Dim cu = SyntaxFactory.ParseCompilationUnit(text)
            Dim n = cu.DescendantTokens().Where(Function(t) t.ToString() = "A").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of AttributeListSyntax)()).FirstOrDefault()

            Dim cu2 = cu.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim result = cu2.ToFullString()

            Assert.Equal(expected, result)

        End Sub

        <Fact>
        Public Sub TestRemoveLastNodeInList_KeepExteriorTrivia()
            Dim text = <![CDATA[
<A> <B> <C>
Class Goo
End Class
]]>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <![CDATA[
<A> <B> 
Class Goo
End Class
]]>.Value.Replace(vbLf, vbCrLf)

            Dim cu = SyntaxFactory.ParseCompilationUnit(text)
            Dim n = cu.DescendantTokens().Where(Function(t) t.ToString() = "C").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of AttributeListSyntax)()).FirstOrDefault()

            Dim cu2 = cu.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim result = cu2.ToFullString()

            Assert.Equal(expected, result)

        End Sub

        <Fact>
        Public Sub TestMiddleLastNodeInList_KeepExteriorTrivia()
            Dim text = <![CDATA[
<A> <B> <C>
Class Goo
End Class
]]>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <![CDATA[
<A>  <C>
Class Goo
End Class
]]>.Value.Replace(vbLf, vbCrLf)

            Dim cu = SyntaxFactory.ParseCompilationUnit(text)
            Dim n = cu.DescendantTokens().Where(Function(t) t.ToString() = "B").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of AttributeListSyntax)()).FirstOrDefault()

            Dim cu2 = cu.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia)

            Dim result = cu2.ToFullString()

            Assert.Equal(expected, result)

        End Sub

        <Fact>
        Public Sub TestRemove_KeepUnbalancedDirectives()
            Dim text = <![CDATA[
#If True
Class Goo
End Class

Class Bar
End Class
#End If
]]>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <![CDATA[
#If True

Class Bar
End Class
#End If
]]>.Value.Replace(vbLf, vbCrLf)

            Dim cu = SyntaxFactory.ParseCompilationUnit(text)
            Dim n = cu.DescendantTokens().Where(Function(t) t.ToString() = "Goo").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ClassBlockSyntax)()).FirstOrDefault()

            Dim cu2 = cu.RemoveNode(n, SyntaxRemoveOptions.KeepUnbalancedDirectives)

            Dim result = cu2.ToFullString()

            Assert.Equal(expected, result)

        End Sub

        <Fact, WorkItem(530316, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530316")>
        Public Sub TestRemove_KeepExternalTrivia_KeepUnbalancedDirectives()
            Dim text = <![CDATA[
#If True
Class Goo

#If True
End Class

Class Bar
#End If

End Class
#End If
]]>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <![CDATA[
#If True
#If True


Class Bar
#End If

End Class
#End If
]]>.Value.Replace(vbLf, vbCrLf)

            Dim cu = SyntaxFactory.ParseCompilationUnit(text)
            Dim n = cu.DescendantTokens().Where(Function(t) t.ToString() = "Goo").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ClassBlockSyntax)()).FirstOrDefault()

            Dim cu2 = cu.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia Or SyntaxRemoveOptions.KeepUnbalancedDirectives)

            Dim result = cu2.ToFullString()

            Assert.Equal(expected, result)

        End Sub

        <Fact>
        Public Sub TestRemove_KeepDirectives()
            Dim text = <![CDATA[
#If True
Class Goo

#If True
#Region "A Region"
#End Region
End Class

Class Bar
#End If

End Class
#End If
]]>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <![CDATA[
#If True
#If True
#Region "A Region"
#End Region

Class Bar
#End If

End Class
#End If
]]>.Value.Replace(vbLf, vbCrLf)

            Dim cu = SyntaxFactory.ParseCompilationUnit(text)
            Dim n = cu.DescendantTokens().Where(Function(t) t.ToString() = "Goo").Select(Function(t) t.Parent.FirstAncestorOrSelf(Of ClassBlockSyntax)()).FirstOrDefault()

            Dim cu2 = cu.RemoveNode(n, SyntaxRemoveOptions.KeepDirectives)

            Dim result = cu2.ToFullString()

            Assert.Equal(expected, result)
        End Sub

        <Fact>
        Public Sub Test_SyntaxTree_ParseTextInvalid()
            Dim treeFromSourceWithPath_valid1 = VisualBasicSyntaxTree.ParseText("", path:=Nothing)

            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim st As SourceText = Nothing
                                                        Dim treeFromSource_invalid = VisualBasicSyntaxTree.ParseText(st)
                                                    End Sub)
        End Sub

        <Fact>
        Public Sub TestSyntaxTree_GetChangesValid()
            ' Added for coverage on GetChanges and SyntaxDiffer
            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic
Imports System.Collections

Module Module1
  Sub Main
    Dim a as Integer = 1
    Console.Writeline(a)
  End Sub
End Module
                           </String>

            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.ToString)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)

            'Get the Imports Clauses
            Dim FirstImportsClause As ImportsStatementSyntax = Root.Imports(0)
            Dim SecondImportsClause As ImportsStatementSyntax = Root.Imports(1)
            Dim ThirdImportsClause As ImportsStatementSyntax = Root.Imports(2)

            Dim ChangesForDifferentTrees = FirstImportsClause.SyntaxTree.GetChanges(SecondImportsClause.SyntaxTree)
            Assert.Equal(0, ChangesForDifferentTrees.Count)

            'Do a transform to Replace and Existing Tree
            Dim name As NameSyntax = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Collections.Generic"))
            Dim oldImportClause As SimpleImportsClauseSyntax = CType(ThirdImportsClause.ImportsClauses(0), SimpleImportsClauseSyntax)
            Dim newImportsClause = oldImportClause.WithName(name)

            'Replace Node with a different Imports Clause
            Root = Root.ReplaceNode(oldImportClause, newImportsClause)

            Dim ChangesFromTransform = ThirdImportsClause.SyntaxTree.GetChanges(newImportsClause.SyntaxTree)
            Assert.Equal(1, ChangesFromTransform.Count)

            'Using the Common Syntax Changes Method as well for coverage
            Dim x As SyntaxTree = ThirdImportsClause.SyntaxTree
            Dim y As SyntaxTree = newImportsClause.SyntaxTree

            Dim changes2UsingCommonSyntax = x.GetChanges(y)
            Assert.Equal(1, changes2UsingCommonSyntax.Count)

            'Verify Changes from VB Specific SyntaxTree and Common SyntaxTree are the same
            Assert.Equal(ChangesFromTransform, changes2UsingCommonSyntax)
        End Sub

        <Fact, WorkItem(658329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658329")>
        Public Sub TestSyntaxTree_GetChangesInValid()
            'GetChanges with two Scenarios where either new or old tree is nothing
            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic

Module Module1
  Sub Main
  End Sub
End Module
                           </String>

            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.ToString)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)

            Dim FirstImportsClause As ImportsStatementSyntax = Root.Imports(0)
            Dim BlankTree As SyntaxTree = Nothing

            Assert.Throws(Of ArgumentNullException)(Sub() FirstImportsClause.SyntaxTree.GetChanges(BlankTree))
        End Sub

        <Fact, WorkItem(658329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658329")>
        Public Sub TestSyntaxTree_GetChangeSpans()
            Dim oldTree = VisualBasicSyntaxTree.ParseText("class A : End Class")
            Dim newTree = oldTree.WithInsertAt(0, "class B : End Class")

            ' Valid operations
            Dim spans = newTree.GetChangedSpans(oldTree)
            Assert.Equal(1, spans.Count)

            'Test Overload with CommonSyntaxTree
            Dim span2 = CType(newTree, SyntaxTree).GetChangedSpans(CType(oldTree, SyntaxTree))
            Assert.Equal(1, spans.Count)
            Assert.Equal(spans(0), span2(0)) ' Ensure Both spans from overloads returns same

            ' Invalid operations with a null tree
            Dim BlankTree As SyntaxTree = Nothing

            Assert.Throws(Of ArgumentNullException)(Sub() newTree.GetChangedSpans(BlankTree))
        End Sub

        <Fact>
        Public Sub TestSyntaxList_Failures()
            'Validate the exceptions being generated when Invalid arguments are used for a TextSpan Constructor           

            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic

Module Module1
  Sub Main
  End Sub
End Module
                           </String>

            'Construct a SyntaxList and verify the bounds exceptions
            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.ToString)
            Dim x As New SyntaxList(Of SyntaxNode)
            For Each node In tree.GetRoot.ChildNodes
                x = x.Add(node)
            Next
            Assert.Equal(4, x.Count)

            Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                              Dim value1 = x(-1)
                                                          End Sub)

            Assert.Throws(Of ArgumentOutOfRangeException)(Sub()
                                                              Dim value1 = x(20)
                                                          End Sub)
        End Sub

        <Fact>
        Public Sub Test_CConst_CreateWithTypeCharacters()
            'Added for Code Coverage 
            Dim compilationDef =
<compilation name="CConst.vb">
    <file name="a.vb">
Imports System

#Const char_a = "c"c
#Const single_a = 1.2!
#Const Date_a = #1/1/2000#

Public Module Module1
#If char_a = "c"c Then
    Public Value_char As Boolean = True
#Else
    Public Value_char As Boolean = False
#End If

#If Single_a = 1.2! Then
    Public Value_single As Boolean = True
#Else
    Public Value_single As Boolean = False
#End If

#If Date_a = #1/1/2000# Then
    Public Value_Date As Boolean = True
#Else
    Public Value_Date As Boolean = False
#End If
    Sub main()
        Console.WriteLine(Value_char)
        Console.WriteLine(Value_single)
        Console.WriteLine(Value_Date)
    End Sub

End Module    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Test_UnaryOperators()
            Dim compilationDef =
<compilation name="CConst.vb">
    <file name="a.vb">
'Type Types
#If -1S Then
'Short
#End If

#If -1% Then
'Integer
#End If

#If -1@ Then
'Decimal
#End If

#If -1.0! Then
'Single
#End If

#If -1.0# Then
'Double
#End If

'Forced Literal Types
#If -1S Then
'Short
#End If

#If -1I Then
'Integer
#End If

#If -1L Then
'Long
#End If

#If -1.0F Then
'Single
#End If

#If -1D Then
'Double
#End If

#If -1UI Then
'unsigned Integer
#End If

#If -1UL Then
'unsigned Long
#End If

'Type Types
#If +1S Then
'Short
#End If

#If +1% Then
'Integer
#End If

#If +1@ Then
'Decimal
#End If

#If +1.0! Then
'Single
#End If

#If +1.0# Then
'Double
#End If

'Forced Literal Types
#If +1S Then
'Short
#End If

#If +1I Then
'Integer
#End If

#If +1L Then
'Long
#End If

#If +1.0F Then
'Single
#End If

#If +1D Then
'Double
#End If

#If +1UI Then
'unsigned Integer
#End If

#If +1UL Then
'unsigned Long
#End If

'Type Types
#If Not 1S Then
'Short
#End If

#If Not 1% Then
'Integer
#End If

#If Not 1@ Then
'Decimal
#End If

#If Not 1.0! Then
'Single
#End If

#If Not 1.0# Then
'Double
#End If

'Forced Literal Types
#If Not 1S Then
'Short
#End If

#If Not 1I Then
'Integer
#End If

#If Not 1L Then
'Long
#End If

#If Not 1.0F Then
'Single
#End If

#If Not 1D Then
'Double
#End If

#If Not 1UI Then
'unsigned Integer
#End If

#If Not 1UL Then
'unsigned Long
#End If

Module Module1
    Sub main()
    End Sub
End Module

</file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        <WorkItem(111538, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems?_a=edit&id=111538")>
        <WorkItem(658398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=658398")>
        Public Sub Test_UnaryOperatorsInvalid()
            'Added for Code Coverage 
            Dim compilationDef =
<compilation name="CConst.vb">
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

#If -"1"c Then
#End If

#If +"1" Then
#End If

#If +" "c Then
#End If

#If +"test"$ Then
#End If

#If Not " "c Then
#End If

#If NOT "test"$ Then
#End If

#If - Then
#End If

#If + Then
#End If

#If NOT Then
#End If

Module Module1
    Sub main()
    End Sub
End Module

</file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)
            compilation.AssertTheseDiagnostics(
<expected>
BC30487: Operator '-' is not defined for type 'Char'.
#If -"1"c Then
~~~~~~~~~~~~~~
BC30487: Operator '+' is not defined for type 'String'.
#If +"1" Then
~~~~~~~~~~~~~
BC30487: Operator '+' is not defined for type 'Char'.
#If +" "c Then
~~~~~~~~~~~~~~
BC30037: Character is not valid.
#If +"test"$ Then
           ~
BC30205: End of statement expected.
#If +"test"$ Then
           ~
BC30487: Operator 'Not' is not defined for type 'Char'.
#If Not " "c Then
~~~~~~~~~~~~~~~~~
BC30037: Character is not valid.
#If NOT "test"$ Then
              ~
BC30205: End of statement expected.
#If NOT "test"$ Then
              ~
BC31427: Syntax error in conditional compilation expression.
#If - Then
      ~~~~
BC31427: Syntax error in conditional compilation expression.
#If + Then
      ~~~~
BC31427: Syntax error in conditional compilation expression.
#If NOT Then
        ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestInvalidModuleScenario_AddingTreesWithImplementsAndInherits()
            'Verifies we can hit the inheritable and implements slots on module blocks through usage of the AddInherits/AddImplements methods in the  ModuleBlockSyntax.
            ' Although Modules do not support inheritance or Implements

            Dim SourceText = <String>
Module Module1
  Sub Main
  End Sub
End Module

Module Module2

End Module

Module Module3

End Module

Interface IGoo
End Interface
</String>

            Dim text = SourceText.Value
            'Construct a SyntaxList and verify the bounds exceptions
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)

            'We want to insert a Implements clause or Implements into Modules
            Dim Module1 = CType(Root.Members(0), ModuleBlockSyntax)
            Dim Module2 = CType(Root.Members(1), ModuleBlockSyntax)
            Dim Module3 = CType(Root.Members(2), ModuleBlockSyntax)

            Assert.Equal(0, Root.GetDiagnostics().Count)

            Dim bldr = SeparatedSyntaxListBuilder(Of TypeSyntax).Create()
            bldr.Add(CreateSimpleTypeName("aaa"))
            Dim sepList = bldr.ToList

            Dim statement = SyntaxFactory.InheritsStatement(SyntaxFactory.Token(SyntaxKind.InheritsKeyword, trailing:=_spaceTrivia), sepList)
            Module2 = Module2.AddInherits(statement)
            Root = Root.ReplaceNode(Root.Members(1), Module2)

            Assert.Equal(0, Root.GetDiagnostics().Count)

            Dim bldr2 = SeparatedSyntaxListBuilder(Of TypeSyntax).Create()
            bldr2.Add(CreateSimpleTypeName("Ifoo"))
            Dim sepList2 = bldr2.ToList
            Dim statement2 = SyntaxFactory.ImplementsStatement(SyntaxFactory.Token(SyntaxKind.ImplementsKeyword, trailing:=_spaceTrivia), sepList2)
            Module3 = Module3.AddImplements(statement2)

            Root = Root.ReplaceNode(Root.Members(2), Module3)
            Assert.Equal(0, Root.GetDiagnostics().Count)  ' No updated diagnostics until the re-parsing

            '//Verify the syntax Tree contains strings
            Assert.True(Root.ToFullString.Contains("Inherits aaa"))
            Assert.True(Root.ToFullString.Contains("Implements Ifoo"))

            Dim compilationDef =
<compilation name="Test">
    <file name="a.vb">
        <%= Root.ToFullString %>
    </file>
</compilation>

            'Verify Compile Errors when try to use
            Dim c = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
                compilationDef,
                New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom))

            c.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ModuleCantInherit, "Inherits aaa"),
                Diagnostic(ERRID.ERR_ModuleCantImplement, "Implements Ifoo"))
        End Sub

        <Fact>
        Public Sub TestSyntaxNode_GetDirectivesMultiplePresent()
            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic


Module Module1
  Sub Main
  End Sub
End Module

#if True then
Module Module2
End Module
#End If
#if False then
Module Module3
End Module
#End IF
</String>

            Dim text = SourceText.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)

            Dim x = Root.GetDirectives()
            Assert.Equal(4, x.Count)
            Assert.Equal("#if True then", x(0).GetText.ToString.TrimEnd)
            Assert.Equal("Microsoft.CodeAnalysis.VisualBasic.Syntax.IfDirectiveTriviaSyntax", x(0).GetType.ToString)
        End Sub

        <Fact>
        Public Sub TestSyntaxNode_GetDirectivesNonePresent()
            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic
Module Module1
  Sub Main
  End Sub
End Module
Module Module2
End Module
Module Module3
End Module
</String>

            Dim text = SourceText.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)
            Dim x = Root.GetDirectives()
            Assert.Equal(0, x.Count)
        End Sub

        <Fact>
        Public Sub TestSyntaxNode_GetDirectivesIncorrectUnbalanced()
            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic
Module Module1
  Sub Main
  End Sub
End Module
#if True then
Module Module2
End Module
Module Module3
End Module
</String>

            Dim text = SourceText.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)
            Dim x = Root.GetDirectives()
            Assert.Equal(1, x.Count)
        End Sub


        <WorkItem(6536, "https://github.com/dotnet/roslyn/issues/6536")>
        <Fact>
        Public Sub TestFindTrivia_NoStackOverflowOnLargeExpression()
            Dim code As New StringBuilder()
            code.Append(<![CDATA[
Module Module1
     Sub Test()
         Dim c =  ]]>.Value)
            For i = 0 To 3000
                code.Append("""asdf"" + ")
            Next
            code.AppendLine(<![CDATA["last"
    End Sub
End Module]]>.Value)

            Dim tree = VisualBasicSyntaxTree.ParseText(code.ToString())
            Dim trivia = tree.GetRoot().FindTrivia(4000)
            ' no stack overflow
        End Sub

#Region "Equality Verifications"
        <Fact>
        Public Sub Test_GlobalImportsEqual()
            Dim SourceText = <String>
Imports System
Imports Microsoft.VisualBasic
Module Module1
  Sub Main
  End Sub
End Module
#if True then
Module Module2
End Module
Module Module3
End Module
</String>

            Dim text = SourceText.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)
            Dim x = Root.GetDirectives()
            Assert.Equal(1, x.Count)
        End Sub

        <Fact>
        Public Sub Test_GlobalImports_Equals()
            Dim SourceText = <String>
Imports System

Module Module1
  Sub Main
    Dim a as Integer = 1
    Console.Writeline(a)
  End Sub
End Module
                           </String>

            Dim tree = VisualBasicSyntaxTree.ParseText(SourceText.ToString)
            Dim Root As CompilationUnitSyntax = CType(tree.GetRoot(), CompilationUnitSyntax)
            Dim FirstImportsClause As ImportsStatementSyntax = Root.Imports(0)
            Dim Obj1 As New GlobalImport(FirstImportsClause.ImportsClauses(0), "ttt")
            Dim Obj2 As New GlobalImport(FirstImportsClause.ImportsClauses(0), "uuu")
            Dim obj3 As GlobalImport = Obj1

            Assert.Equal(Obj1, obj3)
            Assert.NotEqual(Obj1, Obj2)
            If Obj1 = Obj2 Then
                Assert.True(False, "GlobalImports equal Failure")
            End If
            If Obj1 <> obj3 Then
                Assert.True(False, "GlobalImports Not equal Failure")
            End If
        End Sub

        <Fact>
        Public Sub Test_CompilationOptions_Equals()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="allon.vb">
Option Strict On
Option Infer On
Option Explicit On
Option Compare Text
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(False).WithOptionExplicit(True).WithOptionCompareText(False))

            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="allon.vb">
Option Strict On
Option Infer On
Option Explicit On
Option Compare Text
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(False).WithOptionExplicit(False).WithOptionCompareText(False))

            Dim Compilation3 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="allon.vb">
Option Strict On
Option Infer On
Option Explicit On
Option Compare Text
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(False).WithOptionExplicit(True).WithOptionCompareText(False))


            Dim vbpo1 = compilation.Options
            Dim vbpo2 = compilation2.Options
            Dim vbpo3 = Compilation3.Options

            Assert.Equal(vbpo1, vbpo1)
            Assert.Equal(vbpo1, vbpo3)
            Assert.NotEqual(vbpo1, vbpo2)

            Dim Objvbpo1 As Object = vbpo3
            Assert.Equal(vbpo1, Objvbpo1)

            Dim compilation4 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="CompileOptions">
    <file name="allon.vb">
Option Strict Off
Module Module1
Sub Main
End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(False).WithOptionExplicit(True).WithOptionCompareText(False))

            vbpo1 = compilation4.Options
            Dim ObjCommonCompilationOptions As CompilationOptions = vbpo1
            Dim RetValue = ObjCommonCompilationOptions.Equals(ObjectDisplay.NullLiteral)
            Assert.False(RetValue)
        End Sub

        <Fact>
        Public Sub Test_ParseOptions_Equals()
            Dim po1 = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic10)
            Dim po2 = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic9)
            Dim po3 = New VisualBasicParseOptions(languageVersion:=LanguageVersion.VisualBasic10)

            Dim POcompilation1 = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Compile1">
    <file name="a.vb"><![CDATA[
Imports System
Module Module1
Sub Main
End Sub
End Module
        ]]></file>
</compilation>, parseOptions:=po1)

            Assert.Equal(po1, po1)
            Assert.Equal(po1, po3)
            Assert.NotEqual(po1, po2)

            Dim Objpo1 As Object = po3
            Assert.Equal(po1, Objpo1)
        End Sub
#End Region

#Region "SyntaxWalker Verification Tests For Specific Node Types"
        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyGroupByClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="LinqQueryGroupBy">
    <file name="Test.vb">
Module Module1
    Sub main()
        Dim words = New String() {"blueberry", "chimpanzee", "abacus", "banana", "apple", "cheese"}

        Dim wordGroups = From w In words _
                             Group w By Key = w(0) Into Group _
                             Select FirstLetter = Key, WordGroup = Group

    End Sub
End Module            
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.GroupByClause.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyCatchFilterClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
Module Module1
    Sub Main()        
        Try
        Catch ex As Exception When TypeOf (ex) Is ArgumentException
        Catch ex As Exception
        End Try
    End Sub
End Module          
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.CatchFilterClause.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyDistinctClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
Module Module1
    Sub Main()        
       Dim q = From c In {1, 2, 3, 4, 5, 3, 2}
       Select c Distinct
    End Sub
End Module          
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.DistinctClause.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyCaseRange()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
Module Module1
    Sub Main()
        Dim number As Integer = 8
        Select Case number
            Case 1 To 5                
            Case Else                
        End Select
    End Sub
End Module
            </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.RangeCaseClause.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyHandlesClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
            Imports System

        Public Class ContainerClass
            ' Module or class level declaration. 
            WithEvents Obj As New Class1

            Public Class Class1
                ' Declare an event. 
                Public Event Ev_Event()
                Sub CauseSomeEvent()
                    ' Raise an event. 
                    RaiseEvent Ev_Event()
                End Sub
            End Class

            Sub EventHandler() Handles Obj.Ev_Event
                ' Handle the event.                
            End Sub

            ' Call the TestEvents procedure from an instance of the ContainerClass  
            ' class to test the Ev_Event event and the event handler. 
            Public Sub TestEvents()
                Obj.CauseSomeEvent()
            End Sub
        End Class
                        </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.HandlesClause.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.HandlesClauseItem.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.WithEventsEventContainer.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyKeywordEventContainerSyntax()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
Imports System
Module Module1
    Sub Main()
    End Sub
End Module

Class ContainerClass
    WithEvents obj As New Class1

    Public Class Class1
        Public Event ev_events()
        Public Sub causeEvent()
            RaiseEvent ev_events()
        End Sub

        Sub EventHandlerInClass1() Handles Me.ev_events
        End Sub
    End Class
End Class
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.KeywordEventContainer.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyOmittedArgument()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
            Imports System

Module Module1
    Sub Main()
        goo(1, , 2)
    End Sub

    Sub goo(x As Integer, Optional y As Integer = 2, Optional z As Integer = 2)
    End Sub
End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.OmittedArgument.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyMidExpressionClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
        Module Module1
            Sub Main()
                Dim s As String = "abcdef"
                 Mid(s, 2, 1) = "Z"
                Dim s2 As String = ""
                s2 = Mid(s, 2, 1)
            End Sub
        End Module

            </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.MidExpression.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyAggregateClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
                Module Module1
                Sub main()
                     Dim words() = {"cherry", "apple", "blueberry", "cherry"}
                    Dim ag1 = aggregate w in words
                              where w = "cherry"
                              into count
                End Sub
                End Module            
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.AggregateClause.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyDirectives()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
#ExternalChecksum ("Test.vb", "{12345678-1234-1234-1234-123456789abc}", "1a2b3c4e5f617239a49b9a9c0391849d34950f923fab9484")
Module Program
#Const a = 1
    Sub Main(args As String())

#ExternalSource ("test.txt", 1)
#End ExternalSource

#If a = 1 Then
#ElseIf a = 2 Then
#Else
#End If
    End Sub

#Region "Test"
    Sub Goo()

    End Sub
#End Region
End Module
                            </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier(SyntaxWalkerDepth.StructuredTrivia)

            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.ConstDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.IfDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.ElseIfDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.ElseDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.ExternalSourceDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.EndExternalSourceDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.ExternalChecksumDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.RegionDirectiveTrivia.ToString))
            Assert.Equal(1, collector.GetCount(SyntaxKind.EndRegionDirectiveTrivia.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyPartitionClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
        Module Module1
            Sub Main()        
              Dim numbers() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
              Dim first3Numbers = from w in numbers
                                  Take(3)
            End Sub
        End Module          
            </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount("PartitionClauseSyntax"))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyPartitionWhileClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
                Module Module1
                    Sub Main()        
                   Dim numbers() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
                    Dim laterNumbers = from n in numbers
                                       Take While(3)
                                       Select i
                    End Sub
                End Module          
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount("PartitionWhileClauseSyntax"))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyRangeArgument()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
        Imports System
        Module Module1
            Sub Main()
                ReDim myarray(0 to 10) ' Resize to 11 elements.
            End Sub
        End Module
    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.RangeArgument.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyXMLBracketName()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
        Imports System
Module Module1
    Sub Main()
        Dim x = &lt;Goo&gt;
                    &lt;Bar&gt;1&lt;/Goo&gt;
                &lt;/Goo&gt;

        Dim y = x.&lt;Goo&gt;.&lt;Bar&gt;.Value
    End Sub
End Module
                    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(2, collector.GetCount(SyntaxKind.XmlBracketedName.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyIncompleteSyntaxClause()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">
                 &lt;Dim:clscompliant(true)&gt;
                    Module Module1
                    End Module
                    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.IncompleteMember.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifySkippedTokenTrivia()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">                
                    OptImports System
                    Module Module1
                    Sub main
                    End Sub
                    End Module
                    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier(SyntaxWalkerDepth.StructuredTrivia)
            collector.Visit(root)
            Assert.Equal(1, collector.GetCount(SyntaxKind.SkippedTokensTrivia.ToString))
        End Sub

        <Fact>
        Public Sub SyntaxWalkerMethod_VerifyInferredFieldName()
            Dim Compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyntaxWalkerTestTypes">
    <file name="Test.vb">                
Imports System
Module Module1
    Sub Main()
        Dim productName As String = "paperclips"
        Dim productPrice As Double = 1.29
        Dim anonProduct = New With {Key productName, Key productPrice}
    End Sub
End Module
                    </file>
</compilation>)

            Dim tree = Compilation.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim collector = New SyntaxWalkerVerifier()
            collector.Visit(root)
            Assert.Equal(2, collector.GetCount(SyntaxKind.InferredFieldInitializer.ToString))
        End Sub
#End Region
    End Class
End Namespace
