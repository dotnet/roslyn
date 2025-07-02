' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

    Public Class SyntaxUtilitiesTests

        Private Shared Sub VerifySyntaxMap(oldSource As String, newSource As String)

            Dim oldRoot = SyntaxFactory.ParseSyntaxTree(oldSource).GetRoot()
            Dim newRoot = SyntaxFactory.ParseSyntaxTree(newSource).GetRoot()

            For Each oldNode In oldRoot.DescendantNodes().Where(Function(n) n.FullSpan.Length > 0)
                Dim newNode = AbstractEditAndContinueAnalyzer.FindPartner(newRoot, oldRoot, oldNode)
                Assert.True(SyntaxFactory.AreEquivalent(oldNode, newNode), $"Node 'oldNodeEnd' not equivalent to 'newNodeEnd'.")
            Next
        End Sub

        <Fact>
        Public Sub FindPartner1()
            Dim source1 = "
Imports System
Class C
    Shared Sub Main(args As String())
    

        ' sdasd
        Dim b = true
        Do
            Console.WriteLine(""hi"")
        While b = True
    End Sub
End Class
"

            Dim source2 = "
Imports System
Class C
    Shared Sub Main(args As String())
        Dim b = true
        Do
            Console.WriteLine(""hi"")
        While b = True
    End Sub
End Class
"
            VerifySyntaxMap(source1, source2)
        End Sub

        <Fact>
        Public Sub FindLeafNodeAndPartner1()
            Dim leftRoot = SyntaxFactory.ParseSyntaxTree("
Imports System;

Class C
    Public Sub M()
        If 0 = 1 Then
            Console.WriteLine(0)
        End If
    End Sub
End Class
").GetRoot()
            Dim leftPosition = leftRoot.DescendantNodes().OfType(Of LiteralExpressionSyntax).ElementAt(2).SpanStart '0 within Console.WriteLine(0)
            Dim rightRoot = SyntaxFactory.ParseSyntaxTree("
Imports System;

Class C
    Public Sub M()
        If 0 = 1 Then
            If 2 = 3 Then
                Console.WriteLine(0)
            End If
        End If
    End Sub
End Class
").GetRoot()

            Dim leftNode As SyntaxNode = Nothing
            Dim rightNodeOpt As SyntaxNode = Nothing
            AbstractEditAndContinueAnalyzer.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, leftNode, rightNodeOpt)
            Assert.Equal("0", leftNode.ToString())
            Assert.Null(rightNodeOpt)
        End Sub

        <Fact>
        Public Sub FindLeafNodeAndPartner2()
            ' Check that the method does Not fail even if the index of the child (4) 
            ' is greater than the count of children on the corresponding (from the upper side) node (3).
            Dim leftRoot = SyntaxFactory.ParseSyntaxTree("
Imports System;

Class C
    Public Sub M()
        If 0 = 1 Then
            Console.WriteLine(0)
            Console.WriteLine(1)
            Console.WriteLine(2)
            Console.WriteLine(3)
        End If
    End Sub
End Class
").GetRoot()

            Dim leftPosition = leftRoot.DescendantNodes().OfType(Of LiteralExpressionSyntax).ElementAt(5).SpanStart '3 within Console.WriteLine(3)
            Dim rightRoot = SyntaxFactory.ParseSyntaxTree("
Imports System;

Class C
    Public Sub M()
        If 0 = 1 Then
            If 2 = 3 Then
                Console.WriteLine(0)
                Console.WriteLine(1)
                Console.WriteLine(2)
                Console.WriteLine(3)
            End If
        End If
    End Sub
End Class
").GetRoot()

            Dim leftNode As SyntaxNode = Nothing
            Dim rightNodeOpt As SyntaxNode = Nothing
            AbstractEditAndContinueAnalyzer.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, leftNode, rightNodeOpt)
            Assert.Equal("3", leftNode.ToString())
            Assert.Null(rightNodeOpt)
        End Sub
    End Class
End Namespace
