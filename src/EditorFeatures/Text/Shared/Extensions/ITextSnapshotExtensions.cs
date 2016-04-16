// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text.Shared.Extensions
{
    internal static partial class ITextSnapshotExtensions
    {
        public static SnapshotPoint GetPoint(this ITextSnapshot snapshot, int position)
        {
            return new SnapshotPoint(snapshot, position);
        }

        public static SnapshotPoint GetPoint(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            return new SnapshotPoint(snapshot, snapshot.GetPosition(lineNumber, columnIndex));
        }

        /// <summary>
        /// Convert a <see cref="LinePositionSpan"/> to <see cref="TextSpan"/>.
        /// </summary>
        public static TextSpan GetTextSpan(this ITextSnapshot snapshot, LinePositionSpan span)
        {
            return TextSpan.FromBounds(
                GetPosition(snapshot, span.Start.Line, span.Start.Character),
                GetPosition(snapshot, span.End.Line, span.End.Character));
        }

        public static int GetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            return TryGetPosition(snapshot, lineNumber, columnIndex).Value;
        }

        public static int? TryGetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return null;
            }

            int end = snapshot.GetLineFromLineNumber(lineNumber).Start.Position + columnIndex;
            if (end < 0 || end > snapshot.Length)
            {
                return null;
            }

            return end;
        }

        public static bool TryGetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex, out SnapshotPoint position)
        {
            int result = 0;
            position = new SnapshotPoint();

            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return false;
            }

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            if (columnIndex < 0 || columnIndex >= line.Length)
            {
                return false;
            }

            result = line.Start.Position + columnIndex;
            position = new SnapshotPoint(snapshot, result);
            return true;
        }

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, int start, int length)
        {
            return new SnapshotSpan(snapshot, new Span(start, length));
        }

        public static SnapshotSpan GetSpanFromBounds(this ITextSnapshot snapshot, int start, int end)
        {
            return new SnapshotSpan(snapshot, Span.FromBounds(start, end));
        }

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, Span span)
        {
            return new SnapshotSpan(snapshot, span);
        }

        public static ITagSpan<TTag> GetTagSpan<TTag>(this ITextSnapshot snapshot, Span span, TTag tag)
            where TTag : ITag
        {
            return new TagSpan<TTag>(new SnapshotSpan(snapshot, span), tag);
        }

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex)
        {
            return TryGetSpan(snapshot, startLine, startIndex, endLine, endIndex).Value;
        }

        public static SnapshotSpan? TryGetSpan(this ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex)
        {
            var startPosition = snapshot.TryGetPosition(startLine, startIndex);
            var endPosition = snapshot.TryGetPosition(endLine, endIndex);
            if (startPosition == null || endPosition == null)
            {
                return null;
            }

            return new SnapshotSpan(snapshot, Span.FromBounds(startPosition.Value, endPosition.Value));
        }

        public static SnapshotSpan GetFullSpan(this ITextSnapshot snapshot)
        {
            Contract.ThrowIfNull(snapshot);

            return new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
        }

        public static NormalizedSnapshotSpanCollection GetSnapshotSpanCollection(this ITextSnapshot snapshot)
        {
            Contract.ThrowIfNull(snapshot);

            return new NormalizedSnapshotSpanCollection(snapshot.GetFullSpan());
        }

        public static void GetLineAndColumn(this ITextSnapshot snapshot, int position, out int lineNumber, out int columnIndex)
        {
            var line = snapshot.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            columnIndex = position - line.Start.Position;
        }

        /// <summary>
        /// Returns the leading whitespace of the line located at the specified position in the given snapshot.
        /// </summary>
        public static string GetLeadingWhitespaceOfLineAtPosition(this ITextSnapshot snapshot, int position)
        {
            Contract.ThrowIfNull(snapshot);

            var line = snapshot.GetLineFromPosition(position);
            var linePosition = line.GetFirstNonWhitespacePosition();
            if (!linePosition.HasValue)
            {
                return line.GetText();
            }

            var lineText = line.GetText();
            return lineText.Substring(0, linePosition.Value - line.Start);
        }

        /// <summary>
        /// Get the character at the given position.
        /// </summary>
        public static char CharAt(this ITextSnapshot snapshot, int position)
        {
            return snapshot.GetText(position, 1)[0];
        }
    }
}
