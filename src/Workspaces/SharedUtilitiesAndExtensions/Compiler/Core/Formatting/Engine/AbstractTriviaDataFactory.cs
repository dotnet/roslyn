// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract partial class AbstractTriviaDataFactory
{
    private const int SpaceCacheSize = 10;
    private const int LineBreakCacheSize = 5;
    private const int IndentationLevelCacheSize = 20;

    private static readonly Dictionary<LineFormattingOptions, (Whitespace[] spaces, Whitespace[,] whitespaces)> s_optionsToWhitespace = [];
    private static Tuple<LineFormattingOptions, (Whitespace[] spaces, Whitespace[,] whitespaces)>? s_lastOptionAndWhitespace;

    protected readonly TreeData TreeInfo;
    protected readonly LineFormattingOptions Options;

    private readonly Whitespace[] _spaces;
    private readonly Whitespace[,] _whitespaces;

    protected AbstractTriviaDataFactory(TreeData treeInfo, LineFormattingOptions options)
    {
        Contract.ThrowIfNull(treeInfo);

        TreeInfo = treeInfo;
        Options = options;

        (_spaces, _whitespaces) = GetSpacesAndWhitespaces(options);
    }

    private static (Whitespace[] spaces, Whitespace[,] whitespaces) GetSpacesAndWhitespaces(LineFormattingOptions options)
    {
        // Fast path where we'er asking for the same options as last time
        var lastOptionAndWhitespace = s_lastOptionAndWhitespace;
        if (lastOptionAndWhitespace?.Item1 == options)
            return lastOptionAndWhitespace.Item2;

        // Otherwise, get from the dictionary, computing if necessary.
        var (spaces, whitespaces) = ComputeAndCacheSpacesAndWhitespaces(options);

        // Cache this result for the next time.
        s_lastOptionAndWhitespace = Tuple.Create(options, (spaces, whitespaces));
        return (spaces, whitespaces);

        static (Whitespace[] spaces, Whitespace[,] whitespaces) ComputeAndCacheSpacesAndWhitespaces(LineFormattingOptions options)
        {
            // First check if it's already in the cache.
            lock (s_optionsToWhitespace)
            {
                if (s_optionsToWhitespace.TryGetValue(options, out var result))
                    return result;
            }

            // If not, compute it.
            var spaces = new Whitespace[SpaceCacheSize];
            for (var i = 0; i < SpaceCacheSize; i++)
                spaces[i] = new Whitespace(options, space: i, elastic: false);

            var whitespaces = new Whitespace[LineBreakCacheSize, IndentationLevelCacheSize];
            for (var lineIndex = 0; lineIndex < LineBreakCacheSize; lineIndex++)
            {
                for (var indentationLevel = 0; indentationLevel < IndentationLevelCacheSize; indentationLevel++)
                {
                    var indentation = indentationLevel * options.IndentationSize;
                    whitespaces[lineIndex, indentationLevel] = new Whitespace(
                        options, lineBreaks: lineIndex + 1, indentation: indentation, elastic: false);
                }
            }

            // Attempt to store in cache.  But defer to any other thread that may have already stored it.
            lock (s_optionsToWhitespace)
            {
                if (s_optionsToWhitespace.TryGetValue(options, out var result))
                    return result;

                s_optionsToWhitespace[options] = (spaces, whitespaces);
                return (spaces, whitespaces);
            }
        }
    }

    protected TriviaData GetSpaceTriviaData(int space, bool elastic = false)
    {
        Contract.ThrowIfFalse(space >= 0);

        // if result has elastic trivia in them, never use cache
        if (elastic)
        {
            return new Whitespace(this.Options, space, elastic: true);
        }

        if (space < SpaceCacheSize)
        {
            return _spaces[space];
        }

        // create a new space
        return new Whitespace(this.Options, space, elastic: false);
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
                          indentation % Options.IndentationSize == 0;

        if (canUseCache)
        {
            var indentationLevel = indentation / Options.IndentationSize;
            if (indentationLevel < IndentationLevelCacheSize)
            {
                var lineIndex = lineBreaks - 1;
                return _whitespaces[lineIndex, indentationLevel];
            }
        }

        return useTriviaAsItIs
            ? new Whitespace(this.Options, lineBreaks, indentation, elastic)
            : new ModifiedWhitespace(this.Options, lineBreaks, indentation, elastic);
    }

    public abstract TriviaData CreateLeadingTrivia(SyntaxToken token);
    public abstract TriviaData CreateTrailingTrivia(SyntaxToken token);
    public abstract TriviaData Create(SyntaxToken token1, SyntaxToken token2);
}
