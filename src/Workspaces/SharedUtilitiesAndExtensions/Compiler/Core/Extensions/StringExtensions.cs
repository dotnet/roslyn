// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class StringExtensions
{
    extension(string line)
    {
        public int? GetFirstNonWhitespaceOffset()
        {
            Contract.ThrowIfNull(line);

            for (var i = 0; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return i;
                }
            }

            return null;
        }

        public int? GetLastNonWhitespaceOffset()
        {
            Contract.ThrowIfNull(line);

            for (var i = line.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    return i;
                }
            }

            return null;
        }

        public int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(int tabSize)
        {
            var firstNonWhitespaceChar = line.GetFirstNonWhitespaceOffset();

            if (firstNonWhitespaceChar.HasValue)
            {
                return line.GetColumnFromLineOffset(firstNonWhitespaceChar.Value, tabSize);
            }
            else
            {
                // It's all whitespace, so go to the end
                return line.GetColumnFromLineOffset(line.Length, tabSize);
            }
        }

        public int GetColumnFromLineOffset(int endPosition, int tabSize)
        {
            Contract.ThrowIfNull(line);
            Contract.ThrowIfFalse(0 <= endPosition && endPosition <= line.Length);
            Contract.ThrowIfFalse(tabSize > 0);

            return ConvertTabToSpace(line, tabSize, 0, endPosition);
        }

        public int GetLineOffsetFromColumn(int column, int tabSize)
        {
            Contract.ThrowIfNull(line);
            Contract.ThrowIfFalse(column >= 0);
            Contract.ThrowIfFalse(tabSize > 0);

            var currentColumn = 0;

            for (var i = 0; i < line.Length; i++)
            {
                if (currentColumn >= column)
                {
                    return i;
                }

                if (line[i] == '\t')
                {
                    currentColumn += tabSize - (currentColumn % tabSize);
                }
                else
                {
                    currentColumn++;
                }
            }

            // We're asking for a column past the end of the line, so just go to the end.
            return line.Length;
        }
    }

    extension(string lineText)
    {
        public string GetLeadingWhitespace()
        {
            Contract.ThrowIfNull(lineText);

            var firstOffset = lineText.GetFirstNonWhitespaceOffset();

            return firstOffset.HasValue
                ? lineText[..firstOffset.Value]
                : lineText;
        }

        public string GetTrailingWhitespace()
        {
            Contract.ThrowIfNull(lineText);

            var lastOffset = lineText.GetLastNonWhitespaceOffset();

            return lastOffset.HasValue && lastOffset.Value < lineText.Length
                ? lineText[(lastOffset.Value + 1)..]
                : string.Empty;
        }
    }

    extension(string text)
    {
        public int GetTextColumn(int tabSize, int initialColumn)
        {
            var lineText = text.GetLastLineText();
            if (text != lineText)
            {
                return lineText.GetColumnFromLineOffset(lineText.Length, tabSize);
            }

            return text.ConvertTabToSpace(tabSize, initialColumn, text.Length) + initialColumn;
        }

        public string GetFirstLineText()
        {
            var lineBreak = text.IndexOf('\n');
            if (lineBreak < 0)
            {
                return text;
            }

            return text[..(lineBreak + 1)];
        }

        public string GetLastLineText()
        {
            var lineBreak = text.LastIndexOf('\n');
            if (lineBreak < 0)
            {
                return text;
            }

            return text[(lineBreak + 1)..];
        }

        public bool ContainsLineBreak()
        {
            foreach (var ch in text)
            {
                if (ch is '\n' or '\r')
                {
                    return true;
                }
            }

            return false;
        }

        public int GetNumberOfLineBreaks()
        {
            var lineBreaks = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lineBreaks++;
                }
                else if (text[i] == '\r')
                {
                    if (i + 1 == text.Length || text[i + 1] != '\n')
                    {
                        lineBreaks++;
                    }
                }
            }

            return lineBreaks;
        }

        public bool ContainsTab()
        {
            // PERF: Tried replacing this with "text.IndexOf('\t')>=0", but that was actually slightly slower
            foreach (var ch in text)
            {
                if (ch == '\t')
                {
                    return true;
                }
            }

            return false;
        }

        public ImmutableArray<SymbolDisplayPart> ToSymbolDisplayParts()
            => [new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text)];
    }

    extension(string textSnippet)
    {
        public int ConvertTabToSpace(int tabSize, int initialColumn, int endPosition)
        {
            Debug.Assert(tabSize > 0);
            Debug.Assert(endPosition >= 0 && endPosition <= textSnippet.Length);

            var column = initialColumn;

            // now this will calculate indentation regardless of actual content on the buffer except TAB
            for (var i = 0; i < endPosition; i++)
            {
                if (textSnippet[i] == '\t')
                {
                    column += tabSize - column % tabSize;
                }
                else
                {
                    column++;
                }
            }

            return column - initialColumn;
        }
    }

    extension(string? text)
    {
        public int IndexOf(Func<char, bool> predicate)
        {
            if (text == null)
            {
                return -1;
            }

            for (var i = 0; i < text.Length; i++)
            {
                if (predicate(text[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    extension(string? alias)
    {
        public void AppendToAliasNameSet(ImmutableHashSet<string>.Builder builder)
        {
            if (RoslynString.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            builder.Add(alias);

            var caseSensitive = builder.KeyComparer == StringComparer.Ordinal;
            Debug.Assert(builder.KeyComparer == StringComparer.Ordinal || builder.KeyComparer == StringComparer.OrdinalIgnoreCase);
            if (alias.TryGetWithoutAttributeSuffix(caseSensitive, out var aliasWithoutAttribute))
            {
                builder.Add(aliasWithoutAttribute);
                return;
            }

            builder.Add(alias.GetWithSingleAttributeSuffix(caseSensitive));
        }
    }
}
