// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class SyntaxTokenListTests : CSharpTestBase
    {
        [Fact]
        public void TestEquality()
        {
            var node1 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(SyntaxTokenList), default(SyntaxTokenList));

            EqualityTesting.AssertEqual(new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0), new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0));

            // index is considered
            EqualityTesting.AssertNotEqual(new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 1), new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0));

            // position not considered:
            EqualityTesting.AssertEqual(new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 1, 0), new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0));
        }

        [Fact]
        public void TestReverse_Equality()
        {
            var node1 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(SyntaxTokenList).Reverse(), default(SyntaxTokenList).Reverse());

            EqualityTesting.AssertEqual(new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0).Reverse(), new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0).Reverse());

            // index is considered
            EqualityTesting.AssertNotEqual(new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 1).Reverse(), new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0).Reverse());

            // position not considered:
            EqualityTesting.AssertEqual(new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 1, 0).Reverse(), new SyntaxTokenList(node1, node1.ReturnKeyword.Node, 0, 0).Reverse());
        }

        [Fact]
        public void TestEnumeratorEquality()
        {
            Assert.Throws<NotSupportedException>(() => default(SyntaxTokenList.Enumerator).GetHashCode());
            Assert.Throws<NotSupportedException>(() => default(SyntaxTokenList.Enumerator).Equals(default(SyntaxTokenList.Enumerator)));
            Assert.Throws<NotSupportedException>(() => default(SyntaxTokenList.Reversed.Enumerator).GetHashCode());
            Assert.Throws<NotSupportedException>(() => default(SyntaxTokenList.Reversed.Enumerator).Equals(default(SyntaxTokenList.Reversed.Enumerator)));
        }

        [Theory, CombinatorialData]
        public void TestAddInsertRemoveReplace(bool collectionExpression)
        {
            var list = collectionExpression
                ? [SyntaxFactory.ParseToken("A "), SyntaxFactory.ParseToken("B "), SyntaxFactory.ParseToken("C ")]
                : SyntaxFactory.TokenList(SyntaxFactory.ParseToken("A "), SyntaxFactory.ParseToken("B "), SyntaxFactory.ParseToken("C "));

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

            var tokenD = SyntaxFactory.ParseToken("D ");
            var tokenE = SyntaxFactory.ParseToken("E ");

            var newList = list.Add(tokenD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B C D ", newList.ToFullString());

            newList = list.AddRange(new[] { tokenD, tokenE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A B C D E ", newList.ToFullString());

            newList = list.Insert(0, tokenD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("D A B C ", newList.ToFullString());

            newList = list.Insert(1, tokenD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A D B C ", newList.ToFullString());

            newList = list.Insert(2, tokenD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B D C ", newList.ToFullString());

            newList = list.Insert(3, tokenD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B C D ", newList.ToFullString());

            newList = list.InsertRange(0, new[] { tokenD, tokenE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("D E A B C ", newList.ToFullString());

            newList = list.InsertRange(1, new[] { tokenD, tokenE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A D E B C ", newList.ToFullString());

            newList = list.InsertRange(2, new[] { tokenD, tokenE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A B D E C ", newList.ToFullString());

            newList = list.InsertRange(3, new[] { tokenD, tokenE });
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

            newList = list.Replace(elementA, tokenD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("D B C ", newList.ToFullString());

            newList = list.Replace(elementB, tokenD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("A D C ", newList.ToFullString());

            newList = list.Replace(elementC, tokenD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("A B D ", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new[] { tokenD, tokenE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("D E B C ", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new[] { tokenD, tokenE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A D E C ", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new[] { tokenD, tokenE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B D E ", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new SyntaxToken[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("B C ", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new SyntaxToken[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A C ", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new SyntaxToken[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A B ", newList.ToFullString());

            Assert.Equal(-1, list.IndexOf(tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Add(default(SyntaxToken)));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(0, default(SyntaxToken)));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxToken>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxToken>)null));
            Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(elementA, (IEnumerable<SyntaxToken>)null));
        }

        [Fact]
        public void TestAddInsertRemoveReplaceOnEmptyList()
        {
            DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxFactory.TokenList());
            DoTestAddInsertRemoveReplaceOnEmptyList([]);
            DoTestAddInsertRemoveReplaceOnEmptyList(default(SyntaxTokenList));
        }

        private void DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxTokenList list)
        {
            Assert.Equal(0, list.Count);

            var tokenD = SyntaxFactory.ParseToken("D ");
            var tokenE = SyntaxFactory.ParseToken("E ");

            var newList = list.Add(tokenD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D ", newList.ToFullString());

            newList = list.AddRange(new[] { tokenD, tokenE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D E ", newList.ToFullString());

            newList = list.Insert(0, tokenD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D ", newList.ToFullString());

            newList = list.InsertRange(0, new[] { tokenD, tokenE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D E ", newList.ToFullString());

            newList = list.Remove(tokenD);
            Assert.Equal(0, newList.Count);

            Assert.Equal(-1, list.IndexOf(tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Replace(tokenD, tokenE));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ReplaceRange(tokenD, new[] { tokenE }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Add(default(SyntaxToken)));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(0, default(SyntaxToken)));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxToken>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxToken>)null));
        }

        [Fact]
        public void Extensions()
        {
            var list = SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.SizeOfKeyword),
                SyntaxFactory.Literal("x"),
                SyntaxFactory.Token(SyntaxKind.DotToken));

            Assert.Equal(0, list.IndexOf(SyntaxKind.SizeOfKeyword));
            Assert.True(list.Any(SyntaxKind.SizeOfKeyword));

            Assert.Equal(1, list.IndexOf(SyntaxKind.StringLiteralToken));
            Assert.True(list.Any(SyntaxKind.StringLiteralToken));

            Assert.Equal(2, list.IndexOf(SyntaxKind.DotToken));
            Assert.True(list.Any(SyntaxKind.DotToken));

            Assert.Equal(-1, list.IndexOf(SyntaxKind.NullKeyword));
            Assert.False(list.Any(SyntaxKind.NullKeyword));
        }
    }
}
