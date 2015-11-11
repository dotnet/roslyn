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
                Func<SyntaxToken, TextSpan, Task<SyntaxToken>> getNewStartToken,
                Func<SyntaxToken, TextSpan, Task<SyntaxToken>> getNewEndToken,
                CancellationToken cancellationToken)
            {
                var startToken = suppressionTargetInfo.StartToken;
                var endToken = suppressionTargetInfo.EndToken;
                var nodeWithTokens = suppressionTargetInfo.NodeWithTokens;
                var root = await nodeWithTokens.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                var startAndEndTokenAreTheSame = startToken == endToken;
                SyntaxToken newStartToken = await getNewStartToken(startToken, diagnosticSpan).ConfigureAwait(false);

                SyntaxToken newEndToken = endToken;
                if (startAndEndTokenAreTheSame)
                {
                    var annotation = new SyntaxAnnotation();
                    newEndToken = root.ReplaceToken(startToken, newStartToken.WithAdditionalAnnotations(annotation)).GetAnnotatedTokens(annotation).Single();
                    var spanChange = newStartToken.LeadingTrivia.FullSpan.Length - startToken.LeadingTrivia.FullSpan.Length;
                    diagnosticSpan = new TextSpan(diagnosticSpan.Start + spanChange, diagnosticSpan.Length);
                }

                newEndToken = await getNewEndToken(newEndToken, diagnosticSpan).ConfigureAwait(false);

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

            internal static async Task<SyntaxToken> GetNewStartTokenWithAddedPragmaAsync(
                SyntaxToken startToken,
                TextSpan currentDiagnosticSpan,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                Func<SyntaxNode, Task<SyntaxNode>> formatNode,
                bool isRemoveSuppression = false)
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

                var pragmaTrivia = !isRemoveSuppression
                    ? await fixer.CreatePragmaDisableDirectiveTriviaAsync(diagnostic, formatNode, needsLeadingEOL, needsTrailingEndOfLine: true).ConfigureAwait(false)
                    : await fixer.CreatePragmaRestoreDirectiveTriviaAsync(diagnostic, formatNode, needsLeadingEOL, needsTrailingEndOfLine: true).ConfigureAwait(false);

                return startToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaTrivia));
            }

            internal static async Task<SyntaxToken> GetNewEndTokenWithAddedPragmaAsync(
                SyntaxToken endToken,
                TextSpan currentDiagnosticSpan,
                Diagnostic diagnostic,
                AbstractSuppressionCodeFixProvider fixer,
                Func<SyntaxNode, Task<SyntaxNode>> formatNode,
                bool isRemoveSuppression = false)
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

                var pragmaTrivia = !isRemoveSuppression
                    ? await fixer.CreatePragmaRestoreDirectiveTriviaAsync(diagnostic, formatNode, needsLeadingEndOfLine: true, needsTrailingEndOfLine: needsTrailingEOL).ConfigureAwait(false)
                    : await fixer.CreatePragmaDisableDirectiveTriviaAsync(diagnostic, formatNode, needsLeadingEndOfLine: true, needsTrailingEndOfLine: needsTrailingEOL).ConfigureAwait(false);

                if (isEOF)
                {
                    return endToken.WithLeadingTrivia(trivia.InsertRange(index, pragmaTrivia));
                }
                else
                {
                    return endToken.WithTrailingTrivia(trivia.InsertRange(index, pragmaTrivia));
                };
            }

            internal static void NormalizeTriviaOnTokens(AbstractSuppressionCodeFixProvider fixer, ref Document document, ref SuppressionTargetInfo suppressionTargetInfo)
            {
                // For pragma suppression fixes, we need to normalize the leading trivia on start token to account for
                // the trailing trivia on its previous token (and similarly normalize trailing trivia for end token).

                var startToken = suppressionTargetInfo.StartToken;
                var endToken = suppressionTargetInfo.EndToken;
                var nodeWithTokens = suppressionTargetInfo.NodeWithTokens;
                var startAndEndTokensAreSame = startToken == endToken;
                var isEndTokenEOF = fixer.IsEndOfFileToken(endToken);

                var previousOfStart = startToken.GetPreviousToken(includeZeroWidth: true);
                var nextOfEnd = !isEndTokenEOF ? endToken.GetNextToken(includeZeroWidth: true) : default(SyntaxToken);
                if (!previousOfStart.HasTrailingTrivia && !nextOfEnd.HasLeadingTrivia)
                {
                    return;
                }

                var root = nodeWithTokens.SyntaxTree.GetRoot();
                var spanEnd = !isEndTokenEOF ? nextOfEnd.FullSpan.End : endToken.FullSpan.End;
                var subtreeRoot = root.FindNode(new TextSpan(previousOfStart.FullSpan.Start, spanEnd - previousOfStart.FullSpan.Start));

                var currentStartToken = startToken;
                var currentEndToken = endToken;
                var newStartToken = startToken.WithLeadingTrivia(previousOfStart.TrailingTrivia.Concat(startToken.LeadingTrivia));

                SyntaxToken newEndToken = currentEndToken;
                if (startAndEndTokensAreSame)
                {
                    newEndToken = newStartToken;
                }

                newEndToken = newEndToken.WithTrailingTrivia(endToken.TrailingTrivia.Concat(nextOfEnd.LeadingTrivia));

                var newPreviousOfStart = previousOfStart.WithTrailingTrivia();
                var newNextOfEnd = nextOfEnd.WithLeadingTrivia();

                var newSubtreeRoot = subtreeRoot.ReplaceTokens(new[] { startToken, previousOfStart, endToken, nextOfEnd },
                    (o, n) =>
                    {
                        if (o == currentStartToken)
                        {
                            return startAndEndTokensAreSame ? newEndToken : newStartToken;
                        }
                        else if (o == previousOfStart)
                        {
                            return newPreviousOfStart;
                        }
                        else if (o == currentEndToken)
                        {
                            return newEndToken;
                        }
                        else if (o == nextOfEnd)
                        {
                            return newNextOfEnd;
                        }
                        else
                        {
                            return n;
                        }
                    });

                root = root.ReplaceNode(subtreeRoot, newSubtreeRoot);
                document = document.WithSyntaxRoot(root);
                suppressionTargetInfo.StartToken = root.FindToken(startToken.SpanStart);
                suppressionTargetInfo.EndToken = root.FindToken(endToken.SpanStart);
                suppressionTargetInfo.NodeWithTokens = fixer.GetNodeWithTokens(suppressionTargetInfo.StartToken, suppressionTargetInfo.EndToken, root);
            }
        }
    }
}
