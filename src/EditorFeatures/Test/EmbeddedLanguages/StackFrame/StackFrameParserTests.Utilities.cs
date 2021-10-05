// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EmbeddedLanguages.StackFrame
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;

    public partial class StackFrameParserTests
    {
        private static StackFrameToken CreateToken(StackFrameKind kind, string s, ImmutableArray<StackFrameTrivia> leadingTrivia = default, ImmutableArray<StackFrameTrivia> trailingTrivia = default)
            => new(
                kind,
                leadingTrivia.IsDefaultOrEmpty ? ImmutableArray<StackFrameTrivia>.Empty : leadingTrivia,
                CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence.Create(0, s),
                trailingTrivia.IsDefaultOrEmpty ? ImmutableArray<StackFrameTrivia>.Empty : trailingTrivia,
                ImmutableArray<EmbeddedDiagnostic>.Empty,
                value: null!);

        private static StackFrameTrivia CreateTrivia(StackFrameKind kind, string s)
            => new(kind, CodeAnalysis.EmbeddedLanguages.VirtualChars.VirtualCharSequence.Create(0, s), ImmutableArray<EmbeddedDiagnostic>.Empty);

        private static ImmutableArray<StackFrameTrivia> CreateTriviaArray(StackFrameTrivia trivia)
            => ImmutableArray.Create(trivia);

        private static readonly StackFrameToken DotToken = CreateToken(StackFrameKind.DotToken, ".");
        private static readonly StackFrameToken CommaToken = CreateToken(StackFrameKind.CommaToken, ",");
        private static readonly StackFrameToken OpenParenToken = CreateToken(StackFrameKind.OpenParenToken, "(");
        private static readonly StackFrameToken CloseParenToken = CreateToken(StackFrameKind.CloseParenToken, ")");
        private static readonly StackFrameToken OpenBracketToken = CreateToken(StackFrameKind.OpenBracketToken, "[");
        private static readonly StackFrameToken CloseBracketToken = CreateToken(StackFrameKind.CloseBracketToken, "]");
        private static readonly StackFrameToken SpaceToken = CreateToken(StackFrameKind.TextToken, " ");

        private static readonly StackFrameTrivia SpaceTrivia = CreateTrivia(StackFrameKind.WhitespaceTrivia, " ");

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

        private static StackFrameArgumentList ArgumentList(params StackFrameNodeOrToken[] nodesOrTokens)
        {
            var nodesWithParens = nodesOrTokens.Prepend(OpenParenToken).Append(CloseParenToken).ToImmutableArray();
            return new StackFrameArgumentList(nodesWithParens);
        }

        private static StackFrameMethodDeclarationNode MethodDeclaration(
            StackFrameMemberAccessExpressionNode memberAccessExpression,
            StackFrameArgumentList argumentList,
            StackFrameTypeArgumentList? typeArgumnets = null)
        {
            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArgumnets, argumentList);
        }

        private static StackFrameMemberAccessExpressionNode MemberAccessExpression(StackFrameExpressionNode expressionNode, StackFrameBaseIdentifierNode identifierNode)
            => new(expressionNode, DotToken, identifierNode);

        private static StackFrameIdentifierNode Identifier(string identifierName, ImmutableArray<StackFrameTrivia> leadingTrivia = default, ImmutableArray<StackFrameTrivia> trailingTrivia = default)
            => new(CreateToken(StackFrameKind.IdentifierToken, identifierName, leadingTrivia: leadingTrivia, trailingTrivia: trailingTrivia));

        private static StackFrameArrayExpressionNode ArrayExpression(StackFrameExpressionNode identifier, params StackFrameToken[] arrayTokens)
            => new(identifier, arrayTokens.ToImmutableArray());
    }
}
