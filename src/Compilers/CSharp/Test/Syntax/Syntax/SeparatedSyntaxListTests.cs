// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SeparatedSyntaxListTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var node1 = SyntaxFactory.Parameter(SyntaxFactory.Identifier("a"));
            var node2 = SyntaxFactory.Parameter(SyntaxFactory.Identifier("b"));

            EqualityTesting.AssertEqual(default(SeparatedSyntaxList<CSharpSyntaxNode>), default(SeparatedSyntaxList<CSharpSyntaxNode>));

            EqualityTesting.AssertEqual(
                new SeparatedSyntaxList<CSharpSyntaxNode>(new SyntaxNodeOrTokenList(node1, 0)),
                new SeparatedSyntaxList<CSharpSyntaxNode>(new SyntaxNodeOrTokenList(node1, 0)));

            EqualityTesting.AssertEqual(
                new SeparatedSyntaxList<CSharpSyntaxNode>(new SyntaxNodeOrTokenList(node1, 0)),
                new SeparatedSyntaxList<CSharpSyntaxNode>(new SyntaxNodeOrTokenList(node1, 1)));

            EqualityTesting.AssertNotEqual(
                new SeparatedSyntaxList<CSharpSyntaxNode>(new SyntaxNodeOrTokenList(node1, 0)),
                new SeparatedSyntaxList<CSharpSyntaxNode>(new SyntaxNodeOrTokenList(node2, 0)));
        }

        [Fact]
        public void EnumeratorEquality()
        {
            Assert.Throws<NotSupportedException>(() => default(SeparatedSyntaxList<CSharpSyntaxNode>.Enumerator).GetHashCode());
            Assert.Throws<NotSupportedException>(() => default(SeparatedSyntaxList<CSharpSyntaxNode>.Enumerator).Equals(default(SeparatedSyntaxList<CSharpSyntaxNode>.Enumerator)));
        }

        [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/308077")]
        public void TestSeparatedListInsert(bool collectionExpression)
        {
            var list = collectionExpression
                ? []
                : SyntaxFactory.SeparatedList<ExpressionSyntax>();
            var addList = list.Insert(0, SyntaxFactory.ParseExpression("x"));
            Assert.Equal("x", addList.ToFullString());

            var insertBefore = addList.Insert(0, SyntaxFactory.ParseExpression("y"));
            Assert.Equal("y,x", insertBefore.ToFullString());

            var insertAfter = addList.Insert(1, SyntaxFactory.ParseExpression("y"));
            Assert.Equal("x,y", insertAfter.ToFullString());

            var insertBetween = insertAfter.InsertRange(1, new[] { SyntaxFactory.ParseExpression("a"), SyntaxFactory.ParseExpression("b"), SyntaxFactory.ParseExpression("c") });
            Assert.Equal("x,a,b,c,y", insertBetween.ToFullString());

            // inserting after a single line comment keeps separator with previous item
            var argsWithComment = SyntaxFactory.ParseArgumentList(@"(a, // a is good
b // b is better
)").Arguments;
            var insertAfterComment = argsWithComment.Insert(1, SyntaxFactory.Argument(SyntaxFactory.ParseExpression("c")));
            Assert.Equal(@"a, // a is good
c,b // b is better
", insertAfterComment.ToFullString());

            // inserting after a end of line trivia keeps separator with previous item
            var argsWithEOL = SyntaxFactory.ParseArgumentList(@"(a,
b)").Arguments;
            var insertAfterEOL = argsWithEOL.Insert(1, SyntaxFactory.Argument(SyntaxFactory.ParseExpression("c")));
            Assert.Equal(@"a,
c,b", insertAfterEOL.ToFullString());

            // inserting after any other trivia keeps separator with following item
            var argsWithMultiLineComment = SyntaxFactory.ParseArgumentList("(a, /* b is best */ b)").Arguments;
            var insertBeforeMultiLineComment = argsWithMultiLineComment.Insert(1, SyntaxFactory.Argument(SyntaxFactory.ParseExpression("c")));
            Assert.Equal("a,c, /* b is best */ b", insertBeforeMultiLineComment.ToFullString());
        }

        [Theory, CombinatorialData]
        public void TestAddInsertRemove(bool collectionExpression)
        {
            var list = collectionExpression
                ? [
                    SyntaxFactory.ParseExpression("A"),
                    SyntaxFactory.ParseExpression("B"),
                    SyntaxFactory.ParseExpression("C")]
                : SyntaxFactory.SeparatedList<SyntaxNode>(
                    new[] {
                        SyntaxFactory.ParseExpression("A"),
                        SyntaxFactory.ParseExpression("B"),
                        SyntaxFactory.ParseExpression("C") });

            Assert.Equal(3, list.Count);
            Assert.Equal("A", list[0].ToString());
            Assert.Equal("B", list[1].ToString());
            Assert.Equal("C", list[2].ToString());
            Assert.Equal("A,B,C", list.ToFullString());

            var elementA = list[0];
            var elementB = list[1];
            var elementC = list[2];

            Assert.Equal(0, list.IndexOf(elementA));
            Assert.Equal(1, list.IndexOf(elementB));
            Assert.Equal(2, list.IndexOf(elementC));

            SyntaxNode nodeD = SyntaxFactory.ParseExpression("D");
            SyntaxNode nodeE = SyntaxFactory.ParseExpression("E");

            var newList = list.Add(nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A,B,C,D", newList.ToFullString());

            newList = list.AddRange(new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A,B,C,D,E", newList.ToFullString());

            newList = list.Insert(0, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("D,A,B,C", newList.ToFullString());

            newList = list.InsertRange(0, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("D,E,A,B,C", newList.ToFullString());

            newList = list.Insert(1, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A,D,B,C", newList.ToFullString());

            newList = list.Insert(2, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A,B,D,C", newList.ToFullString());

            newList = list.Insert(3, nodeD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A,B,C,D", newList.ToFullString());

            newList = list.InsertRange(0, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("D,E,A,B,C", newList.ToFullString());

            newList = list.InsertRange(1, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A,D,E,B,C", newList.ToFullString());

            newList = list.InsertRange(2, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A,B,D,E,C", newList.ToFullString());

            newList = list.InsertRange(3, new[] { nodeD, nodeE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A,B,C,D,E", newList.ToFullString());

            newList = list.RemoveAt(0);
            Assert.Equal(2, newList.Count);
            Assert.Equal("B,C", newList.ToFullString());

            newList = list.RemoveAt(list.Count - 1);
            Assert.Equal(2, newList.Count);
            Assert.Equal("A,B", newList.ToFullString());

            newList = list.Remove(elementA);
            Assert.Equal(2, newList.Count);
            Assert.Equal("B,C", newList.ToFullString());

            newList = list.Remove(elementB);
            Assert.Equal(2, newList.Count);
            Assert.Equal("A,C", newList.ToFullString());

            newList = list.Remove(elementC);
            Assert.Equal(2, newList.Count);
            Assert.Equal("A,B", newList.ToFullString());

            newList = list.Replace(elementA, nodeD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("D,B,C", newList.ToFullString());

            newList = list.Replace(elementB, nodeD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("A,D,C", newList.ToFullString());

            newList = list.Replace(elementC, nodeD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("A,B,D", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new[] { nodeD, nodeE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("D,E,B,C", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new[] { nodeD, nodeE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A,D,E,C", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new[] { nodeD, nodeE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A,B,D,E", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new SyntaxNode[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("B,C", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new SyntaxNode[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A,C", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new SyntaxNode[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A,B", newList.ToFullString());

            Assert.Equal(-1, list.IndexOf(nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { nodeD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, new[] { nodeD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Replace(nodeD, nodeE));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ReplaceRange(nodeD, new[] { nodeE }));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxNode>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxNode>)null));
            Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(elementA, (IEnumerable<SyntaxNode>)null));
        }

        [Fact]
        public void TestAddInsertRemoveOnEmptyList()
        {
            DoTestAddInsertRemoveOnEmptyList(SyntaxFactory.SeparatedList<SyntaxNode>());
            DoTestAddInsertRemoveOnEmptyList([]);
            DoTestAddInsertRemoveOnEmptyList(default(SeparatedSyntaxList<SyntaxNode>));
        }

        private void DoTestAddInsertRemoveOnEmptyList(SeparatedSyntaxList<SyntaxNode> list)
        {
            Assert.Equal(0, list.Count);

            SyntaxNode nodeD = SyntaxFactory.ParseExpression("D");
            SyntaxNode nodeE = SyntaxFactory.ParseExpression("E");

            var newList = list.Add(nodeD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D", newList.ToFullString());

            newList = list.AddRange(new[] { nodeD, nodeE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D,E", newList.ToFullString());

            newList = list.Insert(0, nodeD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D", newList.ToFullString());

            newList = list.InsertRange(0, new[] { nodeD, nodeE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D,E", newList.ToFullString());

            newList = list.Remove(nodeD);
            Assert.Equal(0, newList.Count);

            Assert.Equal(-1, list.IndexOf(nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, nodeD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(1, new[] { nodeD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { nodeD }));
            Assert.Throws<ArgumentNullException>(() => list.Add(null));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxNode>)null));
            Assert.Throws<ArgumentNullException>(() => list.Insert(0, null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxNode>)null));
        }

        [Fact]
        public void Extensions()
        {
            var list = SyntaxFactory.SeparatedList<SyntaxNode>(
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

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/2630")]
        public void ReplaceSeparator(bool collectionExpression)
        {
            var list = collectionExpression
                ? [SyntaxFactory.IdentifierName("A"), SyntaxFactory.IdentifierName("B"), SyntaxFactory.IdentifierName("C")]
                : SyntaxFactory.SeparatedList<SyntaxNode>(
                    new[] {
                        SyntaxFactory.IdentifierName("A"),
                        SyntaxFactory.IdentifierName("B"),
                        SyntaxFactory.IdentifierName("C"),
                    });

            var newComma = SyntaxFactory.Token(
                collectionExpression ? SyntaxFactory.TriviaList(SyntaxFactory.Space) : [SyntaxFactory.Space],
                SyntaxKind.CommaToken,
                collectionExpression ? SyntaxFactory.TriviaList() : []);
            var newList = list.ReplaceSeparator(
                list.GetSeparator(1),
                newComma);
            Assert.Equal(3, newList.Count);
            Assert.Equal(2, newList.SeparatorCount);
            Assert.Equal(1, newList.GetSeparator(1).GetLeadingTrivia().Count);
        }
    }
}
