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

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxTriviaListTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var node1 = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            var node2 = SyntaxFactory.Token(SyntaxKind.VirtualKeyword);

            EqualityTesting.AssertEqual(default(SyntaxTriviaList), default(SyntaxTriviaList));
            EqualityTesting.AssertEqual(new SyntaxTriviaList(node1, node1.Node, 0, 0), new SyntaxTriviaList(node1, node1.Node, 0, 0));
            EqualityTesting.AssertNotEqual(new SyntaxTriviaList(node1, node1.Node, 0, 1), new SyntaxTriviaList(node1, node1.Node, 0, 0));
            EqualityTesting.AssertNotEqual(new SyntaxTriviaList(node1, node2.Node, 0, 0), new SyntaxTriviaList(node1, node1.Node, 0, 0));
            EqualityTesting.AssertNotEqual(new SyntaxTriviaList(node2, node1.Node, 0, 0), new SyntaxTriviaList(node1, node1.Node, 0, 0));

            // position not considered:
            EqualityTesting.AssertEqual(new SyntaxTriviaList(node1, node1.Node, 1, 0), new SyntaxTriviaList(node1, node1.Node, 0, 0));
        }

        [Fact]
        public void Reverse_Equality()
        {
            var node1 = SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
            var node2 = SyntaxFactory.Token(SyntaxKind.VirtualKeyword);

            EqualityTesting.AssertEqual(default(SyntaxTriviaList.Reversed), default(SyntaxTriviaList.Reversed));
            EqualityTesting.AssertEqual(new SyntaxTriviaList(node1, node1.Node, 0, 0).Reverse(), new SyntaxTriviaList(node1, node1.Node, 0, 0).Reverse());
            EqualityTesting.AssertNotEqual(new SyntaxTriviaList(node1, node1.Node, 0, 1).Reverse(), new SyntaxTriviaList(node1, node1.Node, 0, 0).Reverse());
            EqualityTesting.AssertNotEqual(new SyntaxTriviaList(node1, node2.Node, 0, 0).Reverse(), new SyntaxTriviaList(node1, node1.Node, 0, 0).Reverse());
            EqualityTesting.AssertNotEqual(new SyntaxTriviaList(node2, node1.Node, 0, 0).Reverse(), new SyntaxTriviaList(node1, node1.Node, 0, 0).Reverse());

            // position not considered:
            EqualityTesting.AssertEqual(new SyntaxTriviaList(node1, node1.Node, 1, 0).Reverse(), new SyntaxTriviaList(node1, node1.Node, 0, 0).Reverse());
        }

        [Fact]
        public void TestAddInsertRemoveReplace()
        {
            var list = SyntaxFactory.ParseLeadingTrivia("/*A*//*B*//*C*/");

            Assert.Equal(3, list.Count);
            Assert.Equal("/*A*/", list[0].ToString());
            Assert.Equal("/*B*/", list[1].ToString());
            Assert.Equal("/*C*/", list[2].ToString());
            Assert.Equal("/*A*//*B*//*C*/", list.ToFullString());

            var elementA = list[0];
            var elementB = list[1];
            var elementC = list[2];

            Assert.Equal(0, list.IndexOf(elementA));
            Assert.Equal(1, list.IndexOf(elementB));
            Assert.Equal(2, list.IndexOf(elementC));

            var triviaD = SyntaxFactory.ParseLeadingTrivia("/*D*/")[0];
            var triviaE = SyntaxFactory.ParseLeadingTrivia("/*E*/")[0];

            var newList = list.Add(triviaD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*A*//*B*//*C*//*D*/", newList.ToFullString());

            newList = list.AddRange(new[] { triviaD, triviaE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("/*A*//*B*//*C*//*D*//*E*/", newList.ToFullString());

            newList = list.Insert(0, triviaD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*D*//*A*//*B*//*C*/", newList.ToFullString());

            newList = list.Insert(1, triviaD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*A*//*D*//*B*//*C*/", newList.ToFullString());

            newList = list.Insert(2, triviaD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*A*//*B*//*D*//*C*/", newList.ToFullString());

            newList = list.Insert(3, triviaD);
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*A*//*B*//*C*//*D*/", newList.ToFullString());

            newList = list.InsertRange(0, new[] { triviaD, triviaE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("/*D*//*E*//*A*//*B*//*C*/", newList.ToFullString());

            newList = list.InsertRange(1, new[] { triviaD, triviaE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("/*A*//*D*//*E*//*B*//*C*/", newList.ToFullString());

            newList = list.InsertRange(2, new[] { triviaD, triviaE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("/*A*//*B*//*D*//*E*//*C*/", newList.ToFullString());

            newList = list.InsertRange(3, new[] { triviaD, triviaE });
            Assert.Equal(5, newList.Count);
            Assert.Equal("/*A*//*B*//*C*//*D*//*E*/", newList.ToFullString());

            newList = list.RemoveAt(0);
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*B*//*C*/", newList.ToFullString());

            newList = list.RemoveAt(list.Count - 1);
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*A*//*B*/", newList.ToFullString());

            newList = list.Remove(elementA);
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*B*//*C*/", newList.ToFullString());

            newList = list.Remove(elementB);
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*A*//*C*/", newList.ToFullString());

            newList = list.Remove(elementC);
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*A*//*B*/", newList.ToFullString());

            newList = list.Replace(elementA, triviaD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("/*D*//*B*//*C*/", newList.ToFullString());

            newList = list.Replace(elementB, triviaD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("/*A*//*D*//*C*/", newList.ToFullString());

            newList = list.Replace(elementC, triviaD);
            Assert.Equal(3, newList.Count);
            Assert.Equal("/*A*//*B*//*D*/", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new[] { triviaD, triviaE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*D*//*E*//*B*//*C*/", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new[] { triviaD, triviaE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*A*//*D*//*E*//*C*/", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new[] { triviaD, triviaE });
            Assert.Equal(4, newList.Count);
            Assert.Equal("/*A*//*B*//*D*//*E*/", newList.ToFullString());

            newList = list.ReplaceRange(elementA, new SyntaxTrivia[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*B*//*C*/", newList.ToFullString());

            newList = list.ReplaceRange(elementB, new SyntaxTrivia[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*A*//*C*/", newList.ToFullString());

            newList = list.ReplaceRange(elementC, new SyntaxTrivia[] { });
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*A*//*B*/", newList.ToFullString());

            Assert.Equal(-1, list.IndexOf(triviaD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, triviaD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, triviaD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { triviaD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, new[] { triviaD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Add(default(SyntaxTrivia)));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(0, default(SyntaxTrivia)));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxTrivia>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxTrivia>)null));
            Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(elementA, (IEnumerable<SyntaxTrivia>)null));
        }

        [Fact]
        public void TestAddInsertRemoveReplaceOnEmptyList()
        {
            DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxFactory.ParseLeadingTrivia("/*A*/").RemoveAt(0));
            DoTestAddInsertRemoveReplaceOnEmptyList(default(SyntaxTriviaList));
        }

        private void DoTestAddInsertRemoveReplaceOnEmptyList(SyntaxTriviaList list)
        {
            Assert.Equal(0, list.Count);

            var triviaD = SyntaxFactory.ParseLeadingTrivia("/*D*/")[0];
            var triviaE = SyntaxFactory.ParseLeadingTrivia("/*E*/")[0];

            var newList = list.Add(triviaD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("/*D*/", newList.ToFullString());

            newList = list.AddRange(new[] { triviaD, triviaE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*D*//*E*/", newList.ToFullString());

            newList = list.Insert(0, triviaD);
            Assert.Equal(1, newList.Count);
            Assert.Equal("/*D*/", newList.ToFullString());

            newList = list.InsertRange(0, new[] { triviaD, triviaE });
            Assert.Equal(2, newList.Count);
            Assert.Equal("/*D*//*E*/", newList.ToFullString());

            newList = list.Remove(triviaD);
            Assert.Equal(0, newList.Count);

            Assert.Equal(-1, list.IndexOf(triviaD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(1, triviaD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, triviaD));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(1, new[] { triviaD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, new[] { triviaD }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Replace(triviaD, triviaE));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.ReplaceRange(triviaD, new[] { triviaE }));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Add(default(SyntaxTrivia)));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(0, default(SyntaxTrivia)));
            Assert.Throws<ArgumentNullException>(() => list.AddRange((IEnumerable<SyntaxTrivia>)null));
            Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, (IEnumerable<SyntaxTrivia>)null));
        }

        [Fact]
        public void Extensions()
        {
            var list = SyntaxFactory.ParseLeadingTrivia("/*A*//*B*//*C*/");

            Assert.Equal(0, list.IndexOf(SyntaxKind.MultiLineCommentTrivia));
            Assert.True(list.Any(SyntaxKind.MultiLineCommentTrivia));

            Assert.Equal(-1, list.IndexOf(SyntaxKind.SingleLineCommentTrivia));
            Assert.False(list.Any(SyntaxKind.SingleLineCommentTrivia));
        }
    }
}
