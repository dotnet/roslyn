// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Wrapping
{
    /// <summary>
    /// Represents an edit between two tokens.  Specifically, provides the new trailing trivia for
    /// the <see cref="Edit.Left"/> token and the new leading trivia for the <see
    /// cref="Edit.Right"/> token.
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

        private void AppendTrivia(PooledStringBuilder result, SyntaxTriviaList triviaList)
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
            var leftLastToken = left.IsToken ? left.AsToken() : left.AsNode().GetLastToken();
            var rightFirstToken = right.IsToken ? right.AsToken() : right.AsNode().GetFirstToken();
            return new Edit(leftLastToken, leftTrailingTrivia, rightFirstToken, rightLeadingTrivia);
        }
    }
}
