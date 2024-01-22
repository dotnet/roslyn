// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxListTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var node1 = SyntaxFactory.ReturnStatement();
            var node2 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(SyntaxList<CSharpSyntaxNode>), default(SyntaxList<CSharpSyntaxNode>));
            EqualityTesting.AssertEqual(new SyntaxList<CSharpSyntaxNode>(node1), new SyntaxList<CSharpSyntaxNode>(node1));

            EqualityTesting.AssertNotEqual(new SyntaxList<CSharpSyntaxNode>(node1), new SyntaxList<CSharpSyntaxNode>(node2));
        }

        [Fact]
        public void EnumeratorEquality()
        {
            Assert.Throws<NotSupportedException>(() => default(SyntaxList<CSharpSyntaxNode>.Enumerator).GetHashCode());
            Assert.Throws<NotSupportedException>(() => default(SyntaxList<CSharpSyntaxNode>.Enumerator).Equals(default(SyntaxList<CSharpSyntaxNode>.Enumerator)));
        }

        [Theory, CombinatorialData]
        public void TestAddInsertRemoveReplace(bool collectionExpression)
        {
            var list = collectionExpression
                ? [
                    SyntaxFactory.ParseExpression("A "),
                    SyntaxFactory.ParseExpression("B "),
                    SyntaxFactory.ParseExpression("C ")]
                : SyntaxFactory.List<SyntaxNode>(
                    new[] {
                        SyntaxFactory.ParseExpression("A "),
                        SyntaxFactory.ParseExpression("B "),
                        SyntaxFactory.ParseExpression("C ") });

            Assert.Equal(3, list.Count);
            Assert.Equal("A", list[0].ToString());
            Assert.Equal("B", list[1].ToString());
            Assert.Equal("C", list[2].ToString());
            Assert.Equal("A B C ", list.ToFullString());

            var elementA = list[0];
            var elementB = list[1];
            var elementC = list[2];

            Assert.Equal(0, list.IndexOf(elementA));
            Assert.Equal(1, list.IndexOf(elementB));
            Assert.Equal(2, list.IndexOf(elementC));

            SyntaxNode nodeD = SyntaxFactory.ParseExpression("D ");
            SyntaxNode nodeE = SyntaxFactory.ParseExpression("E ");

            var newList = list.Add(nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B C D ", newList.ToFullString());

            newList = list.AddRange(new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A B C D E ", newList.ToFullString());

            newList = list.Insert(0, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("D A B C ", newList.ToFullString());

            newList = list.Insert(1, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A D B C ", newList.ToFullString());

            newList = list.Insert(2, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B D C ", newList.ToFullString());

            newList = list.Insert(3, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B C D ", newList.ToFullString());

            newList = list.InsertRange(0, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("D E A B C ", newList.ToFullString());

            newList = list.InsertRange(1, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A D E B C ", newList.ToFullString());

            newList = list.InsertRange(2, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A B D E C ", newList.ToFullString());

            newList = list.InsertRange(3, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A B C D E ", newList.ToFullString());

            newList = list.RemoveAt(0);
            Assert.Equal(2, newList.Count);
            Assert.Equal("B C ", newList.ToFullString());

            newList = list.RemoveAt(list.Count - 1);
            Assert.Equal(2, newList.Count);
            Assert.Equal("A B ", newList.ToFullString());

            newList = list.Remove(elementA);
            Assert.Equal(2, newList.Count);
            Assert.Equal("B C ", newList.ToFullString());

            newList = list.Remove(elementB);
            Assert.Equal(2, newList.Count);
            Assert.Equal("A C ", newList.ToFullString());

            newList = list.Remove(elementC);
            Assert.Equal(2, newList.Count);
            Assert.Equal("A B ", newList.ToFullString());

            newList = list.Replace(elementA, nodeD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("D B C ", newList.ToFullString());

            newList = list.Replace(elementB, nodeD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("A D C ", newList.ToFullString());

            newList = list.Replace(elementC, nodeD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("A B D ", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new[] { nodeD, nodeE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("D E B C ", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new[] { nodeD, nodeE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A D E C ", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new[] { nodeD, nodeE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B D E ", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new SyntaxNode[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("B C ", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new SyntaxNode[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A C ", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new SyntaxNode[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A B ", newList.ToFullString());

            Assert.Equal(-1, list.IndexOf(nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { nodeD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, new[] { nodeD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
            Assert.Throws<ArgumentException>(() => list.Replace(nodeD, nodeE));
            Assert.Throws<ArgumentException>(() => list.ReplaceRange(nodeD, new[] { nodeE }));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxNode>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxNode>)null));
            Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(elementA, (IEnumerable<SyntaxNode>)null));
        }

        [Fact]
        public void TestAddInsertRemoveReplaceOnEmptyList()
        {
            DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxFactory.List<SyntaxNode>());
            DoTestAddInsertRemoveReplaceOnEmptyList([]);
            DoTestAddInsertRemoveReplaceOnEmptyList(default(SyntaxList<SyntaxNode>));
        }

        private void DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxList<SyntaxNode> list)
        {
            Assert.Equal(0, list.Count);

            SyntaxNode nodeD = SyntaxFactory.ParseExpression("D ");
            SyntaxNode nodeE = SyntaxFactory.ParseExpression("E ");

            var newList = list.Add(nodeD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D ", newList.ToFullString());

            newList = list.AddRange(new[] { nodeD, nodeE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D E ", newList.ToFullString());

            newList = list.Insert(0, nodeD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D ", newList.ToFullString());

            newList = list.InsertRange(0, new[] { nodeD, nodeE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D E ", newList.ToFullString());

            newList = list.Remove(nodeD);
            Assert.Equal(0, newList.Count);

            Assert.Equal(-1, list.IndexOf(nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(1, new[] { nodeD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { nodeD }));
            Assert.Throws<ArgumentException>(() => list.Replace(nodeD, nodeE));
            Assert.Throws<ArgumentException>(() => list.ReplaceRange(nodeD, new[] { nodeE }));
            Assert.Throws<ArgumentNullException>(() => list.Add(null));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxNode>)null));
            Assert.Throws<ArgumentNullException>(() => list.Insert(0, null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxNode>)null));
        }

        [Fact, WorkItem(127, "https://github.com/dotnet/roslyn/issues/127")]
        public void AddEmptySyntaxList()
        {
            var attributes = new AttributeListSyntax[0];
            var newMethodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "M");
            newMethodDeclaration.AddAttributeLists(attributes);
        }

        [Theory, CombinatorialData]
        public void AddNamespaceAttributeListsAndModifiers(bool collectionExpression)
        {
            var declaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("M"));

            Assert.True(declaration.AttributeLists.Count == 0);
            Assert.True(declaration.Modifiers.Count == 0);

            declaration = declaration.AddAttributeLists(new[]
            {
                SyntaxFactory.AttributeList(collectionExpression
                    ? [SyntaxFactory.Attribute(SyntaxFactory.ParseName("Attr"))]
                    : SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("Attr")))),
            });

            Assert.True(declaration.AttributeLists.Count == 1);
            Assert.True(declaration.Modifiers.Count == 0);

            declaration = declaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            Assert.True(declaration.AttributeLists.Count == 1);
            Assert.True(declaration.Modifiers.Count == 1);
        }

        [Fact]
        public void Extensions()
        {
            var list = SyntaxFactory.List<SyntaxNode>(
                new[] {
                    SyntaxFactory.ParseExpression("A+B"),
                    SyntaxFactory.IdentifierName("B"),
                    SyntaxFactory.ParseExpression("1") });

            Assert.Equal(0, list.IndexOf(SyntaxKind.AddExpression));
            Assert.True(list.Any(SyntaxKind.AddExpression));

            Assert.Equal(1, list.IndexOf(SyntaxKind.IdentifierName));
            Assert.True(list.Any(SyntaxKind.IdentifierName));

            Assert.Equal(2, list.IndexOf(SyntaxKind.NumericLiteralExpression));
            Assert.True(list.Any(SyntaxKind.NumericLiteralExpression));

            Assert.Equal(-1, list.IndexOf(SyntaxKind.WhereClause));
            Assert.False(list.Any(SyntaxKind.WhereClause));
        }

        [Fact]
        public void WithLotsOfChildrenTest()
        {
            var alphabet = "abcdefghijklmnopqrstuvwxyz";
            var commaSeparatedList = string.Join(",", (IEnumerable<char>)alphabet);
            var parsedArgumentList = SyntaxFactory.ParseArgumentList(commaSeparatedList);
            Assert.Equal(alphabet.Length, parsedArgumentList.Arguments.Count);

            for (int position = 0; position < parsedArgumentList.FullWidth; position++)
            {
                var item = ChildSyntaxList.ChildThatContainsPosition(parsedArgumentList, position);
                Assert.Equal(position, item.Position);
                Assert.Equal(1, item.Width);
                if (position % 2 == 0)
                {
                    // Even. We should get a node
                    Assert.True(item.IsNode);
                    Assert.True(item.IsKind(SyntaxKind.Argument));
                    string expectedArgName = ((char)('a' + (position / 2))).ToString();
                    Assert.Equal(expectedArgName, ((ArgumentSyntax)item).Expression.ToString());
                }
                else
                {
                    // Odd. We should get a comma
                    Assert.True(item.IsToken);
                    Assert.True(item.IsKind(SyntaxKind.CommaToken));
                    int expectedTokenIndex = position + 1; // + 1 because there is a (missing) OpenParen at slot 0
                    Assert.Equal(expectedTokenIndex, item.AsToken().Index);
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void EnumerateWithManyChildren_Forward(bool trailingSeparator)
        {
            const int n = 200000;
            var builder = new StringBuilder();
            builder.Append("int[] values = new[] { ");
            for (int i = 0; i < n; i++) builder.Append("0, ");
            if (!trailingSeparator) builder.Append("0 ");
            builder.AppendLine("};");

            var tree = CSharpSyntaxTree.ParseText(builder.ToString());
            // Do not descend into InitializerExpressionSyntax since that will populate SeparatedWithManyChildren._children.
            var node = tree.GetRoot().DescendantNodes().OfType<InitializerExpressionSyntax>().First();

            foreach (var child in node.ChildNodesAndTokens())
            {
                _ = child.ToString();
            }
        }

        // Tests should timeout when using SeparatedWithManyChildren.GetChildPosition()
        // instead of GetChildPositionFromEnd().
        [WorkItem(66475, "https://github.com/dotnet/roslyn/issues/66475")]
        [Theory]
        [CombinatorialData]
        public void EnumerateWithManyChildren_Reverse(bool trailingSeparator)
        {
            const int n = 200000;
            var builder = new StringBuilder();
            builder.Append("int[] values = new[] { ");
            for (int i = 0; i < n; i++) builder.Append("0, ");
            if (!trailingSeparator) builder.Append("0 ");
            builder.AppendLine("};");

            var tree = CSharpSyntaxTree.ParseText(builder.ToString());
            // Do not descend into InitializerExpressionSyntax since that will populate SeparatedWithManyChildren._children.
            var node = tree.GetRoot().DescendantNodes().OfType<InitializerExpressionSyntax>().First();

            foreach (var child in node.ChildNodesAndTokens().Reverse())
            {
                _ = child.ToString();
            }
        }

        [Theory]
        [InlineData("int[] values = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };")]
        [InlineData("int[] values = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };")]
        public void EnumerateWithManyChildren_Compare(string source)
        {
            CSharpSyntaxTree.ParseText(source).VerifyChildNodePositions();

            var builder = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
            foreach (var node in parseAndGetInitializer(source).ChildNodesAndTokens().Reverse())
            {
                builder.Add(node);
            }
            builder.ReverseContents();
            var childNodes1 = builder.ToImmutableAndFree();

            builder = ArrayBuilder<SyntaxNodeOrToken>.GetInstance();
            foreach (var node in parseAndGetInitializer(source).ChildNodesAndTokens())
            {
                builder.Add(node);
            }
            var childNodes2 = builder.ToImmutableAndFree();

            Assert.Equal(childNodes1.Length, childNodes2.Length);

            for (int i = 0; i < childNodes1.Length; i++)
            {
                var child1 = childNodes1[i];
                var child2 = childNodes2[i];
                Assert.Equal(child1.Position, child2.Position);
                Assert.Equal(child1.EndPosition, child2.EndPosition);
                Assert.Equal(child1.Width, child2.Width);
                Assert.Equal(child1.FullWidth, child2.FullWidth);
            }

            static InitializerExpressionSyntax parseAndGetInitializer(string source)
            {
                var tree = CSharpSyntaxTree.ParseText(source);
                // Do not descend into InitializerExpressionSyntax since that will populate SeparatedWithManyChildren._children.
                return tree.GetRoot().DescendantNodes().OfType<InitializerExpressionSyntax>().First();
            }
        }
    }
}
