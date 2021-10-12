// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal static class StackTraceExtensions
    {
        public static string CreateString(this StackFrameNode node, StackFrameTree tree)
            => node.CreateString(tree, skipTrivia: false);

        public static string CreateString(this StackFrameNode node, StackFrameTree tree, bool skipTrivia)
        {
            var span = skipTrivia
                ? node.GetSpanWithoutTrivia(skipLeadingTrivia: true, skipTrailingTrivia: true)
                : node.GetSpan();

            return tree.Text.GetSubSequence(span).CreateString();
        }

        public static string CreateString(this StackFrameToken token)
            => token.VirtualChars.CreateString();

        public static string CreateString(this ImmutableArray<StackFrameTrivia> triviaList)
        {
            if (triviaList.IsDefault)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var trivia in triviaList)
            {
                sb.Append(trivia.VirtualChars.CreateString());
            }

            return sb.ToString();
        }

        public static string CreateString<TNode>(this EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, TNode> list, StackFrameTree tree)
            where TNode : StackFrameNode
        {
            var sb = new StringBuilder();
            foreach (var nodeOrToken in list.NodesAndTokens)
            {
                sb.Append(nodeOrToken.IsNode
                    ? nodeOrToken.Node.CreateString(tree)
                    : nodeOrToken.Token.CreateString());
            }

            return sb.ToString();
        }

        public static TextSpan GetSpanWithoutTrivia(this StackFrameNode node, bool skipLeadingTrivia, bool skipTrailingTrivia)
        {
            var nodeSpan = node.GetSpan();

            if (skipLeadingTrivia)
            {
                var leadingTrivia = node.GetLeadingTrivia();
                var span = GetSpan(leadingTrivia);
                nodeSpan = TextSpan.FromBounds(span.End, nodeSpan.End);
            }

            if (skipTrailingTrivia)
            {
                var trailingTrivia = node.GetTrailingTrivia();
                var span = GetSpan(trailingTrivia);
                nodeSpan = TextSpan.FromBounds(nodeSpan.Start, span.Start);
            }

            return nodeSpan;
        }

        public static ImmutableArray<StackFrameTrivia> GetLeadingTrivia(this StackFrameNode node)
        {
            if (node.ChildCount == 0)
            {
                return ImmutableArray<StackFrameTrivia>.Empty;
            }

            var start = node[0];
            return start.IsNode
                ? start.Node.GetLeadingTrivia()
                : start.Token.LeadingTrivia;
        }

        public static ImmutableArray<StackFrameTrivia> GetTrailingTrivia(this StackFrameNode node)
        {
            if (node.ChildCount == 0)
            {
                return ImmutableArray<StackFrameTrivia>.Empty;
            }

            var end = node[^1];
            return end.IsNode
                ? end.Node.GetTrailingTrivia()
                : end.Token.TrailingTrivia;
        }

        private static TextSpan GetSpan(ImmutableArray<StackFrameTrivia> trivia)
        {
            if (trivia.IsDefaultOrEmpty)
            {
                return new TextSpan();
            }

            var startSpan = trivia[0].GetSpan();
            var endSpan = trivia[^1].GetSpan();

            return TextSpan.FromBounds(startSpan.Start, endSpan.End);
        }
    }
}
