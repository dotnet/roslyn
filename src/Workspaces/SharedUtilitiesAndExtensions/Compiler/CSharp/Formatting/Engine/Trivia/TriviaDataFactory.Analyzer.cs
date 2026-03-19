// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed partial class TriviaDataFactory
{
    private sealed class Analyzer
    {
        public static AnalysisResult Leading(SyntaxToken token)
        {
            var result = default(AnalysisResult);
            Analyze(token.LeadingTrivia, ref result);

            return result;
        }

        public static AnalysisResult Trailing(SyntaxToken token)
        {
            var result = default(AnalysisResult);
            Analyze(token.TrailingTrivia, ref result);

            return result;
        }

        public static AnalysisResult Between(SyntaxToken token1, SyntaxToken token2)
        {
            if (!token1.HasTrailingTrivia && !token2.HasLeadingTrivia)
            {
                return default;
            }

            var result = default(AnalysisResult);

            if (token1.IsMissing && token1.FullWidth() == 0)
            {
                // Consider the following case:
                //
                //          return // <- note the missing semicolon
                //      }
                //
                // in this case, the compiler will insert a missing semicolon token at the 
                // start of the line containing the close curly.  This is problematic as it
                // means that if we're looking at the token-pair for the semicolon and close-
                // curly, then we'll think there is no newline here.  Because we think there
                // is no newline, we won't attempt to indent in a manner that preserves tabs
                // (if the user has 'use tabs for indent' enabled).
                //
                // Here we detect if our previous token is an empty missing token.  If so,
                // we look back to the previous non-missing token to see if it ends with a
                // newline.  If so, we keep track of that so we'll appropriately indent later
                // on. 

                // Keep walking backward until we hit a token whose *full width* is greater than
                // 0.  See if this token has an end of line trivia at the end of it.  Note:
                // we need to "includeZeroWidth" tokens because we can have zero width tokens
                // that still have a full width that is non-zero.  i.e. a missing token that
                // still has trailing trivia on it.

                for (var currentToken = token1; !currentToken.IsKind(SyntaxKind.None);)
                {
                    var previousToken = currentToken.GetPreviousToken(includeSkipped: false, includeZeroWidth: true);
                    if (previousToken.FullWidth() == 0)
                    {
                        currentToken = previousToken;
                        continue;
                    }

                    // Finally hit the first previous token with non-zero full width.
                    if (previousToken.TrailingTrivia is [.., (kind: SyntaxKind.EndOfLineTrivia)])
                        result.LineBreaks = 1;

                    break;
                }
            }
            else
            {
                Analyze(token1.TrailingTrivia, ref result);
            }

            Analyze(token2.LeadingTrivia, ref result);

            return result;
        }

        private static void Analyze(SyntaxTriviaList list, ref AnalysisResult result)
        {
            if (list.Count == 0)
            {
                return;
            }

            foreach (var trivia in list)
            {
                if (trivia.Kind() == SyntaxKind.WhitespaceTrivia)
                {
                    AnalyzeWhitespacesInTrivia(trivia, ref result);
                }
                else if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    AnalyzeLineBreak(trivia, ref result);
                }
                else if (trivia.IsRegularOrDocComment())
                {
                    result.HasComments = true;
                }
                else if (trivia.Kind() == SyntaxKind.SkippedTokensTrivia)
                {
                    result.HasSkippedTokens = true;
                }
                else if (trivia.Kind() is SyntaxKind.DisabledTextTrivia or
                         SyntaxKind.PreprocessingMessageTrivia)
                {
                    result.HasSkippedOrDisabledText = true;
                }
                else if (trivia.Kind() == SyntaxKind.ConflictMarkerTrivia)
                {
                    result.HasConflictMarker = true;
                }
                else
                {
                    Contract.ThrowIfFalse(SyntaxFacts.IsPreprocessorDirective(trivia.Kind()));

                    result.HasPreprocessor = true;
                }
            }
        }

        private static void AnalyzeLineBreak(SyntaxTrivia trivia, ref AnalysisResult result)
        {
            // if there was any space before line break, then we have trailing spaces
            if (result.Space > 0 || result.Tab > 0)
            {
                result.HasTrailingSpace = true;
            }

            // reset space and tab information
            result.LineBreaks++;

            result.HasTabAfterSpace = false;
            result.Space = 0;
            result.Tab = 0;
            result.TreatAsElastic |= trivia.IsElastic();
        }

        private static void AnalyzeWhitespacesInTrivia(SyntaxTrivia trivia, ref AnalysisResult result)
        {
            // trivia already has text. getting text should be noop
            Debug.Assert(trivia.Kind() == SyntaxKind.WhitespaceTrivia);
            Debug.Assert(trivia.Width() == trivia.FullWidth());

            var space = 0;
            var tab = 0;
            var unknownWhitespace = 0;

            var text = trivia.ToString();
            for (var i = 0; i < trivia.Width(); i++)
            {
                if (text[i] == ' ')
                {
                    space++;
                }
                else if (text[i] == '\t')
                {
                    if (result.Space > 0)
                    {
                        result.HasTabAfterSpace = true;
                    }

                    tab++;
                }
                else
                {
                    unknownWhitespace++;
                }
            }

            // set result
            result.Space += space;
            result.Tab += tab;
            result.HasUnknownWhitespace |= unknownWhitespace > 0;
            result.TreatAsElastic |= trivia.IsElastic();
        }

        internal struct AnalysisResult
        {
            internal int LineBreaks { get; set; }
            internal int Space { get; set; }
            internal int Tab { get; set; }

            internal bool HasTabAfterSpace { get; set; }
            internal bool HasUnknownWhitespace { get; set; }
            internal bool HasTrailingSpace { get; set; }
            internal bool HasSkippedTokens { get; set; }
            internal bool HasSkippedOrDisabledText { get; set; }

            internal bool HasConflictMarker { get; set; }
            internal bool HasComments { get; set; }
            internal bool HasPreprocessor { get; set; }

            internal bool TreatAsElastic { get; set; }
        }
    }
}
