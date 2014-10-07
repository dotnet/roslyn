// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class TriviaDataFactory
    {
        private struct CodeShapeAnalyzer
        {
            private readonly FormattingContext context;
            private readonly OptionSet optionSet;
            private readonly TriviaList triviaList;

            private int indentation;
            private bool hasTrailingSpace;
            private int lastLineBreakIndex;
            private bool touchedNoisyCharacterOnCurrentLine;

            public static bool ShouldFormatMultiLine(FormattingContext context, bool firstTriviaInTree, TriviaList triviaList)
            {
                var analyzer = new CodeShapeAnalyzer(context, firstTriviaInTree, triviaList);
                return analyzer.ShouldFormat();
            }

            public static bool ShouldFormatSingleLine(TriviaList list)
            {
                foreach (var commonTrivia in list)
                {
                    var trivia = (SyntaxTrivia)commonTrivia;

                    Contract.ThrowIfTrue(trivia.CSharpKind() == SyntaxKind.EndOfLineTrivia);
                    Contract.ThrowIfTrue(trivia.CSharpKind() == SyntaxKind.SkippedTokensTrivia);
                    Contract.ThrowIfTrue(trivia.CSharpKind() == SyntaxKind.PreprocessingMessageTrivia);

                    // if it contains elastic trivia, always format
                    if (trivia.IsElastic())
                    {
                        return true;
                    }

                    if (trivia.CSharpKind() == SyntaxKind.WhitespaceTrivia)
                    {
                        Debug.Assert(trivia.ToString() == trivia.ToFullString());
                        var text = trivia.ToString();
                        if (text.IndexOf('\t') >= 0)
                        {
                            return true;
                        }
                    }

                    // we don't touch space between two tokens on a single line that contains
                    // multiline comments between them
                    if (trivia.IsRegularOrDocComment())
                    {
                        return false;
                    }

                    if (trivia.CSharpKind() == SyntaxKind.RegionDirectiveTrivia ||
                        trivia.CSharpKind() == SyntaxKind.EndRegionDirectiveTrivia ||
                        SyntaxFacts.IsPreprocessorDirective(trivia.CSharpKind()))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool ContainsSkippedTokensOrText(TriviaList list)
            {
                foreach (var commonTrivia in list)
                {
                    var trivia = (SyntaxTrivia)commonTrivia;

                    if (trivia.CSharpKind() == SyntaxKind.SkippedTokensTrivia ||
                        trivia.CSharpKind() == SyntaxKind.PreprocessingMessageTrivia)
                    {
                        return true;
                    }
                }

                return false;
            }

            private CodeShapeAnalyzer(FormattingContext context, bool firstTriviaInTree, TriviaList triviaList)
            {
                this.context = context;
                this.optionSet = context.OptionSet;
                this.triviaList = triviaList;

                this.indentation = 0;
                this.hasTrailingSpace = false;
                this.lastLineBreakIndex = firstTriviaInTree ? 0 : -1;
                this.touchedNoisyCharacterOnCurrentLine = false;
            }

            private bool UseIndentation
            {
                get { return this.lastLineBreakIndex >= 0; }
            }

            private bool OnElastic(SyntaxTrivia trivia)
            {
                // if it contains elastic trivia, always format
                return trivia.IsElastic();
            }

            private bool OnWhitespace(SyntaxTrivia trivia)
            {
                if (trivia.CSharpKind() != SyntaxKind.WhitespaceTrivia)
                {
                    return false;
                }

                // there was noisy char after end of line trivia
                if (!this.UseIndentation || this.touchedNoisyCharacterOnCurrentLine)
                {
                    this.hasTrailingSpace = true;
                    return false;
                }

                // right after end of line trivia. calculate indentation for current line
                Debug.Assert(trivia.ToString() == trivia.ToFullString());
                var text = trivia.ToString();

                // if text contains tab, we will give up perf optimization and use more expensive one to see whether we need to replace this trivia
                if (text.IndexOf('\t') >= 0)
                {
                    return true;
                }

                this.indentation += text.ConvertTabToSpace(this.optionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp), this.indentation, text.Length);

                return false;
            }

            private bool OnEndOfLine(SyntaxTrivia trivia, int currentIndex)
            {
                if (trivia.CSharpKind() != SyntaxKind.EndOfLineTrivia)
                {
                    return false;
                }

                // end of line trivia right after whitespace trivia
                if (this.hasTrailingSpace)
                {
                    // has trailing whitespace
                    return true;
                }

                // empty line with spaces. remove it.
                if (this.indentation > 0 && !this.touchedNoisyCharacterOnCurrentLine)
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private void ResetStateAfterNewLine(int currentIndex)
            {
                // reset states for current line
                this.indentation = 0;
                this.touchedNoisyCharacterOnCurrentLine = false;
                this.hasTrailingSpace = false;

                // remember last line break index
                this.lastLineBreakIndex = currentIndex;
            }

            private bool OnComment(SyntaxTrivia trivia, int currentIndex)
            {
                if (!trivia.IsRegularOrDocComment())
                {
                    return false;
                }

                // check whether indentation are right
                if (this.UseIndentation && this.indentation != this.context.GetBaseIndentation(trivia.SpanStart))
                {
                    // comment has wrong indentation
                    return true;
                }

                // go deep down for single line documentation comment
                if (trivia.IsSingleLineDocComment() &&
                    ShouldFormatSingleLineDocumentationComment(this.indentation, this.optionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp), trivia))
                {
                    return true;
                }

                return false;
            }

            private bool OnSkippedTokensOrText(SyntaxTrivia trivia)
            {
                if (trivia.CSharpKind() != SyntaxKind.SkippedTokensTrivia &&
                    trivia.CSharpKind() != SyntaxKind.PreprocessingMessageTrivia)
                {
                    return false;
                }

                return Contract.FailWithReturn<bool>("This can't happen");
            }

            private bool OnRegion(SyntaxTrivia trivia, int currentIndex)
            {
                if (trivia.CSharpKind() != SyntaxKind.RegionDirectiveTrivia &&
                    trivia.CSharpKind() != SyntaxKind.EndRegionDirectiveTrivia)
                {
                    return false;
                }

                if (!this.UseIndentation)
                {
                    return true;
                }

                if (indentation != this.context.GetBaseIndentation(trivia.SpanStart))
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private bool OnPreprocessor(SyntaxTrivia trivia, int currentIndex)
            {
                if (!SyntaxFacts.IsPreprocessorDirective(trivia.CSharpKind()))
                {
                    return false;
                }

                if (!this.UseIndentation)
                {
                    return true;
                }

                // preprocessor must be at from column 0
                if (this.indentation != 0)
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private bool OnTouchedNoisyCharacter(SyntaxTrivia trivia)
            {
                if (trivia.IsElastic() ||
                    trivia.CSharpKind() == SyntaxKind.WhitespaceTrivia ||
                    trivia.CSharpKind() == SyntaxKind.EndOfLineTrivia)
                {
                    return false;
                }

                this.touchedNoisyCharacterOnCurrentLine = true;
                this.hasTrailingSpace = false;

                return false;
            }

            private bool ShouldFormat()
            {
                var index = -1;
                foreach (var commonTrivia in triviaList)
                {
                    index++;
                    var trivia = (SyntaxTrivia)commonTrivia;

                    // order in which these methods run has a side effect. don't change the order
                    // each method run
                    if (OnElastic(trivia) ||
                        OnWhitespace(trivia) ||
                        OnEndOfLine(trivia, index) ||
                        OnTouchedNoisyCharacter(trivia) ||
                        OnComment(trivia, index) ||
                        OnSkippedTokensOrText(trivia) ||
                        OnRegion(trivia, index) ||
                        OnPreprocessor(trivia, index))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool ShouldFormatSingleLineDocumentationComment(int indentation, int tabSize, SyntaxTrivia trivia)
            {
                var xmlComment = (DocumentationCommentTriviaSyntax)trivia.GetStructure();

                var sawFirstOne = false;
                foreach (var token in xmlComment.DescendantTokens())
                {
                    foreach (var xmlTrivia in token.LeadingTrivia)
                    {
                        if (xmlTrivia.CSharpKind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                        {
                            // skip first one since its leading whitespace will belong to syntax tree's syntax token
                            // not xml doc comment's token
                            if (!sawFirstOne)
                            {
                                sawFirstOne = true;
                                break;
                            }

                            var xmlCommentText = xmlTrivia.ToString();

                            // "///" == 3.
                            if (xmlCommentText.GetColumnFromLineOffset(xmlCommentText.Length - 3, tabSize) != indentation)
                            {
                                return true;
                            }

                            break;
                        }
                    }
                }

                return false;
            }
        }
    }
}
