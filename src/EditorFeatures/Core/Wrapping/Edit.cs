// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
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
            foreach (var trivia in LeftTrailingTrivia)
            {
                result.Builder.Append(trivia.ToFullString());
            }

            foreach (var trivia in RightLeadingTrivia)
            {
                result.Builder.Append(trivia.ToFullString());
            }

            return result.ToStringAndFree();
        }
    }
}
