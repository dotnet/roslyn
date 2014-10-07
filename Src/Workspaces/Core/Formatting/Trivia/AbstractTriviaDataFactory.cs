using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        private const int SpaceCacheSize = 10;
        private const int LineBreakCacheSize = 5;
        private const int IndentationLevelCacheSize = 20;

        protected readonly TreeData TreeInfo;
        protected readonly FormattingOptions Options;

        private SpaceTriviaData[] spaces = new SpaceTriviaData[SpaceCacheSize];
        private WhitespaceTriviaData[,] whitespaces = new WhitespaceTriviaData[LineBreakCacheSize, IndentationLevelCacheSize];

        protected AbstractTriviaDataFactory(TreeData treeInfo, FormattingOptions options)
        {
            Contract.ThrowIfNull(treeInfo);
            Contract.ThrowIfNull(options);

            this.TreeInfo = treeInfo;
            this.Options = options;

            for (int i = 0; i < SpaceCacheSize; i++)
            {
                this.spaces[i] = new SpaceTriviaData(this.Options, space: i, elastic: false);
            }
        }

        protected TriviaData GetSpaceTriviaData(int space, bool elastic = false)
        {
            Contract.ThrowIfFalse(space >= 0);

            // if result has elastic trivia in them, never use cache
            if (elastic)
            {
                return new SpaceTriviaData(this.Options, space, elastic: true);
            }

            if (space < SpaceCacheSize)
            {
                return this.spaces[space];
            }

            // create a new space
            return new SpaceTriviaData(this.Options, space, elastic: false);
        }

        protected TriviaData GetWhitespaceTriviaData(int lineBreaks, int indentation, bool useTriviaAsItIs, bool elastic)
        {
            Contract.ThrowIfFalse(lineBreaks >= 0);
            Contract.ThrowIfFalse(indentation >= 0);

            // we can use cache
            //  #1. if whitespace trivia don't have any elastic trivia and
            //  #2. analysis (Item1) didnt find anything preventing us from using cache such as trailing whitespace before new line
            //  #3. number of line breaks (Item2) are under cache capacity (line breaks)
            //  #4. indenation (Item3) is aligned to indentation level
            var canUseCache = !elastic &&
                              useTriviaAsItIs &&
                              lineBreaks > 0 &&
                              lineBreaks <= LineBreakCacheSize &&
                              indentation % this.Options.IndentationSize == 0;

            if (canUseCache)
            {
                int indentationLevel = indentation / this.Options.IndentationSize;
                if (indentationLevel < IndentationLevelCacheSize)
                {
                    var lineIndex = lineBreaks - 1;
                    EnsureWhitespaceTriviaInfo(lineIndex, indentationLevel);
                    return this.whitespaces[lineIndex, indentationLevel];
                }
            }

            return
                useTriviaAsItIs ?
                    new WhitespaceTriviaData(this.Options, lineBreaks, indentation, elastic) :
                    new ModifiedWithoutOriginalTriviaData(this.Options, lineBreaks, indentation, elastic);
        }

        private void EnsureWhitespaceTriviaInfo(int lineIndex, int indentationLevel)
        {
            Contract.ThrowIfFalse(lineIndex >= 0 && lineIndex < LineBreakCacheSize);
            Contract.ThrowIfFalse(indentationLevel >= 0 && indentationLevel < this.whitespaces.Length / this.whitespaces.Rank);

            // set up caches
            if (this.whitespaces[lineIndex, indentationLevel] == null)
            {
                var indentation = indentationLevel * this.Options.IndentationSize;
                var triviaInfo = new WhitespaceTriviaData(this.Options, lineBreaks: lineIndex + 1, indentation: indentation, elastic: false);
                Interlocked.CompareExchange(ref this.whitespaces[lineIndex, indentationLevel], triviaInfo, null);
            }
        }

        public abstract TriviaData CreateLeadingTrivia(CommonSyntaxToken token);
        public abstract TriviaData CreateTrailingTrivia(CommonSyntaxToken token);
        public abstract TriviaData Create(CommonSyntaxToken token1, CommonSyntaxToken token2);
    }
}
