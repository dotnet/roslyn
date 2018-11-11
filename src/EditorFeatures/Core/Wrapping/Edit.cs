// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
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
        public readonly SyntaxTriviaList LeftTrailingTrivia;
        public readonly SyntaxTriviaList RightLeadingTrivia;

        public Edit(
            SyntaxToken left, SyntaxTriviaList leftTrailingTrivia,
            SyntaxToken right, SyntaxTriviaList rightLeadingTrivia)
        {
            Left = left;
            Right = right;
            LeftTrailingTrivia = leftTrailingTrivia;
            RightLeadingTrivia = rightLeadingTrivia;
        }

        public string GetNewTrivia()
        {
            var result = PooledStringBuilder.GetInstance();
            AppendTrivia(result, LeftTrailingTrivia);
            AppendTrivia(result, RightLeadingTrivia);
            return result.ToStringAndFree();
        }

        private void AppendTrivia(PooledStringBuilder result, SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                result.Builder.Append(trivia.ToFullString());
            }
        }
    }
}
