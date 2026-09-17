// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpToVisualBasicConverter.Cleanup
{
    internal class CurlyCleanup : CSharpSyntaxRewriter
    {
        private readonly SyntaxTree syntaxTree;

        public CurlyCleanup(SyntaxTree syntaxTree)
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

            if (!token.IsKind(SyntaxKind.CloseBraceToken))
            {
                return token;
            }

            SyntaxToken nextToken = token.GetNextToken(includeSkipped: true);

            int tokenLine = syntaxTree.GetText().Lines.IndexOf(token.Span.Start);
            int nextTokenLine = syntaxTree.GetText().Lines.IndexOf(nextToken.Span.Start);
            bool nextTokenIsCloseBrace = nextToken.IsKind(SyntaxKind.CloseBraceToken);

            int expectedDiff = nextTokenIsCloseBrace ? 1 : 2;
            if (nextTokenLine == tokenLine + expectedDiff)
            {
                return token;
            }

            System.Collections.Generic.IEnumerable<SyntaxTrivia> nonNewLineTrivia = token.TrailingTrivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
            System.Collections.Generic.IEnumerable<SyntaxTrivia> newTrivia = nonNewLineTrivia.Concat(Enumerable.Repeat(SyntaxFactory.EndOfLine("\r\n"), expectedDiff));

            return token.WithTrailingTrivia(SyntaxFactory.TriviaList(newTrivia));
        }
    }
}
