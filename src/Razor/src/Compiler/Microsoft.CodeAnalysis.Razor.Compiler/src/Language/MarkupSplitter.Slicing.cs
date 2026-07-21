// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class MarkupSplitter
{
    /// <summary>
    /// Splits a raw C# node at one or more offsets within its concatenated text, producing a piece per
    /// sub-range. A single class-body C# chunk often straddles several parsed members (e.g. a field
    /// immediately followed by a markup-bearing method), so it must be cut at the member boundaries and
    /// each slice routed independently. Each produced token's <see cref="SourceSpan"/> is recomputed --
    /// line and character indices, not just the absolute index -- because a slice that starts after a
    /// newline maps to a different line, and getting that wrong corrupts source mappings.
    /// </summary>
    /// <param name="node">The C# node to split.</param>
    /// <param name="cuts">Strictly-increasing offsets in the node's concatenated text, each strictly
    /// between 0 and the text length. Produces <c>cuts.Length + 1</c> pieces.</param>
    internal static ImmutableArray<CSharpCodeIntermediateNode> SplitCSharpNode(
        CSharpCodeIntermediateNode node,
        ImmutableArray<int> cuts)
    {
        if (cuts.IsDefaultOrEmpty)
        {
            return [node];
        }

        var pieces = new CSharpCodeIntermediateNode[cuts.Length + 1];
        for (var i = 0; i < pieces.Length; i++)
        {
            pieces[i] = new CSharpCodeIntermediateNode { Source = node.Source, IsImported = node.IsImported };
        }

        // Walk each token, tracking its start offset in the node's concatenated text. A token that spans
        // one or more cut points is itself sliced so each slice lands in the correct piece.
        var tokenStart = 0;
        foreach (var child in node.Children)
        {
            if (child is not IntermediateToken token)
            {
                continue;
            }

            var content = token.Content;
            var tokenEnd = tokenStart + content.Length;

            // The cut offsets that fall strictly inside this token become local slice boundaries.
            var localBoundaries = new List<int> { 0 };
            var pieceOfFirstSlice = PieceIndexOf(cuts, tokenStart);

            foreach (var cut in cuts)
            {
                if (cut > tokenStart && cut < tokenEnd)
                {
                    localBoundaries.Add(cut - tokenStart);
                }
            }

            localBoundaries.Add(content.Length);

            for (var i = 0; i < localBoundaries.Count - 1; i++)
            {
                var localStart = localBoundaries[i];
                var localLength = localBoundaries[i + 1] - localBoundaries[i];
                if (localLength == 0)
                {
                    continue;
                }

                var pieceIndex = pieceOfFirstSlice + i;
                pieces[pieceIndex].Children.Add(SliceToken(token, localStart, localLength));
            }

            tokenStart = tokenEnd;
        }

        return ImmutableArray.Create(pieces);
    }

    // The index of the piece that content at the given node-text offset belongs to: the number of cut
    // points at or before the offset.
    private static int PieceIndexOf(ImmutableArray<int> cuts, int offset)
    {
        var index = 0;
        foreach (var cut in cuts)
        {
            if (cut <= offset)
            {
                index++;
            }
            else
            {
                break;
            }
        }

        return index;
    }

    /// <summary>
    /// Produces a token for the substring <c>[localStart, localStart + localLength)</c> of the given
    /// token's content, with its <see cref="SourceSpan"/> advanced from the token's start across the
    /// skipped prefix (so line/character indices are correct even across newlines).
    /// </summary>
    internal static CSharpIntermediateToken SliceToken(IntermediateToken token, int localStart, int localLength)
    {
        var content = token.Content;
        var slicedContent = content.Substring(localStart, localLength);

        if (token.Source is not { } source)
        {
            return new CSharpIntermediateToken(slicedContent, source: null);
        }

        var (startAbsolute, startLine, startCharacter) =
            AdvanceLocation(source.AbsoluteIndex, source.LineIndex, source.CharacterIndex, content, 0, localStart);

        var (_, endLine, endCharacter) =
            AdvanceLocation(startAbsolute, startLine, startCharacter, content, localStart, localLength);

        var slicedSource = new SourceSpan(
            source.FilePath,
            startAbsolute,
            startLine,
            startCharacter,
            localLength,
            // LineCount is the number of line breaks the span covers (0 for a single-line slice), matching
            // how the writer derives the enhanced #line end line as LineIndex + 1 + LineCount.
            lineCount: endLine - startLine,
            endCharacterIndex: endCharacter);

        return new CSharpIntermediateToken(slicedContent, slicedSource);
    }

    /// <summary>
    /// Advances a source location across <paramref name="count"/> characters of <paramref name="text"/>
    /// starting at <paramref name="start"/>. Matches the writer's line-break accounting: <c>\r\n</c>, a
    /// lone <c>\r</c>, and a lone <c>\n</c> each count as one line break that resets the character index.
    /// </summary>
    internal static (int Absolute, int Line, int Character) AdvanceLocation(
        int absolute, int line, int character, string text, int start, int count)
    {
        var i = start;
        var end = start + count;

        while (i < end)
        {
            var c = text[i];
            absolute++;

            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    // Consume the paired newline as a single line break.
                    absolute++;
                    i++;
                }

                line++;
                character = 0;
            }
            else if (c == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }

            i++;
        }

        return (absolute, line, character);
    }
}
