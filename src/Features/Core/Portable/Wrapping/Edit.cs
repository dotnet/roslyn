// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Wrapping
{
    /// <summary>
    /// Represents an edit between two tokens.  Specifically, provides the new trailing trivia for
    /// the <see cref="Left"/> token and the new leading trivia for the <see
    /// cref="Right"/> token.
    /// </summary>
    internal readonly struct Edit
    {
        public readonly SyntaxToken Left;
        public readonly SyntaxToken Right;
        public readonly SyntaxTriviaList NewLeftTrailingTrivia;
        public readonly SyntaxTriviaList NewRightLeadingTrivia;

        private Edit(
            SyntaxToken left, SyntaxTriviaList newLeftTrailingTrivia,
            SyntaxToken right, SyntaxTriviaList newRightLeadingTrivia)
        {
            if (left.Span.End > right.Span.Start)
                throw new InvalidEditException(left, right);

            Left = left;
            Right = right;
            NewLeftTrailingTrivia = newLeftTrailingTrivia;
            NewRightLeadingTrivia = newRightLeadingTrivia;
        }

        public string GetNewTrivia()
        {
            var result = PooledStringBuilder.GetInstance();
            AppendTrivia(result, NewLeftTrailingTrivia);
            AppendTrivia(result, NewRightLeadingTrivia);
            return result.ToStringAndFree();
        }

        private static void AppendTrivia(PooledStringBuilder result, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                result.Builder.Append(trivia.ToFullString());
            }
        }

        /// <summary>
        /// Create the Edit representing the deletion of all trivia between left and right.
        /// </summary>
        public static Edit DeleteBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
            => UpdateBetween(left, default, default(SyntaxTriviaList), right);

        public static Edit UpdateBetween(
            SyntaxNodeOrToken left, SyntaxTriviaList leftTrailingTrivia,
            SyntaxTrivia rightLeadingTrivia, SyntaxNodeOrToken right)
        {
            return UpdateBetween(left, leftTrailingTrivia, new SyntaxTriviaList(rightLeadingTrivia), right);
        }

        public static Edit UpdateBetween(
            SyntaxNodeOrToken left, SyntaxTriviaList leftTrailingTrivia,
            SyntaxTriviaList rightLeadingTrivia, SyntaxNodeOrToken right)
        {
            var leftLastToken = left.IsToken ? left.AsToken() : left.AsNode()!.GetLastToken();
            var rightFirstToken = right.IsToken ? right.AsToken() : right.AsNode()!.GetFirstToken();
            return new Edit(leftLastToken, leftTrailingTrivia, rightFirstToken, rightLeadingTrivia);
        }

        private sealed class InvalidEditException(SyntaxToken left, SyntaxToken right) : Exception($"Left token had an end '{left.Span.End}' past the start of right token '{right.Span.Start}'")
        {
            // Used for analyzing dumps
#pragma warning disable IDE0052 // Remove unread private members
            private readonly SyntaxTree? _tree = left.SyntaxTree;
            private readonly SyntaxToken _left = left;
            private readonly SyntaxToken _right = right;
        }
    }
}
