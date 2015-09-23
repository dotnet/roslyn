// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal partial class AbstractSuppressionCodeFixProvider
    {
        /// <summary>
        /// Helper methods for pragma based suppression code actions.
        /// </summary>
        private static class PragmaHelpers
        {
            internal async static Task<Document> GetChangeDocumentWithPragmaAdjustedAsync(
                Document document,
                TextSpan diagnosticSpan,
                SuppressionTargetInfo suppressionTargetInfo,
                Func<SyntaxToken, TextSpan, SyntaxToken> getNewStartToken,
                Func<SyntaxToken, TextSpan, SyntaxToken> getNewEndToken,
                CancellationToken cancellationToken)
            {
                var startToken = suppressionTargetInfo.StartToken;
                var endToken = suppressionTargetInfo.EndToken;
                var nodeWithTokens = suppressionTargetInfo.NodeWithTokens;
                var root = await nodeWithTokens.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                var startAndEndTokenAreTheSame = startToken == endToken;
                SyntaxToken newStartToken = getNewStartToken(startToken, diagnosticSpan);

                SyntaxToken newEndToken = endToken;
                if (startAndEndTokenAreTheSame)
                {
                    var annotation = new SyntaxAnnotation();
                    newEndToken = root.ReplaceToken(startToken, newStartToken.WithAdditionalAnnotations(annotation)).GetAnnotatedTokens(annotation).Single();
                    var spanChange = newStartToken.LeadingTrivia.FullSpan.Length - startToken.LeadingTrivia.FullSpan.Length;
                    diagnosticSpan = new TextSpan(diagnosticSpan.Start + spanChange, diagnosticSpan.Length);
                }

                newEndToken = getNewEndToken(newEndToken, diagnosticSpan);

                SyntaxNode newNode;
                if (startAndEndTokenAreTheSame)
                {
                    newNode = nodeWithTokens.ReplaceToken(startToken, newEndToken);
                }
                else
                {
                    newNode = nodeWithTokens.ReplaceTokens(new[] { startToken, endToken }, (o, n) => o == startToken ? newStartToken : newEndToken);
                }

                var newRoot = root.ReplaceNode(nodeWithTokens, newNode);
                return document.WithSyntaxRoot(newRoot);
            }

            private static int GetPositionForPragmaInsertion(ImmutableArray<SyntaxTrivia> triviaList, TextSpan currentDiagnosticSpan, AbstractSuppressionCodeFixProvider fixer, bool isStartToken, out SyntaxTrivia triviaAtIndex)
            {
                // Start token: Insert the #pragma disable directive just **before** the first end of line trivia prior to diagnostic location.
                // End token: Insert the #pragma disable directive just **after** the first end of line trivia after diagnostic location.

                Func<int, int> getNextIndex = cur => isStartToken ? cur - 1 : cur + 1;
                Func<SyntaxTrivia, bool> shouldConsiderTrivia = trivia =>
                    isStartToken ?
                    trivia.FullSpan.End <= currentDiagnosticSpan.Start :
                    trivia.FullSpan.Start >= currentDiagnosticSpan.End;

                var walkedPastDiagnosticSpan = false;
                var seenEndOfLineTrivia = false;
                var index = isStartToken ? triviaList.Length - 1 : 0;
                while (index >= 0 && index < triviaList.Length)
                {
                    var trivia = triviaList[index];

                    walkedPastDiagnosticSpan = walkedPastDiagnosticSpan || shouldConsiderTrivia(trivia);
                    seenEndOfLineTrivia = seenEndOfLineTrivia ||
                        (fixer.IsEndOfLine(trivia) || 
                         (trivia.HasStructure &&
                          trivia.GetStructure().DescendantTrivia().Any(t => fixer.IsEndOfLine(t))));

                    if (walkedPastDiagnosticSpan && seenEndOfLineTrivia)
                    {
                        break;
                    }

                    index = getNextIndex(index);
                }

                triviaAtIndex = index >= 0 && index < triviaList.Length ?
                    triviaList[index] :
                    default(SyntaxTrivia);

                return index;
            }

            internal static SyntaxToken GetNewStartTokenWithAddedPragma(SyntaxToken startToken, TextSpan currentDiagnosticSpan, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer, Func<SyntaxNode, SyntaxNode> formatNode, bool isRemoveSuppression = false)
            {
                var trivia = startToken.LeadingTrivia.ToImmutableArray();
                SyntaxTrivia insertAfterTrivia;
                var index = GetPositionForPragmaInsertion(trivia, currentDiagnosticSpan, fixer, isStartToken: true, triviaAtIndex: out insertAfterTrivia);
                index++;
                
                bool needsLeadingEOL;
                if (index > 0)
                {
                    needsLeadingEOL = !fixer.IsEndOfLine(insertAfterTrivia);
                }
                else if (startToken.FullSpan.Start == 0)
                {
                    needsLeadingEOL = false;
                }
                else
                {
                    needsLeadingEOL = true;
                }

                var pragmaTrivia = !isRemoveSuppression ?
                    fixer.CreatePragmaDisableDirectiveTrivia(diagnostic, formatNode, needsLeadingEOL, needsTrailingEndOfLine: true) :
                    fixer.CreatePragmaRestoreDirectiveTrivia(diagnostic, formatNode, needsLeadingEOL, needsTrailingEndOfLine: true);

                return startToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaTrivia));
            }

            internal static SyntaxToken GetNewEndTokenWithAddedPragma(SyntaxToken endToken, TextSpan currentDiagnosticSpan, Diagnostic diagnostic, AbstractSuppressionCodeFixProvider fixer, Func<SyntaxNode, SyntaxNode> formatNode, bool isRemoveSuppression = false)
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

                SyntaxTrivia insertBeforeTrivia;
                var index = GetPositionForPragmaInsertion(trivia, currentDiagnosticSpan, fixer, isStartToken: false, triviaAtIndex: out insertBeforeTrivia);

                bool needsTrailingEOL;
                if (index < trivia.Length)
                {
                    needsTrailingEOL = !fixer.IsEndOfLine(insertBeforeTrivia);
                }
                else if (isEOF)
                {
                    needsTrailingEOL = false;
                }
                else
                {
                    needsTrailingEOL = true;
                }

                var pragmaTrivia = !isRemoveSuppression ?
                    fixer.CreatePragmaRestoreDirectiveTrivia(diagnostic, formatNode, needsLeadingEndOfLine: true, needsTrailingEndOfLine: needsTrailingEOL) :
                    fixer.CreatePragmaDisableDirectiveTrivia(diagnostic, formatNode, needsLeadingEndOfLine: true, needsTrailingEndOfLine: needsTrailingEOL);

                if (isEOF)
                {
                    return endToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaTrivia));
                }
                else
                {
                    return endToken.WithTrailingTrivia(trivia.InsertRange(index, pragmaTrivia));
                };
            }
        }
    }
}
