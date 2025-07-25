// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Text.Shared.Extensions;

internal static partial class ITextSnapshotExtensions
{
    extension(ITextSnapshot snapshot)
    {
        public SnapshotPoint GetPoint(int position)
        => new SnapshotPoint(snapshot, position);

        public SnapshotPoint? TryGetPoint(int lineNumber, int columnIndex)
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
        public TextSpan GetTextSpan(LinePositionSpan span)
        {
            return TextSpan.FromBounds(
                GetPosition(snapshot, span.Start.Line, span.Start.Character),
                GetPosition(snapshot, span.End.Line, span.End.Character));
        }

        public int GetPosition(int lineNumber, int columnIndex)
            => TryGetPosition(snapshot, lineNumber, columnIndex) ?? throw new InvalidOperationException(TextEditorResources.The_snapshot_does_not_contain_the_specified_position);

        public int? TryGetPosition(int lineNumber, int columnIndex)
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

        public bool TryGetPosition(int lineNumber, int columnIndex, out SnapshotPoint position)
        {
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

            var result = line.Start.Position + columnIndex;
            position = new SnapshotPoint(snapshot, result);
            return true;
        }

        public SnapshotSpan GetSpan(int start, int length)
            => new SnapshotSpan(snapshot, new Span(start, length));

        public SnapshotSpan GetSpanFromBounds(int start, int end)
            => new SnapshotSpan(snapshot, Span.FromBounds(start, end));

        public SnapshotSpan GetSpan(Span span)
            => new SnapshotSpan(snapshot, span);

        public TagSpan<TTag> GetTagSpan<TTag>(Span span, TTag tag) where TTag : ITag
            => new(new SnapshotSpan(snapshot, span), tag);

        public SnapshotSpan GetSpan(int startLine, int startIndex, int endLine, int endIndex)
            => TryGetSpan(snapshot, startLine, startIndex, endLine, endIndex) ?? throw new InvalidOperationException(TextEditorResources.The_snapshot_does_not_contain_the_specified_span);

        public SnapshotSpan? TryGetSpan(int startLine, int startIndex, int endLine, int endIndex)
        {
            var startPosition = snapshot.TryGetPosition(startLine, startIndex);
            var endPosition = snapshot.TryGetPosition(endLine, endIndex);
            if (startPosition == null || endPosition == null)
            {
                return null;
            }

            return new SnapshotSpan(snapshot, Span.FromBounds(startPosition.Value, endPosition.Value));
        }

        public SnapshotSpan GetFullSpan()
        {
            Contract.ThrowIfNull(snapshot);

            return new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
        }

        public NormalizedSnapshotSpanCollection GetSnapshotSpanCollection()
        {
            Contract.ThrowIfNull(snapshot);

            return new NormalizedSnapshotSpanCollection(snapshot.GetFullSpan());
        }

        public void GetLineAndCharacter(int position, out int lineNumber, out int characterIndex)
        {
            var line = snapshot.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            characterIndex = position - line.Start.Position;
        }

        /// <summary>
        /// Returns the leading whitespace of the line located at the specified position in the given snapshot.
        /// </summary>
        public string GetLeadingWhitespaceOfLineAtPosition(int position)
        {
            Contract.ThrowIfNull(snapshot);

            var line = snapshot.GetLineFromPosition(position);
            var linePosition = line.GetFirstNonWhitespacePosition();
            if (!linePosition.HasValue)
            {
                return line.GetText();
            }

            var lineText = line.GetText();
            return lineText[..(linePosition.Value - line.Start)];
        }

        public bool AreOnSameLine(int x1, int x2)
            => snapshot.GetLineNumberFromPosition(x1) == snapshot.GetLineNumberFromPosition(x2);
    }
}
