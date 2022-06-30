// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class TriviaDataFactory
    {
        private struct CodeShapeAnalyzer
        {
            private readonly FormattingContext _context;
            private readonly SyntaxFormattingOptions _options;
            private readonly TriviaList _triviaList;

            private int _indentation;
            private bool _hasTrailingSpace;
            private int _lastLineBreakIndex;
            private bool _touchedNoisyCharacterOnCurrentLine;

            public static bool ShouldFormatMultiLine(FormattingContext context, bool firstTriviaInTree, TriviaList triviaList)
            {
                var analyzer = new CodeShapeAnalyzer(context, firstTriviaInTree, triviaList);
                return analyzer.ShouldFormat();
            }

            public static bool ShouldFormatSingleLine(TriviaList list)
            {
                foreach (var trivia in list)
                {
                    Contract.ThrowIfTrue(trivia.Kind() == SyntaxKind.EndOfLineTrivia);
                    Contract.ThrowIfTrue(trivia.Kind() == SyntaxKind.SkippedTokensTrivia);
                    Contract.ThrowIfTrue(trivia.Kind() == SyntaxKind.PreprocessingMessageTrivia);

                    // if it contains elastic trivia, always format
                    if (trivia.IsElastic())
                    {
                        return true;
                    }

                    if (trivia.Kind() == SyntaxKind.WhitespaceTrivia)
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

                    if (trivia.Kind() == SyntaxKind.RegionDirectiveTrivia ||
                        trivia.Kind() == SyntaxKind.EndRegionDirectiveTrivia ||
                        SyntaxFacts.IsPreprocessorDirective(trivia.Kind()))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool ContainsSkippedTokensOrText(TriviaList list)
            {
                foreach (var trivia in list)
                {
                    if (trivia.Kind() is SyntaxKind.SkippedTokensTrivia or
                        SyntaxKind.PreprocessingMessageTrivia)
                    {
                        return true;
                    }
                }

                return false;
            }

            private CodeShapeAnalyzer(FormattingContext context, bool firstTriviaInTree, TriviaList triviaList)
            {
                _context = context;
                _options = context.Options;
                _triviaList = triviaList;

                _indentation = 0;
                _hasTrailingSpace = false;
                _lastLineBreakIndex = firstTriviaInTree ? 0 : -1;
                _touchedNoisyCharacterOnCurrentLine = false;
            }

            private bool UseIndentation
            {
                get { return _lastLineBreakIndex >= 0; }
            }

            private static bool OnElastic(SyntaxTrivia trivia)
            {
                // if this is structured trivia then we need to check for elastic trivia in any descendant
                if (trivia.GetStructure() is { ContainsAnnotations: true } structure)
                {
                    foreach (var t in structure.DescendantTrivia())
                    {
                        if (t.IsElastic())
                        {
                            return true;
                        }
                    }
                }

                // if it contains elastic trivia, always format
                return trivia.IsElastic();
            }

            private bool OnWhitespace(SyntaxTrivia trivia)
            {
                if (trivia.Kind() != SyntaxKind.WhitespaceTrivia)
                {
                    return false;
                }

                // there was noisy char after end of line trivia
                if (!this.UseIndentation || _touchedNoisyCharacterOnCurrentLine)
                {
                    _hasTrailingSpace = true;
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

                _indentation += text.ConvertTabToSpace(_options.TabSize, _indentation, text.Length);

                return false;
            }

            private bool OnEndOfLine(SyntaxTrivia trivia, int currentIndex)
            {
                if (trivia.Kind() != SyntaxKind.EndOfLineTrivia)
                {
                    return false;
                }

                // end of line trivia right after whitespace trivia
                if (_hasTrailingSpace)
                {
                    // has trailing whitespace
                    return true;
                }

                // empty line with spaces. remove it.
                if (_indentation > 0 && !_touchedNoisyCharacterOnCurrentLine)
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private void ResetStateAfterNewLine(int currentIndex)
            {
                // reset states for current line
                _indentation = 0;
                _touchedNoisyCharacterOnCurrentLine = false;
                _hasTrailingSpace = false;

                // remember last line break index
                _lastLineBreakIndex = currentIndex;
            }

            private bool OnComment(SyntaxTrivia trivia)
            {
                if (!trivia.IsRegularOrDocComment())
                {
                    return false;
                }

                // check whether indentation are right
                if (this.UseIndentation && _indentation != _context.GetBaseIndentation(trivia.SpanStart))
                {
                    // comment has wrong indentation
                    return true;
                }

                // go deep down for single line documentation comment
                if (trivia.IsSingleLineDocComment() &&
                    ShouldFormatSingleLineDocumentationComment(_indentation, _options.TabSize, trivia))
                {
                    return true;
                }

                return false;
            }

            private static bool OnSkippedTokensOrText(SyntaxTrivia trivia)
            {
                if (trivia.Kind() is not SyntaxKind.SkippedTokensTrivia and
                    not SyntaxKind.PreprocessingMessageTrivia)
                {
                    return false;
                }

                throw ExceptionUtilities.Unreachable;
            }

            private bool OnRegion(SyntaxTrivia trivia, int currentIndex)
            {
                if (trivia.Kind() is not SyntaxKind.RegionDirectiveTrivia and
                    not SyntaxKind.EndRegionDirectiveTrivia)
                {
                    return false;
                }

                if (!this.UseIndentation)
                {
                    return true;
                }

                if (_indentation != _context.GetBaseIndentation(trivia.SpanStart))
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private bool OnPreprocessor(SyntaxTrivia trivia, int currentIndex)
            {
                if (!SyntaxFacts.IsPreprocessorDirective(trivia.Kind()))
                {
                    return false;
                }

                if (!this.UseIndentation)
                {
                    return true;
                }

                // preprocessor must be at from column 0
                if (_indentation != 0)
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private bool OnTouchedNoisyCharacter(SyntaxTrivia trivia)
            {
                if (trivia.IsElastic() ||
                    trivia.Kind() == SyntaxKind.WhitespaceTrivia ||
                    trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    return false;
                }

                _touchedNoisyCharacterOnCurrentLine = true;
                _hasTrailingSpace = false;

                return false;
            }

            private bool ShouldFormat()
            {
                var index = -1;
                foreach (var trivia in _triviaList)
                {
                    index++;

                    // order in which these methods run has a side effect. don't change the order
                    // each method run
                    if (OnElastic(trivia) ||
                        OnWhitespace(trivia) ||
                        OnEndOfLine(trivia, index) ||
                        OnTouchedNoisyCharacter(trivia) ||
                        OnComment(trivia) ||
                        OnSkippedTokensOrText(trivia) ||
                        OnRegion(trivia, index) ||
                        OnPreprocessor(trivia, index) ||
                        OnDisabledTextTrivia(trivia, index))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool OnDisabledTextTrivia(SyntaxTrivia trivia, int index)
            {
                if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                {
                    var triviaString = trivia.ToString();
                    if (!string.IsNullOrEmpty(triviaString) && SyntaxFacts.IsNewLine(triviaString.Last()))
                    {
                        ResetStateAfterNewLine(index);
                    }
                }

                return false;
            }

            private static bool ShouldFormatSingleLineDocumentationComment(int indentation, int tabSize, SyntaxTrivia trivia)
            {
                Debug.Assert(trivia.HasStructure);

                var xmlComment = (DocumentationCommentTriviaSyntax)trivia.GetStructure()!;

                var sawFirstOne = false;
                foreach (var token in xmlComment.DescendantTokens())
                {
                    foreach (var xmlTrivia in token.LeadingTrivia)
                    {
                        if (xmlTrivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
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
