' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SyntaxListTests

        <Fact>
        Public Sub TestAddInsertRemoveReplace()
            Dim list = SyntaxFactory.List(Of SyntaxNode)({
                SyntaxFactory.ParseExpression("A "),
                SyntaxFactory.ParseExpression("B "),
                SyntaxFactory.ParseExpression("C ")})

            Assert.Equal(3, list.Count)
            Assert.Equal("A", list(0).ToString())
            Assert.Equal("B", list(1).ToString())
            Assert.Equal("C", list(2).ToString())
            Assert.Equal("A B C ", list.ToFullString())

            Dim elementA = list(0)
            Dim elementB = list(1)
            Dim elementC = list(2)

            Assert.Equal(0, list.IndexOf(elementA))
            Assert.Equal(1, list.IndexOf(elementB))
            Assert.Equal(2, list.IndexOf(elementC))

            Dim nodeD As SyntaxNode = SyntaxFactory.ParseExpression("D ")
            Dim nodeE As SyntaxNode = SyntaxFactory.ParseExpression("E ")

            Dim newList = list.Add(nodeD)
            Assert.Equal(4, newList.Count)
            Assert.Equal("A B C D ", newList.ToFullString())

            newList = list.AddRange({nodeD, nodeE})
            Assert.Equal(5, newList.Count)
            Assert.Equal("A B C D E ", newList.ToFullString())

            newList = list.Insert(0, nodeD)
            Assert.Equal(4, newList.Count)
            Assert.Equal("D A B C ", newList.ToFullString())

            newList = list.Insert(1, nodeD)
            Assert.Equal(4, newList.Count)
            Assert.Equal("A D B C ", newList.ToFullString())

            newList = list.Insert(2, nodeD)
            Assert.Equal(4, newList.Count)
            Assert.Equal("A B D C ", newList.ToFullString())

            newList = list.Insert(3, nodeD)
            Assert.Equal(4, newList.Count)
            Assert.Equal("A B C D ", newList.ToFullString())

            newList = list.InsertRange(0, {nodeD, nodeE})
            Assert.Equal(5, newList.Count)
            Assert.Equal("D E A B C ", newList.ToFullString())

            newList = list.InsertRange(1, {nodeD, nodeE})
            Assert.Equal(5, newList.Count)
            Assert.Equal("A D E B C ", newList.ToFullString())

            newList = list.InsertRange(2, {nodeD, nodeE})
            Assert.Equal(5, newList.Count)
            Assert.Equal("A B D E C ", newList.ToFullString())

            newList = list.InsertRange(3, {nodeD, nodeE})
            Assert.Equal(5, newList.Count)
            Assert.Equal("A B C D E ", newList.ToFullString())

            newList = list.RemoveAt(0)
            Assert.Equal(2, newList.Count)
            Assert.Equal("B C ", newList.ToFullString())

            newList = list.RemoveAt(list.Count - 1)
            Assert.Equal(2, newList.Count)
            Assert.Equal("A B ", newList.ToFullString())

            newList = list.Remove(elementA)
            Assert.Equal(2, newList.Count)
            Assert.Equal("B C ", newList.ToFullString())

            newList = list.Remove(elementB)
            Assert.Equal(2, newList.Count)
            Assert.Equal("A C ", newList.ToFullString())

            newList = list.Remove(elementC)
            Assert.Equal(2, newList.Count)
            Assert.Equal("A B ", newList.ToFullString())

            newList = list.Replace(elementA, nodeD)
            Assert.Equal(3, newList.Count)
            Assert.Equal("D B C ", newList.ToFullString())

            newList = list.Replace(elementB, nodeD)
            Assert.Equal(3, newList.Count)
            Assert.Equal("A D C ", newList.ToFullString())

            newList = list.Replace(elementC, nodeD)
            Assert.Equal(3, newList.Count)
            Assert.Equal("A B D ", newList.ToFullString())

            newList = list.ReplaceRange(elementA, {nodeD, nodeE})
            Assert.Equal(4, newList.Count)
            Assert.Equal("D E B C ", newList.ToFullString())

            newList = list.ReplaceRange(elementB, {nodeD, nodeE})
            Assert.Equal(4, newList.Count)
            Assert.Equal("A D E C ", newList.ToFullString())

            newList = list.ReplaceRange(elementC, {nodeD, nodeE})
            Assert.Equal(4, newList.Count)
            Assert.Equal("A B D E ", newList.ToFullString())

            newList = list.ReplaceRange(elementA, New SyntaxNode() {})
            Assert.Equal(2, newList.Count)
            Assert.Equal("B C ", newList.ToFullString())

            newList = list.ReplaceRange(elementB, New SyntaxNode() {})
            Assert.Equal(2, newList.Count)
            Assert.Equal("A C ", newList.ToFullString())

            newList = list.ReplaceRange(elementC, New SyntaxNode() {})
            Assert.Equal(2, newList.Count)
            Assert.Equal("A B ", newList.ToFullString())

            Assert.Equal(-1, list.IndexOf(nodeD))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.Insert(-1, nodeD))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.Insert(list.Count + 1, nodeD))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.InsertRange(-1, {nodeD}))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.InsertRange(list.Count + 1, {nodeD}))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.RemoveAt(-1))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.RemoveAt(list.Count))
            Assert.Throws(Of ArgumentException)(Function() list.Replace(nodeD, nodeE))
            Assert.Throws(Of ArgumentException)(Function() list.ReplaceRange(nodeD, {nodeE}))
            Assert.Throws(Of ArgumentNullException)(Function() list.Add(Nothing))
            Assert.Throws(Of ArgumentNullException)(Function() list.AddRange(DirectCast(Nothing, IEnumerable(Of SyntaxNode))))
            Assert.Throws(Of ArgumentNullException)(Function() list.Insert(0, Nothing))
            Assert.Throws(Of ArgumentNullException)(Function() list.InsertRange(0, DirectCast(Nothing, IEnumerable(Of SyntaxNode))))
            Assert.Throws(Of ArgumentNullException)(Function() list.ReplaceRange(elementA, DirectCast(Nothing, IEnumerable(Of SyntaxNode))))
        End Sub

        <Fact>
        Public Sub TestAddInsertRemoveReplaceOnEmptyList()
            DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxFactory.List(Of SyntaxNode)())
            DoTestAddInsertRemoveReplaceOnEmptyList(Nothing)
        End Sub

        Private Sub DoTestAddInsertRemoveReplaceOnEmptyList(list As SyntaxList(Of SyntaxNode))
            Assert.Equal(0, list.Count)

            Dim nodeD As SyntaxNode = SyntaxFactory.ParseExpression("D ")
            Dim nodeE As SyntaxNode = SyntaxFactory.ParseExpression("E ")

            Dim newList = list.Add(nodeD)
            Assert.Equal(1, newList.Count)
            Assert.Equal("D ", newList.ToFullString())

            newList = list.AddRange({nodeD, nodeE})
            Assert.Equal(2, newList.Count)
            Assert.Equal("D E ", newList.ToFullString())

            newList = list.Insert(0, nodeD)
            Assert.Equal(1, newList.Count)
            Assert.Equal("D ", newList.ToFullString())

            newList = list.InsertRange(0, {nodeD, nodeE})
            Assert.Equal(2, newList.Count)
            Assert.Equal("D E ", newList.ToFullString())

            newList = list.Remove(nodeD)
            Assert.Equal(0, newList.Count)

            Assert.Equal(-1, list.IndexOf(nodeD))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.RemoveAt(0))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.Insert(1, nodeD))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.Insert(-1, nodeD))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.InsertRange(1, {nodeD}))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() list.InsertRange(-1, {nodeD}))
            Assert.Throws(Of ArgumentException)(Function() list.Replace(nodeD, nodeE))
            Assert.Throws(Of ArgumentException)(Function() list.ReplaceRange(nodeD, {nodeE}))
            Assert.Throws(Of ArgumentNullException)(Function() list.Add(Nothing))
            Assert.Throws(Of ArgumentNullException)(Function() list.AddRange(DirectCast(Nothing, IEnumerable(Of SyntaxNode))))
            Assert.Throws(Of ArgumentNullException)(Function() list.Insert(0, Nothing))
            Assert.Throws(Of ArgumentNullException)(Function() list.InsertRange(0, DirectCast(Nothing, IEnumerable(Of SyntaxNode))))
        End Sub

        <Fact>
        Public Sub Extensions()
            Dim list = SyntaxFactory.List(Of SyntaxNode)(
                   {SyntaxFactory.ParseExpression("A+B"),
                    SyntaxFactory.IdentifierName("B"),
                    SyntaxFactory.ParseExpression("1")})

            Assert.Equal(0, list.IndexOf(SyntaxKind.AddExpression))
            Assert.True(list.Any(SyntaxKind.AddExpression))

            Assert.Equal(1, list.IndexOf(SyntaxKind.IdentifierName))
            Assert.True(list.Any(SyntaxKind.IdentifierName))

            Assert.Equal(2, list.IndexOf(SyntaxKind.NumericLiteralExpression))
            Assert.True(list.Any(SyntaxKind.NumericLiteralExpression))

            Assert.Equal(-1, list.IndexOf(SyntaxKind.WhereClause))
            Assert.False(list.Any(SyntaxKind.WhereClause))
        End Sub

        <Fact>
        Public Sub WithLotsOfChildrenTest()
            Dim alphabet = "abcdefghijklmnopqrstuvwxyz"
            Dim commaSeparatedList = String.Join(",", DirectCast(alphabet, IEnumerable(Of Char)))
            Dim parsedArgumentList = SyntaxFactory.ParseArgumentList("(" & commaSeparatedList & ")")
            Assert.Equal(alphabet.Length, parsedArgumentList.Arguments.Count)

            Dim openParen = ChildSyntaxList.ChildThatContainsPosition(parsedArgumentList, 0)
            Assert.True(openParen.IsKind(SyntaxKind.OpenParenToken))
            Assert.Equal(1, openParen.FullWidth)

            ' Start at 1 and stop one short to skip the open/close paren tokens
            For position = 1 To parsedArgumentList.FullWidth - 2

                Dim item = ChildSyntaxList.ChildThatContainsPosition(parsedArgumentList, position)
                Assert.Equal(position, item.Position)
                Assert.Equal(1, item.FullWidth)

                If position Mod 2 = 1 Then
                    ' Odd. We should get a node
                    Assert.True(item.IsNode)
                    Assert.True(item.IsKind(SyntaxKind.SimpleArgument))
                    Dim expectedArgName As String = ChrW(AscW("a") + (position \ 2)).ToString()
                    Assert.Equal(expectedArgName, CType(item, SimpleArgumentSyntax).Expression.ToString())
                Else
                    ' Even. We should get a comma
                    Assert.True(item.IsToken)
                    Assert.True(item.IsKind(SyntaxKind.CommaToken))
                    Assert.Equal(position, item.AsToken.Index)
                End If
            Next
        End Sub

        <Fact>
        Public Sub EnumerateWithManyChildren_Forward()
            Const n = 200000
            Dim builder As New System.Text.StringBuilder()
            builder.AppendLine("Module M")
            builder.AppendLine("    Sub Main")
            builder.Append("        Dim values As Integer() = {")
            For i = 0 To n - 1
                builder.Append("0, ")
            Next
            builder.AppendLine("}")
            builder.AppendLine("    End Sub")
            builder.AppendLine("End Module")

            Dim tree = VisualBasicSyntaxTree.ParseText(builder.ToString())
            ' Do not descend into CollectionInitializerSyntax since that will populate SeparatedWithManyChildren._children.
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of CollectionInitializerSyntax)().First()

            For Each child In node.ChildNodesAndTokens()
                child.ToString()
            Next
        End Sub

        ' Tests should timeout when using SeparatedWithManyChildren.GetChildPosition()
        ' instead of GetChildPositionFromEnd().
        <WorkItem(66475, "https://github.com/dotnet/roslyn/issues/66475")>
        <Fact>
        Public Sub EnumerateWithManyChildren_Reverse()
            Const n = 200000
            Dim builder As New System.Text.StringBuilder()
            builder.AppendLine("Module M")
            builder.AppendLine("    Sub Main")
            builder.Append("        Dim values As Integer() = {")
            For i = 0 To n - 1
                builder.Append("0, ")
            Next
            builder.AppendLine("}")
            builder.AppendLine("    End Sub")
            builder.AppendLine("End Module")

            Dim tree = VisualBasicSyntaxTree.ParseText(builder.ToString())
            ' Do not descend into CollectionInitializerSyntax since that will populate SeparatedWithManyChildren._children.
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of CollectionInitializerSyntax)().First()

            For Each child In node.ChildNodesAndTokens().Reverse()
                child.ToString()
            Next
        End Sub

    End Class
End Namespace
