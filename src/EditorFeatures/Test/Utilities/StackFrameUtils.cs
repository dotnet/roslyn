// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

using StackFrameNodeOrToken = CodeAnalysis.EmbeddedLanguages.Common.EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
using StackFrameToken = CodeAnalysis.EmbeddedLanguages.Common.EmbeddedSyntaxToken<StackFrameKind>;
using StackFrameTrivia = CodeAnalysis.EmbeddedLanguages.Common.EmbeddedSyntaxTrivia<StackFrameKind>;

internal static class StackFrameUtils
{
    public static void AssertEqual(StackFrameNodeOrToken expected, StackFrameNodeOrToken actual)
    {
        Assert.Equal(expected.IsNode, actual.IsNode);
        if (expected.IsNode)
        {
            AssertEqual(expected.Node, actual.Node);
        }
        else
        {
            AssertEqual(expected.Token, actual.Token);
        }
    }

    public static void AssertEqual(StackFrameNode? expected, StackFrameNode? actual)
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

    public static void Print(StackFrameNode node, StringBuilder sb)
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

    public static void Print(ImmutableArray<StackFrameTrivia> triviaArray, StringBuilder sb)
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

    public static void AssertEqual(StackFrameToken expected, StackFrameToken actual)
    {
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.IsMissing, actual.IsMissing);
        Assert.Equal(expected.VirtualChars.CreateString(), actual.VirtualChars.CreateString());

        AssertEqual(expected.LeadingTrivia, actual.LeadingTrivia, expected);
        AssertEqual(expected.TrailingTrivia, actual.TrailingTrivia, expected);
    }

    public static void AssertEqual(ImmutableArray<StackFrameTrivia> expected, ImmutableArray<StackFrameTrivia> actual, StackFrameToken token)
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
            sb.AppendLine($"Trivia is different on {token.Kind}");
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

    public static void AssertEqual(StackFrameTrivia expected, StackFrameTrivia actual)
    {
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.VirtualChars.CreateString(), actual.VirtualChars.CreateString());
    }

    public static IEnumerable<CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence> Enumerate(StackFrameToken token)
    {
        foreach (var trivia in token.LeadingTrivia)
        {
            yield return trivia.VirtualChars;
        }

        yield return token.VirtualChars;

        foreach (var trivia in token.TrailingTrivia)
        {
            yield return trivia.VirtualChars;
        }
    }

    public static IEnumerable<CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence> Enumerate(StackFrameNode node)
    {
        foreach (var nodeOrToken in node)
        {
            if (nodeOrToken.IsNode)
            {
                foreach (var charSequence in Enumerate(nodeOrToken.Node))
                {
                    yield return charSequence;
                }
            }
            else if (nodeOrToken.Token.Kind != StackFrameKind.None)
            {
                foreach (var charSequence in Enumerate(nodeOrToken.Token))
                {
                    yield return charSequence;
                }
            }
            else
            {
                // If we encounter a None token make sure it has default values
                Assert.True(nodeOrToken.Token.IsMissing);
                Assert.True(nodeOrToken.Token.LeadingTrivia.IsDefault);
                Assert.True(nodeOrToken.Token.TrailingTrivia.IsDefault);
                Assert.Null(nodeOrToken.Token.Value);
                Assert.True(nodeOrToken.Token.VirtualChars.IsDefault);
            }
        }
    }
}
