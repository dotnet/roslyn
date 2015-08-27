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
            private readonly AbstractSuppressionCodeFixProvider _fixer;
            private readonly string _title;
            private readonly SyntaxToken _startToken;
            private readonly SyntaxToken _endToken;
            private readonly SyntaxNode _nodeWithTokens;
            private readonly Document _document;
            private readonly Diagnostic _diagnostic;

            public PragmaWarningCodeAction(
                AbstractSuppressionCodeFixProvider fixer,
                SyntaxToken startToken,
                SyntaxToken endToken,
                SyntaxNode nodeWithTokens,
                Document document,
                Diagnostic diagnostic)
            {
                _fixer = fixer;
                _startToken = startToken;
                _endToken = endToken;
                _nodeWithTokens = nodeWithTokens;
                _document = document;
                _diagnostic = diagnostic;

                _title = fixer.TitleForPragmaWarningSuppressionFix;
            }

            protected async override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var startAndEndTokenAreTheSame = _startToken == _endToken;
                SyntaxToken newStartToken = GetNewStartToken(_startToken, _diagnostic, _fixer);

                SyntaxToken newEndToken = _endToken;
                if (startAndEndTokenAreTheSame)
                {
                    newEndToken = newStartToken;
                }

                newEndToken = GetNewEndToken(newEndToken, _diagnostic, _fixer);

                SyntaxNode newNode;
                if (startAndEndTokenAreTheSame)
                {
                    newNode = _nodeWithTokens.ReplaceToken(_startToken, newEndToken);
                }
                else
                {
                    newNode = _nodeWithTokens.ReplaceTokens(new[] { _startToken, _endToken }, (o, n) => o == _startToken ? newStartToken : newEndToken);
                }

                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = root.ReplaceNode(_nodeWithTokens, newNode);
                return _document.WithSyntaxRoot(newRoot);
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

            public override string Title => _title;
            public override string EquivalenceKey => _title + _diagnostic.Id;

            public SyntaxToken StartToken_TestOnly => _startToken;
            public SyntaxToken EndToken_TestOnly => _endToken;
        }
    }
}
