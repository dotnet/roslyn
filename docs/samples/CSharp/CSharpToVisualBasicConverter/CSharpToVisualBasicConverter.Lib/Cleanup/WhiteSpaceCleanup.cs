// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpToVisualBasicConverter.Cleanup
{
    internal class WhiteSpaceCleanup : CSharpSyntaxRewriter
    {
        private readonly SyntaxTree syntaxTree;

        public WhiteSpaceCleanup(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);
            if (token.IsMissing)
            {
                return token;
            }

            bool changed;

            do
            {
                changed = false;
                if ((token.HasTrailingTrivia && token.TrailingTrivia.Count >= 3) ||
                    (token.HasLeadingTrivia && token.LeadingTrivia.Count >= 3))
                {
                    List<SyntaxTrivia> newLeadingTrivia = RemoveBlankLineTrivia(token.LeadingTrivia, ref changed);
                    List<SyntaxTrivia> newTrailingTrivia = RemoveBlankLineTrivia(token.TrailingTrivia, ref changed);

                    if (changed)
                    {
                        token = token.WithLeadingTrivia(SyntaxFactory.TriviaList(newLeadingTrivia));
                        token = token.WithTrailingTrivia(SyntaxFactory.TriviaList(newTrailingTrivia));
                    }
                }
            }
            while (changed);

            return token;
        }

        private static List<SyntaxTrivia> RemoveBlankLineTrivia(SyntaxTriviaList trivia, ref bool changed)
        {
            List<SyntaxTrivia> newTrivia = new List<SyntaxTrivia>();

            for (int i = 0; i < trivia.Count;)
            {
                SyntaxTrivia trivia1 = trivia.ElementAt(i);
                newTrivia.Add(trivia1);

                if (i < trivia.Count - 2)
                {
                    SyntaxTrivia trivia2 = trivia.ElementAt(i + 1);
                    SyntaxTrivia trivia3 = trivia.ElementAt(i + 2);

                    if (trivia1.IsKind(SyntaxKind.EndOfLineTrivia) &&
                        trivia2.IsKind(SyntaxKind.WhitespaceTrivia) &&
                        trivia3.IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        // Skip the whitespace with a newline.
                        newTrivia.Add(trivia3);
                        changed = true;
                        i += 3;
                        continue;
                    }
                }

                i++;
            }

            return newTrivia;
        }
    }
}
