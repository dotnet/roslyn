// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class PragmaWarningCodeAction : CodeAction
        {
            private readonly AbstractSuppressionCodeFixProvider fixer;
            private readonly string title;
            private readonly SyntaxToken startToken;
            private readonly SyntaxToken endToken;
            private readonly SyntaxNode nodeWithTokens;
            private readonly Document document;
            private readonly Diagnostic diagnostic;

            public PragmaWarningCodeAction(
                AbstractSuppressionCodeFixProvider fixer,
                SyntaxToken startToken,
                SyntaxToken endToken,
                SyntaxNode nodeWithTokens,
                Document document,
                Diagnostic diagnostic)
            {
                this.fixer = fixer;
                this.startToken = startToken;
                this.endToken = endToken;
                this.nodeWithTokens = nodeWithTokens;
                this.document = document;
                this.diagnostic = diagnostic;

                this.title = fixer.TitleForPragmaWarningSuppressionFix;
            }

            protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var startAndEndTokenAreTheSame = startToken == endToken;
                SyntaxToken newStartToken = GetNewStartToken(startToken, diagnostic, fixer);

                SyntaxToken newEndToken = endToken;
                if (startAndEndTokenAreTheSame)
                {
                    newEndToken = newStartToken;
                }

                newEndToken = GetNewEndToken(newEndToken, diagnostic, fixer);

                SyntaxNode newNode;
                if (startAndEndTokenAreTheSame)
                {
                    newNode = nodeWithTokens.ReplaceToken(startToken, newEndToken);
                }
                else
                {
                    newNode = nodeWithTokens.ReplaceTokens(new[] { startToken, endToken }, (o, n) => o == startToken ? newStartToken : newEndToken);
                }

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(nodeWithTokens, newNode);
                return document.WithSyntaxRoot(newRoot);
            }

            private static SyntaxToken GetNewStartToken(SyntaxToken startToken, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer)
            {
                var trivia = startToken.LeadingTrivia.ToImmutableArray();

                // Insert the #pragma disable directive after all leading new line trivia but before first trivia of any other kind.
                int index;
                SyntaxTrivia firstNonEOLTrivia = trivia.FirstOrDefault(t => !fixer.IsEndOfLine(t));
                if (firstNonEOLTrivia == default(SyntaxTrivia))
                {
                    index = trivia.Length;
                }
                else
                {
                    index = trivia.IndexOf(firstNonEOLTrivia);
                }

                bool needsLeadingEOL;
                if (index > 0)
                {
                    needsLeadingEOL = !fixer.IsEndOfLine(trivia[index - 1]);
                }
                else if (startToken.FullSpan.Start == 0)
                {
                    needsLeadingEOL = false;
                }
                else
                {
                    needsLeadingEOL = true;
                }

                var pragmaWarningTrivia = fixer.CreatePragmaDisableDirectiveTrivia(diagnostic, needsLeadingEOL);

                return startToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaWarningTrivia));
            }

            private static SyntaxToken GetNewEndToken(SyntaxToken endToken, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer)
            {
                ImmutableArray<SyntaxTrivia> trivia;
                var isEOF = fixer.IsEndOfFileToken(endToken);
                if (isEOF)
                {
                    trivia = endToken.LeadingTrivia.ToImmutableArray();
                }
                else
                {
                    trivia = endToken.TrailingTrivia.ToImmutableArray();
                }

                SyntaxTrivia lastNonEOLTrivia = trivia.LastOrDefault(t => !fixer.IsEndOfLine(t));

                // Insert the #pragma restore directive after the last trailing trivia that is not a new line trivia.
                int index;
                if (lastNonEOLTrivia == default(SyntaxTrivia))
                {
                    index = 0;
                }
                else
                {
                    index = trivia.IndexOf(lastNonEOLTrivia) + 1;
                }

                bool needsTrailingEOL;
                if (index < trivia.Length)
                {
                    needsTrailingEOL = !fixer.IsEndOfLine(trivia[index]);
                }
                else if (isEOF)
                {
                    needsTrailingEOL = false;
                }
                else
                {
                    needsTrailingEOL = true;
                }

                var pragmaRestoreTrivia = fixer.CreatePragmaRestoreDirectiveTrivia(diagnostic, needsTrailingEOL);

                if (isEOF)
                {
                    return endToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaRestoreTrivia));
                }
                else
                {
                    return endToken.WithTrailingTrivia(trivia.InsertRange(index, pragmaRestoreTrivia));
                }
            }

            public override string Title
            {
                get
                {
                    return this.title;
                }
            }

            public SyntaxToken StartToken_TestOnly
            {
                get { return this.startToken; }
            }

            public SyntaxToken EndToken_TestOnly
            {
                get { return this.endToken; }
            }
        }
    }
}
