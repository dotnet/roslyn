' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyntaxRewriterTests

#Region "Green Tree / SeparatedSyntaxList"

        <Fact>
        Public Sub TestGreenSeparatedDeleteNone()
            ' the argument list Is a SeparatedSyntaxList
            Dim input = "F(A,B,C)"
            Dim output = input

            Dim rewriter = New GreenRewriter()

            TestGreen(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestGreenSeparatedDeleteSome()
            ' the argument list Is a SeparatedSyntaxList
            Dim input = "F(A,B,C)"
            Dim output = "F(A,C)"

            ' delete the middle argument (should clear the following comma)
            Dim rewriter = New GreenRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SimpleArgument AndAlso node.ToString() = "B", Nothing, node)
                End Function)

            TestGreen(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestGreenSeparatedDeleteAll()
            ' the argument list Is a SeparatedSyntaxList
            Dim input = "F(A,B,C)"
            Dim output = "F()"

            ' delete all arguments, should clear the intervening commas
            Dim rewriter = New GreenRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SimpleArgument, Nothing, node)
                End Function)

            TestGreen(input, output, rewriter, isStmt:=True)
        End Sub

#End Region ' Green Tree / SeparatedSyntaxList

#Region "Green Tree / SyntaxList"

        <Fact>
        Public Sub TestGreenDeleteNone()
            ' class declarations constitute a SyntaxList
            Dim input = <![CDATA[
                Class A
                End Class
                Class B
                End Class
                Class C
                End Class
            ]]>.Value
            Dim output = input

            Dim rewriter As GreenRewriter = New GreenRewriter()

            TestGreen(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestGreenDeleteSome()
            ' class declarations constitute a SyntaxList
            Dim input = <![CDATA[
                Class A
                End Class
                Class B
                End Class
                Class C
                End Class
            ]]>.Value
            Dim output = <![CDATA[
                Class A
                End Class
                Class C
                End Class
            ]]>.Value

            Dim rewriter = New GreenRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.ClassBlock AndAlso node.ToString().Contains("B"), Nothing, node)
                End Function)

            TestGreen(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestGreenDeleteAll()
            ' class declarations constitute a SyntaxList
            Dim input = <![CDATA[
                Class A
                End Class
                Class B
                End Class
                Class C
                End Class
            ]]>.Value
            Dim output = <![CDATA[
            ]]>.Value

            ' delete all statements
            Dim rewriter = New GreenRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.ClassBlock, Nothing, node)
                End Function)

            TestGreen(input, output, rewriter, isStmt:=False)
        End Sub

#End Region ' Green Tree / SyntaxList

#Region "Red Tree / SeparatedSyntaxList"

        <Fact>
        Public Sub TestRedSeparatedDeleteNone()
            ' the argument list Is a SeparatedSyntaxList
            Dim input = "F(A,B,C)"
            Dim output = input

            Dim rewriter = New RedRewriter()

            TestRed(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestRedSeparatedDeleteSome()
            ' the argument list Is a SeparatedSyntaxList
            Dim input = "F(A,B,C)"

            ' delete the middle type argument (should clear the following comma)
            Dim rewriter = New RedRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SimpleArgument AndAlso node.ToString() = "B", Nothing, node)
                End Function)

            Dim caught As Exception = Nothing
            Try
                TestRed(input, "", rewriter, isStmt:=True)
            Catch ex As InvalidOperationException
                caught = ex
            End Try

            Assert.NotNull(caught)
        End Sub

        <Fact>
        Public Sub TestRedSeparatedDeleteAll()
            ' the argument list Is a SeparatedSyntaxList
            Dim input = "F(A,B,C)"

            ' delete all arguments, should clear the intervening commas
            Dim rewriter = New RedRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SimpleArgument, Nothing, node)
                End Function)

            Dim caught As Exception = Nothing
            Try
                TestRed(input, "", rewriter, isStmt:=True)
            Catch ex As InvalidOperationException
                caught = ex
            End Try

            Assert.NotNull(caught)
        End Sub

#End Region ' Red Tree / SeparatedSyntaxList

#Region "Red Tree / SyntaxTokenList"

        <Fact>
        Public Sub TestRedTokenDeleteNone()
            ' commas in an implicit array creation constitute a SyntaxTokenList
            Dim input = <![CDATA[
                Class c
                Sub s(x as Boolean(,,))
                End Sub
                End Class
            ]]>.Value
            Dim output = input

            Dim rewriter As RedRewriter = New RedRewriter()

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestRedTokenDeleteSome()
            ' commas in an implicit array creation constitute a SyntaxTokenList
            Dim input = <![CDATA[
                Class c
                Sub s(x as Boolean(,,))
                End Sub
                End Class
            ]]>.Value
            Dim output = <![CDATA[
                Class c
                Sub s(x as Boolean(,))
                End Sub
                End Class
            ]]>.Value

            ' delete one comma
            Dim first As Boolean = True
            Dim rewriter = New RedRewriter(rewriteToken:=
                Function(token)
                    If token.Kind = SyntaxKind.CommaToken AndAlso first Then
                        first = False
                        Return Nothing
                    End If
                    Return token
                End Function)

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestRedTokenDeleteAll()
            ' commas in an implicit array creation constitute a SyntaxTokenList
            Dim input = <![CDATA[
                Class c
                Sub s(x as Boolean(,,))
                End Sub
                End Class
            ]]>.Value
            Dim output = <![CDATA[
                Class c
                Sub s(x as Boolean())
                End Sub
                End Class
            ]]>.Value

            ' delete all commas
            Dim rewriter = New RedRewriter(rewriteToken:=
                Function(token)
                    Return If(token.Kind = SyntaxKind.CommaToken, Nothing, token)
                End Function)

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

#End Region ' Red Tree / SyntaxTokenList

#Region "Red Tree / SyntaxNodeOrTokenList"

        ' These only in the syntax tree inside SeparatedSyntaxLists, so they are not visitable.
        ' We can't call this directly due to its protection level.

#End Region ' Red Tree / SyntaxNodeOrTokenList

#Region "Red Tree / SyntaxTriviaList"

        <Fact>
        Public Sub TestRedTriviaDeleteNone()
            ' whitespace and comments constitute a SyntaxTriviaList
            Dim input = " a() ' comment"
            Dim output = input

            Dim rewriter As RedRewriter = New RedRewriter()

            TestRed(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestRedTriviaDeleteSome()
            ' whitespace and comments constitute a SyntaxTriviaList
            Dim input = " a() ' comment"
            Dim output = "a()' comment"

            ' delete all whitespace trivia (leave comments)
            Dim rewriter = New RedRewriter(rewriteTrivia:=
                Function(trivia)
                    Return If(trivia.Kind = SyntaxKind.WhitespaceTrivia, Nothing, trivia)
                End Function)

            TestRed(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestRedTriviaDeleteAll()
            ' whitespace and comments constitute a SyntaxTriviaList
            Dim input = " a() ' comment"
            Dim output = "a()"

            ' delete all trivia
            Dim rewriter = New RedRewriter(rewriteTrivia:=Function(trivia) Nothing)

            TestRed(input, output, rewriter, isStmt:=True)
        End Sub

#End Region ' Red Tree / SyntaxTriviaList

#Region "Red Tree / SyntaxList"

        <Fact>
        Public Sub TestRedDeleteNone()
            ' attributes are a SyntaxList
            Dim input = <![CDATA[
                <Attr1()>
                <Attr2()>
                <Attr3()>
                Class Q
                End Class
            ]]>.Value
            Dim output = input

            Dim rewriter = New RedRewriter()

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestRedDeleteSome()
            ' attributes are a SyntaxList
            Dim input = <![CDATA[
                <Attr1()>
                <Attr2()>
                <Attr3()>
                Class Q
                End Class
            ]]>.Value
            Dim output = <![CDATA[
                <Attr1()>
                <Attr3()>
                Class Q
                End Class
            ]]>.Value

            Dim rewriter = New RedRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.AttributeList AndAlso node.ToString().Contains("2"), Nothing, node)
                End Function)

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestRedDeleteAll()
            ' attributes are a SyntaxList
            Dim input = <![CDATA[
                <Attr1()>
                <Attr2()>
                <Attr3()>
                Class Q
                End Class
            ]]>.Value
            Dim output = <![CDATA[
                Class Q
                End Class
            ]]>.Value

            Dim rewriter = New RedRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.AttributeList, Nothing, node)
                End Function)

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

#End Region ' Red Tree / SyntaxList

#Region "Misc"

        <Fact>
        Public Sub TestRedSeparatedDeleteLast()
            Dim input = "F(A,B,C)"
            Dim output = "F(A,B,)"

            ' delete the last argument (should clear the *preceding* comma)
            Dim rewriter = New RedRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SimpleArgument AndAlso node.ToString() = "C", Nothing, node)
                End Function)

            TestRed(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestGreenSeparatedDeleteLast()
            Dim input = "F(A,B,C)"
            Dim output = "F(A,B)"

            ' delete the last argument (should clear the *preceding* comma)
            Dim rewriter = New GreenRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SimpleArgument AndAlso node.ToString() = "C", Nothing, node)
                End Function)

            TestGreen(input, output, rewriter, isStmt:=True)
        End Sub

        <Fact>
        Public Sub TestRedSeparatedDeleteLastWithTrailingSeparator()
            Dim input = <![CDATA[
                Class Q
                Sub A()
                End Sub
                Sub B()
                End Sub
                End Class
            ]]>.Value

            Dim rewriter = New RedRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SubBlock AndAlso node.ToString().Contains("B"), Nothing, node)
                End Function)

            Dim caught As Exception = Nothing
            Dim output = <![CDATA[
                Class Q
                Sub A()
                End Sub
                End Class
            ]]>.Value

            TestRed(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestGreenSeparatedDeleteLastWithTrailingSeparator()
            Dim input = <![CDATA[
                Class Q
                Sub A()
                End Sub
                Sub B()
                End Sub
                End Class
            ]]>.Value
            Dim output = <![CDATA[
                Class Q
                Sub A()
                End Sub
                End Class
            ]]>.Value

            Dim rewriter = New GreenRewriter(rewriteNode:=
                Function(node)
                    Return If(node.Kind = SyntaxKind.SubBlock AndAlso node.ToString().Contains("B"), Nothing, node)
                End Function)

            TestGreen(input, output, rewriter, isStmt:=False)
        End Sub

        <Fact>
        Public Sub TestRedSeparatedDeleteSeparator()
            Dim red = SyntaxFactory.ParseExecutableStatement("F(A,B,C)")

            Assert.False(red.ContainsDiagnostics)

            Dim rewriter = New RedRewriter(rewriteToken:=
                Function(token)
                    Return If(token.Kind = SyntaxKind.CommaToken, Nothing, token)
                End Function)

            Assert.Throws(Of InvalidOperationException)(Sub() rewriter.Visit(red))
        End Sub

        <Fact>
        Public Sub TestCreateSyntaxTreeWithoutClone()
            ' Test SyntaxTree.CreateWithoutClone() implicitly invoked by accessing the SyntaxTree property.
            ' Ensure this API preserves reference equality of the syntax node.
            Dim expression = SyntaxFactory.ParseExpression("0")
            Dim tree = expression.SyntaxTree
            Assert.Same(expression, tree.GetRoot())
            Assert.False(tree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?")
        End Sub

        <Fact>
        Public Sub RemoveDocCommentNode()
            Dim oldSource = <![CDATA[
''' <see cref='C'/>
Class C
End Class
]]>

            Dim expectedNewSource = <![CDATA[
''' 
Class C
End Class
]]>

            Dim oldTree = VisualBasicSyntaxTree.ParseText(oldSource.Value, options:=New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose))
            Dim oldRoot = oldTree.GetRoot()
            Dim xmlNode = oldRoot.DescendantNodes(descendIntoTrivia:=True).OfType(Of XmlEmptyElementSyntax)().Single()
            Dim newRoot = oldRoot.RemoveNode(xmlNode, SyntaxRemoveOptions.KeepDirectives)

            Assert.Equal(expectedNewSource.Value, newRoot.ToFullString())
        End Sub

        <WorkItem(991474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991474")>
        <Fact>
        Public Sub ReturnNothingFromStructuredTriviaRoot_Succeeds()
            Dim Text =
<x>#Region
Class C 
End Class
#End Region"</x>.Value

            Dim expectedText =
<x>Class C 
End Class
#End Region"</x>.Value

            Dim root = SyntaxFactory.ParseCompilationUnit(Text)
            Dim newRoot = New RemoveRegionRewriter().Visit(root)

            Assert.Equal(expectedText, newRoot.ToFullString())
        End Sub

        Private Class RemoveRegionRewriter
            Inherits VisualBasicSyntaxRewriter

            Public Sub New()
                MyBase.New(visitIntoStructuredTrivia:=True)
            End Sub

            Public Overrides Function VisitRegionDirectiveTrivia(node As RegionDirectiveTriviaSyntax) As SyntaxNode
                Return Nothing
            End Function

        End Class

#End Region ' Misc

#Region "Helper Types"

        Private Sub TestGreen(input As String, output As String, rewriter As GreenRewriter, isStmt As Boolean)
            Dim red As VisualBasicSyntaxNode
            If isStmt Then
                red = SyntaxFactory.ParseExecutableStatement(input)
            Else
                red = SyntaxFactory.ParseCompilationUnit(input)
            End If

            Dim green = red.ToGreen()

            Assert.False(green.ContainsDiagnostics)

            Dim result As InternalSyntax.VisualBasicSyntaxNode = rewriter.Visit(green)

            Assert.Equal(input = output, green Is result)
            Assert.Equal(output.Trim(), result.ToFullString().Trim())
        End Sub

        Private Sub TestRed(input As String, output As String, rewriter As RedRewriter, isStmt As Boolean)
            Dim red As VisualBasicSyntaxNode
            If isStmt Then
                red = SyntaxFactory.ParseExecutableStatement(input)
            Else
                red = SyntaxFactory.ParseCompilationUnit(input)
            End If

            Assert.False(red.ContainsDiagnostics)

            Dim result = rewriter.Visit(red)

            Assert.Equal(input = output, red Is result)
            Assert.Equal(output.Trim(), result.ToFullString().Trim())
        End Sub

#End Region ' Helper Types

#Region "Helper Types"

        ''' <summary>
        ''' This Rewriter exposes delegates for the methods that would normally be overridden.
        ''' </summary>
        Friend Class GreenRewriter
            Inherits InternalSyntax.VisualBasicSyntaxRewriter

            Private ReadOnly _rewriteNode As Func(Of InternalSyntax.VisualBasicSyntaxNode, InternalSyntax.VisualBasicSyntaxNode)
            Private ReadOnly _rewriteToken As Func(Of InternalSyntax.SyntaxToken, InternalSyntax.SyntaxToken)
            Private ReadOnly _rewriteTrivia As Func(Of InternalSyntax.SyntaxTrivia, InternalSyntax.SyntaxTrivia)

            Friend Sub New(
                    Optional rewriteNode As Func(Of InternalSyntax.VisualBasicSyntaxNode, InternalSyntax.VisualBasicSyntaxNode) = Nothing,
                    Optional rewriteToken As Func(Of InternalSyntax.SyntaxToken, InternalSyntax.SyntaxToken) = Nothing,
                    Optional rewriteTrivia As Func(Of InternalSyntax.SyntaxTrivia, InternalSyntax.SyntaxTrivia) = Nothing)
                Me._rewriteNode = rewriteNode
                Me._rewriteToken = rewriteToken
                Me._rewriteTrivia = rewriteTrivia
            End Sub

            Public Overrides Function Visit(node As InternalSyntax.VisualBasicSyntaxNode) As InternalSyntax.VisualBasicSyntaxNode
                Dim visited As InternalSyntax.VisualBasicSyntaxNode = MyBase.Visit(node)
                If _rewriteNode Is Nothing OrElse visited Is Nothing Then
                    Return visited
                Else
                    Return _rewriteNode(visited)
                End If
            End Function

            Public Overrides Function VisitSyntaxToken(token As InternalSyntax.SyntaxToken) As InternalSyntax.SyntaxToken
                Dim visited = MyBase.VisitSyntaxToken(token)
                If _rewriteToken Is Nothing Then
                    Return visited
                Else
                    Return _rewriteToken(visited)
                End If
            End Function

            Public Overrides Function VisitSyntaxTrivia(trivia As InternalSyntax.SyntaxTrivia) As InternalSyntax.SyntaxTrivia
                Dim visited As InternalSyntax.SyntaxTrivia = MyBase.VisitSyntaxTrivia(trivia)
                If _rewriteTrivia Is Nothing Then
                    Return visited
                Else
                    Return _rewriteTrivia(visited)
                End If
            End Function
        End Class


        ''' <summary>
        ''' This Rewriter exposes delegates for the methods that would normally be overridden.
        ''' </summary>
        Friend Class RedRewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _rewriteNode As Func(Of SyntaxNode, SyntaxNode)
            Private ReadOnly _rewriteToken As Func(Of SyntaxToken, SyntaxToken)
            Private ReadOnly _rewriteTrivia As Func(Of SyntaxTrivia, SyntaxTrivia)

            Friend Sub New(
                    Optional rewriteNode As Func(Of SyntaxNode, SyntaxNode) = Nothing,
                    Optional rewriteToken As Func(Of SyntaxToken, SyntaxToken) = Nothing,
                    Optional rewriteTrivia As Func(Of SyntaxTrivia, SyntaxTrivia) = Nothing)
                Me._rewriteNode = rewriteNode
                Me._rewriteToken = rewriteToken
                Me._rewriteTrivia = rewriteTrivia
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Dim visited = MyBase.Visit(node)
                If _rewriteNode Is Nothing OrElse visited Is Nothing Then
                    Return visited
                Else
                    Return _rewriteNode(visited)
                End If
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim visited As SyntaxToken = MyBase.VisitToken(token)
                If _rewriteToken Is Nothing Then
                    Return visited
                Else
                    Return _rewriteToken(visited)
                End If
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                Dim visited As SyntaxTrivia = MyBase.VisitTrivia(trivia)
                If _rewriteTrivia Is Nothing Then
                    Return visited
                Else
                    Return _rewriteTrivia(visited)
                End If
            End Function
        End Class

#End Region ' Helper Types
    End Class

End Namespace
