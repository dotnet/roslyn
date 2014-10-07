using System.Collections.Generic;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.CSharp.Extensions;
using Roslyn.Services.Formatting;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal partial class TriviaFormatter
    {
        private struct TriviaLineBuilder
        {
            private readonly FormattingContext context;
            private readonly List<SyntaxTrivia> triviaOnLine;

            private bool containsOnlyWhitespace;
            private bool containsSkippedTokensOrText;

            public TriviaLineBuilder(FormattingContext context, List<SyntaxTrivia> buffer)
            {
                Contract.ThrowIfNull(context);
                Contract.ThrowIfNull(buffer);

                this.context = context;
                this.triviaOnLine = buffer;
                this.triviaOnLine.Clear();

                this.containsOnlyWhitespace = true;
                this.containsSkippedTokensOrText = false;
            }

            public void Add(SyntaxTrivia trivia)
            {
                Contract.ThrowIfTrue(trivia.Kind == SyntaxKind.EndOfLineTrivia);

                if (trivia.Kind == SyntaxKind.WhitespaceTrivia)
                {
                    triviaOnLine.Add(trivia);
                    return;
                }

                this.containsOnlyWhitespace = false;

                if (trivia.Kind == SyntaxKind.SingleLineCommentTrivia ||
                    trivia.Kind == SyntaxKind.DisabledTextTrivia ||
                    trivia.Kind == SyntaxKind.RegionDirective ||
                    trivia.Kind == SyntaxKind.EndRegionDirective)
                {
                    triviaOnLine.Add(trivia);
                }
                else if (trivia.Kind == SyntaxKind.MultiLineCommentTrivia ||
                         trivia.Kind == SyntaxKind.DocumentationComment)
                {
                    triviaOnLine.Add(trivia);
                }
                else if (trivia.Kind == SyntaxKind.SkippedTokens ||
                         trivia.Kind == SyntaxKind.PreprocessingMessageTrivia)
                {
                    this.containsSkippedTokensOrText = true;
                    triviaOnLine.Add(trivia);
                }
                else
                {
                    Contract.ThrowIfFalse(trivia.Kind.IsPreprocessorDirective());
                    triviaOnLine.Add(trivia);
                }
            }

            public void Reset()
            {
                this.triviaOnLine.Clear();

                this.containsOnlyWhitespace = true;
                this.containsSkippedTokensOrText = false;
            }

            public void CommitBetweenTokens(int initialColumn, List<SyntaxTrivia> triviaList)
            {
                Contract.ThrowIfTrue(this.containsSkippedTokensOrText);
                Contract.ThrowIfTrue(this.containsOnlyWhitespace);

                var currentColumn = initialColumn;

                // okay, current line should contain more than whitespaces
                for (int i = 0; i < this.triviaOnLine.Count; i++)
                {
                    if (TryProcessOneWhitespaceTrivia(i, ref currentColumn, triviaList))
                    {
                        continue;
                    }

                    // the trivia is not a whitespace trivia, just add it to collection and add up full width
                    var trivia = this.triviaOnLine[i];

                    triviaList.Add(trivia);
                    currentColumn += trivia.FullWidth();
                }

                return;
            }

            public int CommitLines(bool beginningOfNewLine, List<SyntaxTrivia> triviaList)
            {
                Contract.ThrowIfTrue(this.containsSkippedTokensOrText);

                if (this.containsOnlyWhitespace)
                {
                    // number of lines committed
                    return 0;
                }

                // start index without any leading whitespace trivia when indentation is used. otherwise start from 0
                var startIndex = beginningOfNewLine ? GetFirstNonWhitespaceTriviaIndexInBuffer(startIndex: 0) : 0;
                Contract.ThrowIfFalse(startIndex >= 0);

                var baseIndentation = this.context.GetBaseIndentation(triviaOnLine[startIndex].Span.Start);
                return ProcessMultilineTrivia(beginningOfNewLine, startIndex, baseIndentation, triviaList);
            }

            public int CommitLeftOver(bool beginningOfNewLine, int indentation, List<SyntaxTrivia> triviaList)
            {
                Contract.ThrowIfTrue(this.containsSkippedTokensOrText);

                if (this.containsOnlyWhitespace)
                {
                    // number of lines committed
                    return 0;
                }

                // start index without any leading whitespace trivia when indentation is used. otherwise start from 0
                var startIndex = beginningOfNewLine ? GetFirstNonWhitespaceTriviaIndexInBuffer(startIndex: 0) : 0;
                Contract.ThrowIfFalse(startIndex >= 0);

                var lineBreaks = ProcessMultilineTrivia(beginningOfNewLine, startIndex, indentation, triviaList);

                triviaList.Add(Syntax.CarriageReturnLineFeed);
                return lineBreaks + 1;
            }

            private int ProcessMultilineTrivia(bool beginningOfNewLine, int startIndex, int indentation, List<SyntaxTrivia> triviaList)
            {
                var lineBreaks = 0;
                var currentColumn = indentation;

                var indentationDelta = beginningOfNewLine ? GetIndentationDelta(indentation) : 0;

                // okay, current line should contain more than whitespaces
                for (int i = startIndex; i < triviaOnLine.Count; i++)
                {
                    // indentation only matters to the very first one on current line
                    var appendIndentation = beginningOfNewLine && (i == startIndex);
                    if (TryProcessOneNonWhitespaceTrivia(i, appendIndentation, indentation, indentationDelta, ref beginningOfNewLine, ref lineBreaks, triviaList))
                    {
                        return lineBreaks;
                    }

                    if (TryProcessOneWhitespaceTrivia(i, ref currentColumn, triviaList))
                    {
                        continue;
                    }

                    // the trivia is not a whitespace trivia, just add its full width.
                    currentColumn += this.triviaOnLine[i].FullWidth();
                }

                return lineBreaks;
            }

            private bool TryProcessOneWhitespaceTrivia(int currentIndex, ref int currentColumn, List<SyntaxTrivia> triviaList)
            {
                var trivia = this.triviaOnLine[currentIndex];

                // if there is whitespace trivia between two trivia, make sure we calculate right spaces between them.
                if (trivia.Kind != SyntaxKind.WhitespaceTrivia)
                {
                    return false;
                }

                // whitespace between noisy characters. convert tab to space if there is any. 
                // tab can only appear in indentation
                var text = trivia.GetText();
                var spaces = text.ConvertStringTextPositionToColumn(this.context.Options.TabSize, currentColumn, text.Length);

                AppendWhitespaceTrivia(GetSpaces(spaces), triviaList);

                // add right number of spaces
                currentColumn += spaces;
                return true;
            }

            private bool TryProcessOneNonWhitespaceTrivia(
                int currentIndex,
                bool appendIndentation,
                int indentation,
                int indentationDelta,
                ref bool beginningOfNewLine,
                ref int lineBreaks,
                List<SyntaxTrivia> triviaList)
            {
                var trivia = this.triviaOnLine[currentIndex];

                // well easy case first.
                if (TrySimpleSingleLineOrDisabledTextCase(trivia, beginningOfNewLine, indentation, triviaList, ref lineBreaks))
                {
                    return true;
                }

                // now complex multiline stuff
                if (trivia.Kind == SyntaxKind.MultiLineCommentTrivia)
                {
                    lineBreaks += AppendMultilineDocumentOrRegularComment(appendIndentation, indentation, indentationDelta, trivia, triviaList);

                    // only whitespace left. remove trailing whitespace
                    if (HasOnlyTrailingWhitespace(currentIndex + 1))
                    {
                        return true;
                    }
                }

                if (trivia.Kind == SyntaxKind.DocumentationComment)
                {
                    lineBreaks += AppendMultilineDocumentOrRegularComment(appendIndentation, indentation, indentationDelta, trivia, triviaList);
                    if (DoneProcessingMultilineDocumentComment(trivia, currentIndex))
                    {
                        return true;
                    }
                }

                // no longer it is a beginning of a line
                beginningOfNewLine = false;

                return false;
            }

            private bool TrySimpleSingleLineOrDisabledTextCase(
                SyntaxTrivia trivia,
                bool appendIndentation,
                int indentation,
                List<SyntaxTrivia>
                triviaList,
                ref int lineBreaks)
            {
                // well easy case first.
                if (trivia.Kind == SyntaxKind.SingleLineCommentTrivia)
                {
                    AppendIndentationStringIfPossible(appendIndentation, indentation, triviaList);
                    triviaList.Add(trivia);
                    return true;
                }
                else if (trivia.Kind == SyntaxKind.RegionDirective ||
                         trivia.Kind == SyntaxKind.EndRegionDirective)
                {
                    AppendIndentationStringIfPossible(appendIndentation, indentation, triviaList);

                    lineBreaks += 1;
                    triviaList.Add(trivia);
                    return true;
                }
                else if (trivia.Kind.IsPreprocessorDirective())
                {
                    // for now, put it at the column 0
                    lineBreaks += 1;
                    triviaList.Add(trivia);
                    return true;
                }
                else if (trivia.Kind == SyntaxKind.DisabledTextTrivia)
                {
                    lineBreaks += trivia.GetFullText().GetNumberOfLineBreaks();
                    triviaList.Add(trivia);
                    return true;
                }

                return false;
            }

            private bool DoneProcessingMultilineDocumentComment(SyntaxTrivia trivia, int currentIndexInBuffer)
            {
                if (IsSingleLineDocumentComment(trivia))
                {
                    // can not have anything after this
                    return true;
                }

                // only whitespace left.
                if (HasOnlyTrailingWhitespace(currentIndexInBuffer + 1))
                {
                    return true;
                }

                return false;
            }

            private int AppendMultilineDocumentOrRegularComment(
                bool appendIndentation,
                int indentation,
                int indentationDelta,
                SyntaxTrivia trivia,
                List<SyntaxTrivia> triviaList)
            {
                // append indentation of the first line of the xml doc comment
                AppendIndentationStringIfPossible(appendIndentation, indentation, triviaList);

                var forceIndentation = IsSingleLineDocumentComment(trivia);
                if (indentationDelta == 0 && appendIndentation && !forceIndentation)
                {
                    // rest of them are already in good shape
                    triviaList.Add(trivia);
                }
                else
                {
                    // create new xml doc comment
                    AppendReindentedText(trivia, forceIndentation && appendIndentation, indentation, indentationDelta, triviaList);
                }

                return trivia.GetFullText().GetNumberOfLineBreaks();
            }

            private void AppendReindentedText(SyntaxTrivia trivia, bool forceIndentation, int indentation, int indentationDelta, List<SyntaxTrivia> triviaList)
            {
                var xmlDocumentation = trivia.GetFullText().ReindentStartOfXmlDocumentationComment(
                    forceIndentation,
                    indentation,
                    indentationDelta,
                    this.context.Options.UseTab,
                    this.context.Options.TabSize);

                var parsedTrivia = Syntax.ParseLeadingTrivia(xmlDocumentation);
                Contract.ThrowIfFalse(parsedTrivia.Count == 1);

                triviaList.Add(parsedTrivia[0]);
            }

            private int GetExistingIndentation()
            {
                var spaces = 0;

                for (int i = 0; i < triviaOnLine.Count; i++)
                {
                    var trivia = triviaOnLine[i];

                    if (trivia.Kind == SyntaxKind.WhitespaceTrivia)
                    {
                        var text = trivia.GetText();
                        spaces += text.ConvertStringTextPositionToColumn(this.context.Options.TabSize, spaces, text.Length);

                        continue;
                    }

                    return spaces;
                }

                return 0;
            }

            private int GetFirstNonWhitespaceTriviaIndexInBuffer(int startIndex)
            {
                for (int i = startIndex; i < triviaOnLine.Count; i++)
                {
                    var trivia = triviaOnLine[i];

                    // eat up all leading whitespaces (indentation)
                    if (trivia.Kind != SyntaxKind.WhitespaceTrivia)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private bool HasOnlyTrailingWhitespace(int startIndex)
            {
                return GetFirstNonWhitespaceTriviaIndexInBuffer(startIndex) < 0;
            }

            private void AppendIndentationStringIfPossible(bool appendIndentation, int indentation, List<SyntaxTrivia> triviaList)
            {
                // apply indentation only if we are told to do so.
                if (!appendIndentation)
                {
                    return;
                }

                var indentatationString = indentation.CreateIndentationString(this.context.Options.UseTab, this.context.Options.TabSize);
                AppendWhitespaceTrivia(indentatationString, triviaList);
            }

            private int GetIndentationDelta(int indentation)
            {
                return indentation - GetExistingIndentation();
            }
        }
    }
}
