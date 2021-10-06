// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Roslyn.Test.Utilities;
using Xunit;
using System;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;

    public partial class StackFrameParserTests
    {
        private static void Verify(string input, StackFrameMethodDeclarationNode? methodDeclaration = null, bool expectFailure = false)
        {
            var tree = StackFrameParser.TryParse(input);
            if (expectFailure)
            {
                Assert.Null(tree);
                return;
            }

            AssertEx.NotNull(tree);
            VerifyCharacterSpans(input, tree);

            if (methodDeclaration is null)
            {
                Assert.Null(tree.Root.MethodDeclaration);
            }
            else
            {
                AssertEqual(methodDeclaration, tree.Root.MethodDeclaration);
            }

            Assert.True(tree.Root.AtTrivia.HasValue);
            Assert.False(tree.Root.InTrivia.HasValue);
            Assert.Null(tree.Root.FileInformationExpression);
            Assert.False(tree.Root.TrailingTrivia.HasValue);
        }
        private static void AssertEqual(StackFrameNodeOrToken expected, StackFrameNodeOrToken actual)
        {
            AssertEqual(expected.Node, actual.Node);
            AssertEqual(expected.Token, actual.Token);
        }

        private static void AssertEqual(StackFrameNode? expected, StackFrameNode? actual)
        {
            if (expected is null)
            {
                Assert.Null(actual);
                return;
            }

            AssertEx.NotNull(actual);

            Assert.Equal(expected.Kind, actual.Kind);
            Assert.True(expected.ChildCount == actual.ChildCount, PrintChildDifference(expected, actual));

            for (var i = 0; i < expected.ChildCount; i++)
            {
                AssertEqual(expected.ChildAt(i), actual.ChildAt(i));
            }

            static string PrintChildDifference(StackFrameNode expected, StackFrameNode actual)
            {
                var sb = new StringBuilder();
                sb.Append("Expected: ");
                Print(expected, sb);
                sb.AppendLine();

                sb.Append("Actual: ");
                Print(actual, sb);

                return sb.ToString();
            }
        }

        private static void Print(StackFrameNode node, StringBuilder sb)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    Print(child.Node, sb);
                }
                else
                {
                    if (!child.Token.LeadingTrivia.IsDefaultOrEmpty)
                    {
                        Print(child.Token.LeadingTrivia, sb);
                    }

                    sb.Append(child.Token.VirtualChars.CreateString());

                    if (!child.Token.TrailingTrivia.IsDefaultOrEmpty)
                    {
                        Print(child.Token.TrailingTrivia, sb);
                    }
                }
            }
        }

        private static void Print(ImmutableArray<StackFrameTrivia> triviaArray, StringBuilder sb)
        {
            if (triviaArray.IsDefault)
            {
                sb.Append("<default>");
                return;
            }

            if (triviaArray.IsEmpty)
            {
                sb.Append("<empty>");
                return;
            }

            foreach (var trivia in triviaArray)
            {
                sb.Append(trivia.VirtualChars.CreateString());
            }
        }

        private static void AssertEqual(StackFrameToken expected, StackFrameToken actual)
        {
            AssertEqual(expected.LeadingTrivia, actual.LeadingTrivia);
            AssertEqual(expected.TrailingTrivia, actual.TrailingTrivia);

            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.IsMissing, actual.IsMissing);
            Assert.Equal(expected.VirtualChars.CreateString(), actual.VirtualChars.CreateString());
        }

        private static void VerifyCharacterSpans(string originalText, StackFrameTree tree)
        {
            var textSeq = VirtualCharSequence.Create(0, originalText);
            var index = 0;
            List<VirtualChar> enumeratedParsedCharacters = new();

            foreach (var charSeq in EnumerateTree(tree))
            {
                foreach (var ch in charSeq)
                {
                    enumeratedParsedCharacters.Add(ch);

                    if (textSeq[index++] != ch)
                    {
                        Assert.True(false, PrintDifference());
                    }
                }
            }

            // Make sure we enumerated the total input
            Assert.Equal(index, textSeq.Length);

            string PrintDifference()
            {
                var sb = new StringBuilder();

                var start = Math.Max(0, index - 10);
                var end = Math.Min(index, originalText.Length - 1);

                sb.Append("Expected: \t");
                PrintString(originalText, start, end, sb);
                sb.AppendLine();

                sb.Append("Actual: \t");
                var enumeratedString = new string(enumeratedParsedCharacters.Select(ch => (char)ch.Value).ToArray());
                PrintString(enumeratedString, start, end, sb);
                sb.AppendLine();

                return sb.ToString();

                static void PrintString(string s, int start, int end, StringBuilder sb)
                {
                    if (start > 0)
                    {
                        sb.Append("...");
                    }
                    sb.Append(s[start..end]);
                    if (end < s.Length - 1)
                    {
                        sb.Append("...");
                    }
                }
            }
        }

        private static IEnumerable<VirtualCharSequence> EnumerateTree(StackFrameTree tree)
        {
            var root = tree.Root;

            if (root.AtTrivia.HasValue)
            {
                yield return root.AtTrivia.Value.VirtualChars;
            }

            var methodDeclaration = root.MethodDeclaration;
            foreach (var seq in Enumerate(methodDeclaration))
            {
                yield return seq;
            }

            if (root.TrailingTrivia.HasValue)
            {
                yield return root.TrailingTrivia.Value.VirtualChars;
            }
        }

        private static IEnumerable<VirtualCharSequence> Enumerate(StackFrameNode node)
        {
            foreach (var nodeOrToken in node)
            {
                if (nodeOrToken.IsNode)
                {
                    foreach (var charSequence in Enumerate(nodeOrToken.Node).ToArray())
                    {
                        yield return charSequence;
                    }
                }
                else if (!nodeOrToken.Token.VirtualChars.IsDefaultOrEmpty)
                {
                    var token = nodeOrToken.Token;
                    if (!token.LeadingTrivia.IsDefault)
                    {
                        foreach (var trivia in token.LeadingTrivia)
                        {
                            yield return trivia.VirtualChars;
                        }
                    }

                    yield return nodeOrToken.Token.VirtualChars;

                    if (!token.TrailingTrivia.IsDefault)
                    {
                        foreach (var trivia in token.TrailingTrivia)
                        {
                            yield return trivia.VirtualChars;
                        }
                    }
                }
            }
        }

        private static void AssertEqual(ImmutableArray<StackFrameTrivia> expected, ImmutableArray<StackFrameTrivia> actual)
        {
            var diffMessage = PrintDiff();

            if (expected.IsDefault)
            {
                Assert.True(actual.IsDefault, diffMessage);
                return;
            }

            Assert.False(actual.IsDefault, diffMessage);
            Assert.True(expected.Length == actual.Length, diffMessage);

            for (var i = 0; i < expected.Length; i++)
            {
                AssertEqual(expected[i], actual[i]);
            }

            string PrintDiff()
            {
                var sb = new StringBuilder();
                sb.Append("Expected: ");

                if (!expected.IsDefaultOrEmpty)
                {
                    sb.Append('[');
                }

                Print(expected, sb);

                if (expected.IsDefaultOrEmpty)
                {
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("]");
                }

                sb.Append("Actual: ");

                if (!actual.IsDefaultOrEmpty)
                {
                    sb.Append('[');
                }

                Print(actual, sb);

                if (!actual.IsDefaultOrEmpty)
                {
                    sb.Append(']');
                }

                return sb.ToString();
            }
        }

        private static void AssertEqual(StackFrameTrivia expected, StackFrameTrivia actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.VirtualChars.CreateString(), actual.VirtualChars.CreateString());
        }
    }
}
