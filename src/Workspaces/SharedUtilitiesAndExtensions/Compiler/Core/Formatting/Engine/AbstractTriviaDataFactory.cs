// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class AbstractTriviaDataFactory
    {
        private const int SpaceCacheSize = 10;
        private const int LineBreakCacheSize = 5;
        private const int IndentationLevelCacheSize = 20;

        protected readonly TreeData TreeInfo;
        protected readonly AnalyzerConfigOptions Options;

        protected readonly bool UseTabs;
        protected readonly int TabSize;
        protected readonly int IndentationSize;

        private readonly Whitespace[] _spaces;
        private readonly Whitespace?[,] _whitespaces = new Whitespace[LineBreakCacheSize, IndentationLevelCacheSize];

        protected AbstractTriviaDataFactory(TreeData treeInfo, AnalyzerConfigOptions options)
        {
            Contract.ThrowIfNull(treeInfo);
            Contract.ThrowIfNull(options);

            this.TreeInfo = treeInfo;
            this.Options = options;

            UseTabs = options.GetOption(FormattingOptions2.UseTabs);
            TabSize = options.GetOption(FormattingOptions2.TabSize);
            IndentationSize = options.GetOption(FormattingOptions2.IndentationSize);

            _spaces = new Whitespace[SpaceCacheSize];
            for (var i = 0; i < SpaceCacheSize; i++)
            {
                _spaces[i] = new Whitespace(this.Options, space: i, elastic: false, language: treeInfo.Root.Language);
            }
        }

        protected TriviaData GetSpaceTriviaData(int space, bool elastic = false)
        {
            Contract.ThrowIfFalse(space >= 0);

            // if result has elastic trivia in them, never use cache
            if (elastic)
            {
                return new Whitespace(this.Options, space, elastic: true, language: this.TreeInfo.Root.Language);
            }

            if (space < SpaceCacheSize)
            {
                return _spaces[space];
            }

            // create a new space
            return new Whitespace(this.Options, space, elastic: false, language: this.TreeInfo.Root.Language);
        }

        protected TriviaData GetWhitespaceTriviaData(int lineBreaks, int indentation, bool useTriviaAsItIs, bool elastic)
        {
            Contract.ThrowIfFalse(lineBreaks >= 0);
            Contract.ThrowIfFalse(indentation >= 0);

            // we can use cache
            //  #1. if whitespace trivia don't have any elastic trivia and
            //  #2. analysis (Item1) didn't find anything preventing us from using cache such as trailing whitespace before new line
            //  #3. number of line breaks (Item2) are under cache capacity (line breaks)
            //  #4. indentation (Item3) is aligned to indentation level
            var canUseCache = !elastic &&
                              useTriviaAsItIs &&
                              lineBreaks > 0 &&
                              lineBreaks <= LineBreakCacheSize &&
                              indentation % IndentationSize == 0;

            if (canUseCache)
            {
                var indentationLevel = indentation / IndentationSize;
                if (indentationLevel < IndentationLevelCacheSize)
                {
                    var lineIndex = lineBreaks - 1;
                    EnsureWhitespaceTriviaInfo(lineIndex, indentationLevel);
                    return _whitespaces[lineIndex, indentationLevel]!;
                }
            }

            return
                useTriviaAsItIs ?
                    new Whitespace(this.Options, lineBreaks, indentation, elastic, language: this.TreeInfo.Root.Language) :
                    new ModifiedWhitespace(this.Options, lineBreaks, indentation, elastic, language: this.TreeInfo.Root.Language);
        }

        private void EnsureWhitespaceTriviaInfo(int lineIndex, int indentationLevel)
        {
            Contract.ThrowIfFalse(lineIndex >= 0 && lineIndex < LineBreakCacheSize);
            Contract.ThrowIfFalse(indentationLevel >= 0 && indentationLevel < _whitespaces.Length / _whitespaces.Rank);

            // set up caches
            if (_whitespaces[lineIndex, indentationLevel] == null)
            {
                var indentation = indentationLevel * IndentationSize;
                var triviaInfo = new Whitespace(this.Options, lineBreaks: lineIndex + 1, indentation: indentation, elastic: false, language: this.TreeInfo.Root.Language);
                Interlocked.CompareExchange(ref _whitespaces[lineIndex, indentationLevel], triviaInfo, null);
            }
        }

        public abstract TriviaData CreateLeadingTrivia(SyntaxToken token);
        public abstract TriviaData CreateTrailingTrivia(SyntaxToken token);
        public abstract TriviaData Create(SyntaxToken token1, SyntaxToken token2);
    }
}
