// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text.Shared.Extensions
{
    internal static partial class ITextSnapshotExtensions
    {
        public static SnapshotPoint GetPoint(this ITextSnapshot snapshot, int position)
            => new SnapshotPoint(snapshot, position);

        public static SnapshotPoint? TryGetPoint(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            var position = snapshot.TryGetPosition(lineNumber, columnIndex);
            if (position.HasValue)
            {
                return new SnapshotPoint(snapshot, position.Value);
            }
            else
            {
                return null;
            }
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
            => TryGetPosition(snapshot, lineNumber, columnIndex).Value;

        public static int? TryGetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return null;
            }

            var end = snapshot.GetLineFromLineNumber(lineNumber).Start.Position + columnIndex;
            if (end < 0 || end > snapshot.Length)
            {
                return null;
            }

            return end;
        }

        public static bool TryGetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex, out SnapshotPoint position)
        {
            var result = 0;
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
            => new SnapshotSpan(snapshot, new Span(start, length));

        public static SnapshotSpan GetSpanFromBounds(this ITextSnapshot snapshot, int start, int end)
            => new SnapshotSpan(snapshot, Span.FromBounds(start, end));

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, Span span)
            => new SnapshotSpan(snapshot, span);

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

        public static void GetLineAndCharacter(this ITextSnapshot snapshot, int position, out int lineNumber, out int characterIndex)
        {
            var line = snapshot.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            characterIndex = position - line.Start.Position;
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

        public static bool AreOnSameLine(this ITextSnapshot snapshot, int x1, int x2)
            => snapshot.GetLineNumberFromPosition(x1) == snapshot.GetLineNumberFromPosition(x2);
    }
}
