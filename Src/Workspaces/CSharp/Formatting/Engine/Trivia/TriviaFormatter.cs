using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.CSharp.Extensions;
using Roslyn.Services.Formatting;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Formatting
{
    internal partial class TriviaFormatter
    {
        #region Caches
        private static readonly string[] SpaceCache;
        #endregion

        private readonly FormattingContext context;
        private readonly SyntaxToken token1;
        private readonly SyntaxToken token2;

        private readonly int lineBreaks;
        private readonly int spaces;

        static TriviaFormatter()
        {
            SpaceCache = new string[20];
            for (int i = 0; i < 20; i++)
            {
                SpaceCache[i] = new string(' ', i);
            }
        }

        public TriviaFormatter(FormattingContext context, SyntaxToken token1, SyntaxToken token2, int lineBreaks, int spaces)
        {
            Contract.ThrowIfNull(context);
            Contract.ThrowIfFalse(lineBreaks >= 0);
            Contract.ThrowIfFalse(spaces >= 0);

            this.context = context;

            this.token1 = token1;
            this.token2 = token2;

            this.lineBreaks = lineBreaks;
            this.spaces = spaces;
        }

        public List<SyntaxTrivia> FormatToSyntaxTriviaList()
        {
            var list = TriviaListPool.Allocate();

            FormatAndAppendToSyntaxTriviaList(list);
            return TriviaListPool.ReturnAndFree(list);
        }

        public string FormatToString()
        {
            // optimized for common case
            // used pool to get short live heap objects
            var builder = StringBuilderPool.Allocate();
            var list = TriviaListPool.Allocate();

            FormatAndAppendToSyntaxTriviaList(list);

            for (int i = 0; i < list.Count; i++)
            {
                builder.Append(list[i].GetFullText());
            }

            TriviaListPool.Free(list);
            return StringBuilderPool.ReturnAndFree(builder);
        }

        private void FormatAndAppendToSyntaxTriviaList(List<SyntaxTrivia> triviaList)
        {
            int currentLineBreaks;

            var buffer = TriviaListPool.Allocate();
            var triviaBuilder = ProcessTriviaOnEachLine(buffer, triviaList, out currentLineBreaks);

            // single line trivia case
            if (this.lineBreaks == 0 && currentLineBreaks == 0)
            {
                var initialColumn = this.context.TreeInfo.GetColumnOfToken(token1, this.context.Options.TabSize) + token1.Width();
                triviaBuilder.CommitBetweenTokens(initialColumn, triviaList);
                return;
            }

            // multi line trivia case
            var beginningOfNewLine = currentLineBreaks != 0;
            currentLineBreaks += triviaBuilder.CommitLeftOver(beginningOfNewLine, this.spaces, triviaList);

            AppendLineBreakTrivia(currentLineBreaks, triviaList);

            var indentationString = this.spaces.CreateIndentationString(this.context.Options.UseTab, this.context.Options.TabSize);

            AppendWhitespaceTrivia(indentationString, triviaList);
            TriviaListPool.Free(buffer);
        }

        private void AppendLineBreakTrivia(int currentLineBreaks, List<SyntaxTrivia> triviaList)
        {
            if (currentLineBreaks >= this.lineBreaks)
            {
                return;
            }

            // by default, prepend extra new lines asked rather than append.
            var tempList = TriviaListPool.Allocate();
            for (int i = currentLineBreaks; i < this.lineBreaks; i++)
            {
                tempList.Add(Syntax.CarriageReturnLineFeed);
            }

            triviaList.InsertRange(0, tempList);
            TriviaListPool.Free(tempList);
        }

        private TriviaLineBuilder ProcessTriviaOnEachLine(List<SyntaxTrivia> buffer, List<SyntaxTrivia> triviaList, out int currentLineBreaks)
        {
            var triviaBuilder = new TriviaLineBuilder(this.context, buffer);

            // initialize
            currentLineBreaks = 0;

            var treatElasticAsNewLine = true;
            foreach (var trivia in GetTriviaBetweenTokens())
            {
                // trivia list we have is between two tokens. so trailing trivia belongs to previous token will not
                // start from new line.
                var beginningOfNewLine = currentLineBreaks != 0 || IsFirstTriviaInTree();

                if (trivia.IsElastic && treatElasticAsNewLine)
                {
                    treatElasticAsNewLine = false;
                    CommitLinesAndAddLineBreak(beginningOfNewLine, triviaList, ref currentLineBreaks, ref triviaBuilder);
                    continue;
                }

                if (trivia.Kind == SyntaxKind.EndOfLineTrivia)
                {
                    treatElasticAsNewLine = false;
                    CommitLinesAndAddLineBreak(beginningOfNewLine, triviaList, ref currentLineBreaks, ref triviaBuilder);
                    continue;
                }

                if (trivia.Kind == SyntaxKind.SingleLineCommentTrivia ||
                    trivia.Kind == SyntaxKind.MultiLineCommentTrivia)
                {
                    treatElasticAsNewLine = true;
                    triviaBuilder.Add(trivia);
                    continue;
                }

                if (trivia.Kind == SyntaxKind.DocumentationComment ||
                    trivia.Kind == SyntaxKind.DisabledTextTrivia ||
                    trivia.Kind.IsPreprocessorDirective())
                {
                    treatElasticAsNewLine = true;

                    if (trivia.Kind == SyntaxKind.DocumentationComment && !IsSingleLineDocumentComment(trivia))
                    {
                        triviaBuilder.Add(trivia);
                        continue;
                    }

                    CommitLines(beginningOfNewLine, triviaList, trivia, ref currentLineBreaks, ref triviaBuilder);
                    continue;
                }

                treatElasticAsNewLine = false;
                triviaBuilder.Add(trivia);
            }

            return triviaBuilder;
        }

        private static void CommitLines(
            bool beginningOfNewLine,
            List<SyntaxTrivia> triviaList,
            SyntaxTrivia trivia,
            ref int currentLineBreaks,
            ref TriviaLineBuilder triviaBuilder)
        {
            triviaBuilder.Add(trivia);
            currentLineBreaks += triviaBuilder.CommitLines(beginningOfNewLine, triviaList);

            triviaBuilder.Reset();
        }

        private static void CommitLinesAndAddLineBreak(
            bool beginningOfNewLine,
            List<SyntaxTrivia> triviaList,
            ref int currentLineBreaks,
            ref TriviaLineBuilder triviaBuilder)
        {
            currentLineBreaks += triviaBuilder.CommitLines(beginningOfNewLine, triviaList);

            triviaList.Add(Syntax.CarriageReturnLineFeed);
            currentLineBreaks++;

            triviaBuilder.Reset();
        }

        private static bool IsSingleLineDocumentComment(SyntaxTrivia trivia)
        {
            if (trivia.Kind != SyntaxKind.DocumentationComment)
            {
                return false;
            }

            // try to find first doc comment exterior trivia
            var xmlComment = (DocumentationCommentSyntax)trivia.GetStructure();
            foreach (var token in xmlComment.DescendantTokens())
            {
                foreach (var xmlTrivia in token.LeadingTrivia)
                {
                    if (xmlTrivia.Kind == SyntaxKind.DocumentationCommentExteriorTrivia)
                    {
                        return xmlTrivia.GetText().LastIndexOf("///") >= 0;
                    }
                }
            }

            return false;
        }

        private static void AppendWhitespaceTrivia(string indentationString, List<SyntaxTrivia> triviaList)
        {
            if (string.IsNullOrEmpty(indentationString))
            {
                return;
            }

            triviaList.Add(Syntax.Whitespace(indentationString, elastic: false));
        }

        private bool IsFirstTriviaInTree()
        {
            return this.token1.Kind == SyntaxKind.None;
        }

        private static string GetSpaces(int space)
        {
            if (space >= 0 && space < 20)
            {
                return SpaceCache[space];
            }

            return new string(' ', space);
        }

        private IEnumerable<SyntaxTrivia> GetTriviaBetweenTokens()
        {
            if (this.token1.Kind != SyntaxKind.None && this.token1.HasTrailingTrivia)
            {
                for (int i = 0; i < this.token1.TrailingTrivia.Count; i++)
                {
                    yield return this.token1.TrailingTrivia[i];
                }
            }

            if (this.token2.Kind != SyntaxKind.None && this.token2.HasLeadingTrivia)
            {
                for (int i = 0; i < this.token2.LeadingTrivia.Count; i++)
                {
                    yield return this.token2.LeadingTrivia[i];
                }
            }
        }

        public static bool ContainsSkippedTokensOrText(TriviaList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var trivia = list[i];

                if (trivia.Kind == SyntaxKind.SkippedTokens ||
                    trivia.Kind == SyntaxKind.PreprocessingMessageTrivia)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldFormatTriviaOnSingleLine(TriviaList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var trivia = list[i];

                Contract.ThrowIfTrue(trivia.Kind == SyntaxKind.EndOfLineTrivia);
                Contract.ThrowIfTrue(trivia.Kind == SyntaxKind.SkippedTokens);
                Contract.ThrowIfTrue(trivia.Kind == SyntaxKind.PreprocessingMessageTrivia);

                // if it contains elastic trivia, always format
                if (trivia.IsElastic)
                {
                    return true;
                }

                if (trivia.Kind == SyntaxKind.WhitespaceTrivia)
                {
                    Debug.Assert(trivia.GetText() == trivia.GetFullText());
                    var text = trivia.GetText();
                    if (text.IndexOf('\t') >= 0)
                    {
                        return true;
                    }
                }

                if (trivia.Kind == SyntaxKind.SingleLineCommentTrivia)
                {
                    return false;
                }

                if (trivia.Kind == SyntaxKind.MultiLineCommentTrivia)
                {
                    // we don't touch space between two tokens on a single line that contains
                    // multiline comments between them
                    return false;
                }

                if (trivia.Kind == SyntaxKind.DocumentationComment)
                {
                    if (IsSingleLineDocumentComment(trivia))
                    {
                        return false;
                    }

                    // we don't touch space between two tokens on a single line that contains
                    // multiline doc comments between them
                    return false;
                }

                if (trivia.Kind == SyntaxKind.RegionDirective ||
                    trivia.Kind == SyntaxKind.EndRegionDirective ||
                    trivia.Kind.IsPreprocessorDirective())
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ShouldFormatTriviaOnMultipleLines(FormattingOptions options, bool firstTriviaInTree, int desiredIndentation, TriviaList list)
        {
            return MultiLineAnalyzer.ShouldFormat(options, firstTriviaInTree, desiredIndentation, list);
        }
    }
}
