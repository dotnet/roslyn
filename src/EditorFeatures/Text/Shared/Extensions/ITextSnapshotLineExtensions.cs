// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text.Shared.Extensions
{
    internal static class ITextSnapshotLineExtensions
    {
        /// <summary>
        /// Returns the first non-whitespace position on the given line, or null if 
        /// the line is empty or contains only whitespace.
        /// </summary>
        public static int? GetFirstNonWhitespacePosition(this ITextSnapshotLine line)
        {
            Contract.ThrowIfNull(line);

            var text = line.GetText();

            for (var i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return line.Start + i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first non-whitespace position on the given line as an offset
        /// from the start of the line, or null if the line is empty or contains only
        /// whitespace.
        /// </summary>
        public static int? GetFirstNonWhitespaceOffset(this ITextSnapshotLine line)
        {
            Contract.ThrowIfNull(line);

            var text = line.GetText();

            for (var i = 0; i < text.Length; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the last non-whitespace position on the given line, or null if 
        /// the line is empty or contains only whitespace.
        /// </summary>
        public static int? GetLastNonWhitespacePosition(this ITextSnapshotLine line)
            => line.AsTextLine().GetLastNonWhitespacePosition();

        /// <summary>
        /// Determines whether the specified line is empty or contains whitespace only.
        /// </summary>
        public static bool IsEmptyOrWhitespace(this ITextSnapshotLine line, int startIndex = 0, int endIndex = -1)
        {
            Contract.ThrowIfNull(line, "line");
            Contract.ThrowIfFalse(startIndex >= 0);

            var text = line.GetText();

            if (endIndex == -1)
            {
                endIndex = text.Length;
            }

            for (var i = startIndex; i < endIndex; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static ITextSnapshotLine? GetPreviousMatchingLine(this ITextSnapshotLine line, Func<ITextSnapshotLine, bool> predicate)
        {
            Contract.ThrowIfNull(line, @"line");
            Contract.ThrowIfNull(predicate, @"tree");

            if (line.LineNumber <= 0)
            {
                return null;
            }

            var snapshot = line.Snapshot;
            for (var lineNumber = line.LineNumber - 1; lineNumber >= 0; lineNumber--)
            {
                var currentLine = snapshot.GetLineFromLineNumber(lineNumber);
                if (!predicate(currentLine))
                {
                    continue;
                }

                return currentLine;
            }

            return null;
        }

        public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this ITextSnapshotLine line, IEditorOptions editorOptions)
            => line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(editorOptions.GetTabSize());

        public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this ITextSnapshotLine line, int tabSize)
            => line.GetText().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);

        public static int GetColumnFromLineOffset(this ITextSnapshotLine line, int lineOffset, IEditorOptions editorOptions)
            => line.GetText().GetColumnFromLineOffset(lineOffset, editorOptions.GetTabSize());

        public static int GetLineOffsetFromColumn(this ITextSnapshotLine line, int column, IEditorOptions editorOptions)
            => line.GetText().GetLineOffsetFromColumn(column, editorOptions.GetTabSize());

        /// <summary>
        /// Checks if the given line at the given snapshot index starts with the provided value.
        /// </summary>
        public static bool StartsWith(this ITextSnapshotLine line, int index, string value, bool ignoreCase)
        {
            var snapshot = line.Snapshot;
            if (index + value.Length > snapshot.Length)
                return false;

            for (var i = 0; i < value.Length; i++)
            {
                var snapshotIndex = index + i;
                var actualCharacter = snapshot[snapshotIndex];
                var expectedCharacter = value[i];

                if (ignoreCase)
                {
                    actualCharacter = char.ToLowerInvariant(actualCharacter);
                    expectedCharacter = char.ToLowerInvariant(expectedCharacter);
                }

                if (actualCharacter != expectedCharacter)
                    return false;
            }

            return true;
        }

        public static bool Contains(this ITextSnapshotLine line, int index, string value, bool ignoreCase)
        {
            var snapshot = line.Snapshot;
            for (var i = index; i < line.End; i++)
            {
                if (i + value.Length > snapshot.Length)
                    return false;

                if (line.StartsWith(i, value, ignoreCase))
                    return true;
            }

            return false;
        }
    }
}
