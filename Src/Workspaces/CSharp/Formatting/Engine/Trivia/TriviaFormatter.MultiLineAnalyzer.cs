using System.Diagnostics;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Formatting;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal partial class TriviaFormatter
    {
        private struct MultiLineAnalyzer
        {
            private readonly FormattingOptions options;
            private readonly int desiredIndentation;
            private readonly TriviaList triviaList;

            private int indentation;
            private bool hasTrailingSpace;
            private int lastLineBreakIndex;
            private bool touchedNoisyCharacterOnCurrentLine;

            public static bool ShouldFormat(FormattingOptions options, bool firstTriviaInTree, int desiredIndentation, TriviaList triviaList)
            {
                var analyzer = new MultiLineAnalyzer(options, firstTriviaInTree, desiredIndentation, triviaList);
                return analyzer.ShouldFormat();
            }

            private MultiLineAnalyzer(FormattingOptions options, bool firstTriviaInTree, int desiredIndentation, TriviaList triviaList)
            {
                this.options = options;
                this.desiredIndentation = desiredIndentation;
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
                return trivia.IsElastic;
            }

            private bool OnWhitespace(SyntaxTrivia trivia)
            {
                if (trivia.Kind != SyntaxKind.WhitespaceTrivia)
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
                Debug.Assert(trivia.GetText() == trivia.GetFullText());
                var text = trivia.GetText();

                // if text contains tab, we will give up perf optimization and use more expensive one to see whether we need to replace this trivia
                if (text.IndexOf('\t') >= 0)
                {
                    return true;
                }

                this.indentation += text.ConvertStringTextPositionToColumn(this.options.TabSize, this.indentation, text.Length);

                return false;
            }

            private bool OnEndOfLine(SyntaxTrivia trivia, int currentIndex)
            {
                if (trivia.Kind != SyntaxKind.EndOfLineTrivia)
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
                if (trivia.Kind != SyntaxKind.SingleLineCommentTrivia &&
                    trivia.Kind != SyntaxKind.MultiLineCommentTrivia &&
                    trivia.Kind != SyntaxKind.DocumentationComment)
                {
                    return false;
                }

                // check whether indentation are right
                if (this.UseIndentation && this.indentation != this.desiredIndentation)
                {
                    // comment has wrong indentation
                    return true;
                }

                // go deep down for single line documentation comment
                if (IsSingleLineDocumentComment(trivia) &&
                    ShouldFormatSingleLineDocumentationComment(this.indentation, this.options.TabSize, trivia))
                {
                    return true;
                }

                return false;
            }

            private bool OnSkippedTokensOrText(SyntaxTrivia trivia)
            {
                if (trivia.Kind != SyntaxKind.SkippedTokens &&
                    trivia.Kind != SyntaxKind.PreprocessingMessageTrivia)
                {
                    return false;
                }

                return Contract.FailWithReturn<bool>("This can't happen");
            }

            private bool OnRegion(SyntaxTrivia trivia, int currentIndex)
            {
                if (trivia.Kind != SyntaxKind.RegionDirective &&
                    trivia.Kind != SyntaxKind.EndRegionDirective)
                {
                    return false;
                }

                if (!this.UseIndentation)
                {
                    return true;
                }

                if (indentation != desiredIndentation)
                {
                    return true;
                }

                ResetStateAfterNewLine(currentIndex);
                return false;
            }

            private bool OnPreprocessor(SyntaxTrivia trivia, int currentIndex)
            {
                if (!trivia.Kind.IsPreprocessorDirective())
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
                if (trivia.IsElastic ||
                    trivia.Kind == SyntaxKind.WhitespaceTrivia ||
                    trivia.Kind == SyntaxKind.EndOfLineTrivia)
                {
                    return false;
                }

                this.touchedNoisyCharacterOnCurrentLine = true;
                this.hasTrailingSpace = false;

                return false;
            }

            private bool ShouldFormat()
            {
                for (int i = 0; i < triviaList.Count; i++)
                {
                    var trivia = triviaList[i];

                    // order in which these methods run has a side effect. don't change the order
                    // each method run
                    if (OnElastic(trivia) ||
                        OnWhitespace(trivia) ||
                        OnEndOfLine(trivia, i) ||
                        OnTouchedNoisyCharacter(trivia) ||
                        OnComment(trivia, i) ||
                        OnSkippedTokensOrText(trivia) ||
                        OnRegion(trivia, i) ||
                        OnPreprocessor(trivia, i))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool ShouldFormatSingleLineDocumentationComment(int indentation, int tabSize, SyntaxTrivia trivia)
            {
                var xmlComment = (DocumentationCommentSyntax)trivia.GetStructure();

                var sawFirstOne = false;
                foreach (var token in xmlComment.DescendantTokens())
                {
                    foreach (var xmlTrivia in token.LeadingTrivia)
                    {
                        if (xmlTrivia.Kind == SyntaxKind.DocumentationCommentExteriorTrivia)
                        {
                            // skip first one since its leading whitespace will belong to syntax tree's syntax token
                            // not xml doc comment's token
                            if (!sawFirstOne)
                            {
                                sawFirstOne = true;
                                break;
                            }

                            var xmlCommentText = xmlTrivia.GetText();

                            // "///" == 3.
                            if (xmlCommentText.ConvertStringTextPositionToColumn(tabSize, xmlCommentText.Length - 3) != indentation)
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
