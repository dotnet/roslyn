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
    public class SyntaxNodeOrTokenListTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var node1 = SyntaxFactory.Parameter(SyntaxFactory.Identifier("a"));
            var node2 = SyntaxFactory.Parameter(SyntaxFactory.Identifier("b"));

            EqualityTesting.AssertEqual(default(SeparatedSyntaxList<CSharpSyntaxNode>), default(SeparatedSyntaxList<CSharpSyntaxNode>));
            EqualityTesting.AssertEqual(new SyntaxNodeOrTokenList(node1, 0), new SyntaxNodeOrTokenList(node1, 0));
            EqualityTesting.AssertEqual(new SyntaxNodeOrTokenList(node1, 0), new SyntaxNodeOrTokenList(node1, 1));
            EqualityTesting.AssertNotEqual(new SyntaxNodeOrTokenList(node1, 0), new SyntaxNodeOrTokenList(node2, 0));
        }

        [Fact]
        public void EnumeratorEquality()
        {
            Assert.Throws<NotSupportedException>(() => default(SyntaxNodeOrTokenList.Enumerator).GetHashCode());
            Assert.Throws<NotSupportedException>(() => default(SyntaxNodeOrTokenList.Enumerator).Equals(default(SyntaxNodeOrTokenList.Enumerator)));
        }

        [Theory, CombinatorialData]
        public void TestAddInsertRemove(bool collectionExpression)
        {
            var list = collectionExpression
                ? [SyntaxFactory.ParseToken("A "), SyntaxFactory.ParseToken("B "), SyntaxFactory.ParseToken("C ")]
                : SyntaxFactory.NodeOrTokenList(SyntaxFactory.ParseToken("A "), SyntaxFactory.ParseToken("B "), SyntaxFactory.ParseToken("C "));

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

            SyntaxNodeOrToken tokenD = SyntaxFactory.ParseToken("D ");
            SyntaxNodeOrToken nameE = SyntaxFactory.ParseExpression("E ");

            var newList = list.Add(tokenD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B C D ", newList.ToFullString());

            newList = list.AddRange(new[] { tokenD, nameE });
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

            newList = list.InsertRange(0, new[] { tokenD, nameE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("D E A B C ", newList.ToFullString());

            newList = list.InsertRange(1, new[] { tokenD, nameE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A D E B C ", newList.ToFullString());

            newList = list.InsertRange(2, new[] { tokenD, nameE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("A B D E C ", newList.ToFullString());

            newList = list.InsertRange(3, new[] { tokenD, nameE });
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

            newList = list.ReplaceRange(elementA, new[] { tokenD, nameE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("D E B C ", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new[] { tokenD, nameE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A D E C ", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new[] { tokenD, nameE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("A B D E ", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new SyntaxNodeOrToken[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("B C ", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new SyntaxNodeOrToken[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A C ", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new SyntaxNodeOrToken[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("A B ", newList.ToFullString());

            Assert.Equal(-1, list.IndexOf(tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Replace(tokenD, nameE));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ReplaceRange(tokenD, new[] { nameE }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Add(default(SyntaxNodeOrToken)));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(0, default(SyntaxNodeOrToken)));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxNodeOrToken>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxNodeOrToken>)null));
            Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(elementA, (IEnumerable<SyntaxNodeOrToken>)null));
        }

        [Fact]
        public void TestAddInsertRemoveReplaceOnEmptyList()
        {
            DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxFactory.NodeOrTokenList());
            DoTestAddInsertRemoveReplaceOnEmptyList([]);
            DoTestAddInsertRemoveReplaceOnEmptyList(default(SyntaxNodeOrTokenList));
        }

        private void DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxNodeOrTokenList list)
        {
            Assert.Equal(0, list.Count);

            SyntaxNodeOrToken tokenD = SyntaxFactory.ParseToken("D ");
            SyntaxNodeOrToken nodeE = SyntaxFactory.ParseExpression("E ");

            var newList = list.Add(tokenD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D ", newList.ToFullString());

            newList = list.AddRange(new[] { tokenD, nodeE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D E ", newList.ToFullString());

            newList = list.Insert(0, tokenD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("D ", newList.ToFullString());

            newList = list.InsertRange(0, new[] { tokenD, nodeE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("D E ", newList.ToFullString());

            newList = list.Remove(tokenD);
            Assert.Equal(0, newList.Count);

            Assert.Equal(-1, list.IndexOf(tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, tokenD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { tokenD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Add(default(SyntaxNodeOrToken)));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(0, default(SyntaxNodeOrToken)));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxNodeOrToken>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxNodeOrToken>)null));
        }
    }
}
