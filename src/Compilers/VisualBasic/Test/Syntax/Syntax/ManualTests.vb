' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class GeneratedTests

        <Fact>
        Public Sub TestUpdateWithNull()
            ' create type parameter with constraint clause
            Dim tp = SyntaxFactory.TypeParameter(Nothing, SyntaxFactory.Identifier("T"), SyntaxFactory.TypeParameterSingleConstraintClause(SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("IFoo"))))

            ' attempt to make variant w/o constraint clause (do not access property first)
            Dim tp2 = tp.WithTypeParameterConstraintClause(Nothing)

            ' correctly creates variant w/o constraint clause
            Assert.Null(tp2.TypeParameterConstraintClause)
        End Sub

        <Fact, WorkItem(546397, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546397")>
        Public Sub TestConstructClassBlock()
            Dim c = SyntaxFactory.ClassBlock(SyntaxFactory.ClassStatement("C").AddTypeParameterListParameters(SyntaxFactory.TypeParameter("T"))) _
                          .AddImplements(SyntaxFactory.ImplementsStatement(SyntaxFactory.ParseTypeName("X"), SyntaxFactory.ParseTypeName("Y")))

            Dim expectedText As String = _
                "Class C(Of T)" + vbCrLf + _
                "    Implements X, Y" + vbCrLf + _
                vbCrLf + _
                "End Class"

            Dim actualText = c.NormalizeWhitespace().ToFullString()
            Assert.Equal(expectedText, actualText)
        End Sub

        <Fact()>
        Public Sub TestCastExpression()
            Dim objUnderTest As VisualBasicSyntaxNode = SyntaxFactory.CTypeExpression(SyntaxFactory.Token(SyntaxKind.CTypeKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), GenerateRedCharacterLiteralExpression(), SyntaxFactory.Token(SyntaxKind.CommaToken), GenerateRedArrayType(), SyntaxFactory.Token(SyntaxKind.CloseParenToken))
            Assert.True(Not objUnderTest Is Nothing, "obj can't be Nothing")
        End Sub

        <Fact()>
        Public Sub TestOnErrorGoToStatement()
            Dim objUnderTest As VisualBasicSyntaxNode = SyntaxFactory.OnErrorGoToStatement(SyntaxKind.OnErrorGoToLabelStatement, SyntaxFactory.Token(SyntaxKind.OnKeyword), SyntaxFactory.Token(SyntaxKind.ErrorKeyword), SyntaxFactory.Token(SyntaxKind.GoToKeyword), Nothing, SyntaxFactory.IdentifierLabel(GenerateRedIdentifierToken()))
            Assert.True(Not objUnderTest Is Nothing, "obj can't be Nothing")
        End Sub

        <Fact()>
        Public Sub TestMissingToken()

            For k = CInt(SyntaxKind.AddHandlerKeyword) To CInt(SyntaxKind.AggregateKeyword) - 1
                If CType(k, SyntaxKind).ToString() = k.ToString Then Continue For ' Skip any "holes" in the SyntaxKind enum
                Dim objUnderTest As SyntaxToken = SyntaxFactory.MissingToken(CType(k, SyntaxKind))
                Assert.Equal(objUnderTest.Kind, CType(k, SyntaxKind))
            Next k

            For k = CInt(SyntaxKind.CommaToken) To CInt(SyntaxKind.AtToken) - 1
                If CType(k, SyntaxKind).ToString() = k.ToString Then Continue For ' Skip any "holes" in the SyntaxKind enum
                Dim objUnderTest As SyntaxToken = SyntaxFactory.MissingToken(CType(k, SyntaxKind))
                Assert.Equal(objUnderTest.Kind, CType(k, SyntaxKind))
            Next k
        End Sub

        ''' Bug 7983
        <Fact()>
        Public Sub TestParsedSyntaxTreeToString()

            Dim input = "    Module m1" + vbCrLf + _
                        "Sub      Main(args As String())" + vbCrLf + _
                        "Sub1  (   Function(p   As   Integer   )" + vbCrLf + _
                        "Sub2(    )" + vbCrLf + _
                        "End FUNCTION)" + vbCrLf + _
                        "End              Sub" + vbCrLf + _
                        "End       Module                     "

            Dim node = VisualBasicSyntaxTree.ParseText(input)
            Assert.Equal(input, node.ToString())
        End Sub

        ''' Bug 10283
        <Fact()>
        Public Sub Bug_10283()
            Dim input = "Dim foo()"
            Dim node = VisualBasicSyntaxTree.ParseText(input)
            Dim arrayRankSpecifier = DirectCast(node.GetCompilationUnitRoot().Members(0), FieldDeclarationSyntax).Declarators(0).Names(0).ArrayRankSpecifiers(0)
            Assert.Equal(1, arrayRankSpecifier.Rank)
            Assert.Equal(0, arrayRankSpecifier.CommaTokens.Count)

            input = "Dim foo(,,,)"
            node = VisualBasicSyntaxTree.ParseText(input)
            arrayRankSpecifier = DirectCast(node.GetCompilationUnitRoot().Members(0), FieldDeclarationSyntax).Declarators(0).Names(0).ArrayRankSpecifiers(0)
            Assert.Equal(4, arrayRankSpecifier.Rank)
            Assert.Equal(3, arrayRankSpecifier.CommaTokens.Count)
        End Sub

        <WorkItem(543310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543310")>
        <Fact()>
        Public Sub SyntaxDotParseCompilationUnitContainingOnlyWhitespace()
            Dim node = SyntaxFactory.ParseCompilationUnit("  ")
            Assert.True(node.HasLeadingTrivia)
            Assert.Equal(1, node.GetLeadingTrivia().Count)
            Assert.Equal(1, node.DescendantTrivia().Count())
            Assert.Equal("  ", node.GetLeadingTrivia().First().ToString())
        End Sub

        <WorkItem(543310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543310")>
        <Fact()>
        Public Sub SyntaxTreeDotParseCompilationUnitContainingOnlyWhitespace()
            Dim node = VisualBasicSyntaxTree.ParseText("  ").GetRoot()
            Assert.True(node.HasLeadingTrivia)
            Assert.Equal(1, node.GetLeadingTrivia().Count)
            Assert.Equal(1, node.DescendantTrivia().Count())
            Assert.Equal("  ", node.GetLeadingTrivia().First().ToString())
        End Sub

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact()>
        Public Sub SyntaxTreeIsHidden_Bug13776()
            Dim source = <![CDATA[
Module Program
Sub Main()
If a Then
a()
Else If b Then
#End ExternalSource
b()
#ExternalSource
Else
c()
End If
End Sub
End Module
]]>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source)

            Assert.Equal(LineVisibility.Visible, tree.GetLineVisibility(0))
            Assert.Equal(LineVisibility.Visible, tree.GetLineVisibility(source.Length - 2))
            Assert.Equal(LineVisibility.Visible, tree.GetLineVisibility(source.IndexOf("a()", StringComparison.Ordinal)))
            Assert.Equal(LineVisibility.Visible, tree.GetLineVisibility(source.IndexOf("b()", StringComparison.Ordinal)))
            Assert.Equal(LineVisibility.Visible, tree.GetLineVisibility(source.IndexOf("c()", StringComparison.Ordinal)))
        End Sub

        <WorkItem(546586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546586")>
        <Fact()>
        Public Sub KindsWithSameNameAsTypeShouldNotDropKindWhenUpdating_Bug16244()
            Dim assignmentStatement = GeneratedTests.GenerateRedAddAssignmentStatement()
            Dim newAssignmentStatement = assignmentStatement.Update(assignmentStatement.Kind, GeneratedTests.GenerateRedAddExpression(), SyntaxFactory.Token(SyntaxKind.PlusEqualsToken), GeneratedTests.GenerateRedAddExpression())
            Assert.Equal(assignmentStatement.Kind, newAssignmentStatement.Kind)
        End Sub

        <Fact>
        Public Sub TestSeparatedListFactory_DefaultSeparators()
            Dim null1 = SyntaxFactory.SeparatedList(CType(Nothing, ParameterSyntax()))

            Assert.Equal(0, null1.Count)
            Assert.Equal(0, null1.SeparatorCount)
            Assert.Equal("", null1.ToString())

            Dim null2 = SyntaxFactory.SeparatedList(CType(Nothing, IEnumerable(Of ModifiedIdentifierSyntax)))

            Assert.Equal(0, null2.Count)
            Assert.Equal(0, null2.SeparatorCount)
            Assert.Equal("", null2.ToString())

            Dim empty1 = SyntaxFactory.SeparatedList(New TypeArgumentListSyntax() {})

            Assert.Equal(0, empty1.Count)
            Assert.Equal(0, empty1.SeparatorCount)
            Assert.Equal("", empty1.ToString())

            Dim empty2 = SyntaxFactory.SeparatedList(Enumerable.Empty(Of TypeParameterSyntax)())

            Assert.Equal(0, empty2.Count)
            Assert.Equal(0, empty2.SeparatorCount)
            Assert.Equal("", empty2.ToString())

            Dim singleton1 = SyntaxFactory.SeparatedList({SyntaxFactory.IdentifierName("a")})

            Assert.Equal(1, singleton1.Count)
            Assert.Equal(0, singleton1.SeparatorCount)
            Assert.Equal("a", singleton1.ToString())

            Dim singleton2 = SyntaxFactory.SeparatedList(CType({SyntaxFactory.IdentifierName("x")}, IEnumerable(Of ExpressionSyntax)))

            Assert.Equal(1, singleton2.Count)
            Assert.Equal(0, singleton2.SeparatorCount)
            Assert.Equal("x", singleton2.ToString())

            Dim list1 = SyntaxFactory.SeparatedList({SyntaxFactory.IdentifierName("a"), SyntaxFactory.IdentifierName("b"), SyntaxFactory.IdentifierName("c")})

            Assert.Equal(3, list1.Count)
            Assert.Equal(2, list1.SeparatorCount)
            Assert.Equal("a,b,c", list1.ToString())

            Dim builder = New List(Of ArgumentSyntax)()
            builder.Add(SyntaxFactory.SimpleArgument(SyntaxFactory.IdentifierName("x")))
            builder.Add(SyntaxFactory.SimpleArgument(SyntaxFactory.IdentifierName("y")))
            builder.Add(SyntaxFactory.SimpleArgument(SyntaxFactory.IdentifierName("z")))

            Dim list2 = SyntaxFactory.SeparatedList(builder)

            Assert.Equal(3, list2.Count)
            Assert.Equal(2, list2.SeparatorCount)
            Assert.Equal("x,y,z", list2.ToString())

        End Sub

        <Fact(), WorkItem(701158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/701158")>
        Public Sub FindTokenOnStartOfContinuedLine()
            Dim code =
                <code>
                Namespace a
                    &lt;TestClass&gt; _
                    Public Class UnitTest1
                   End Class
                End Namespace
            </code>.Value
            Dim text = SourceText.From(code)
            Dim tree = VisualBasicSyntaxTree.ParseText(text)
            Dim token = tree.GetRoot().FindToken(text.Lines.Item(3).Start)
            Assert.Equal(">", token.ToString())
        End Sub
        
        <Fact, WorkItem(7182, "https://github.com/dotnet/roslyn/issues/7182")>
        Public Sub WhenTextContainsTrailingTrivia_SyntaxNode_ContainsSkippedText_ReturnsTrue()
            Dim parsedTypeName = SyntaxFactory.ParseTypeName("System.Collections.Generic.List(Of Integer), mscorlib")
            Assert.True(parsedTypeName.ContainsSkippedText)
        End Sub
    End Class
End Namespace
