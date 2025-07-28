// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class SnapshotSpanExtensions
{
    extension(SnapshotSpan snapshotSpan)
    {
        public ITrackingSpan CreateTrackingSpan(SpanTrackingMode trackingMode)
        => snapshotSpan.Snapshot.CreateTrackingSpan(snapshotSpan.Span, trackingMode);

        public void GetLinesAndCharacters(
            out int startLineNumber,
            out int startCharacterIndex,
            out int endLineNumber,
            out int endCharacterIndex)
        {
            snapshotSpan.Snapshot.GetLineAndCharacter(snapshotSpan.Span.Start, out startLineNumber, out startCharacterIndex);
            snapshotSpan.Snapshot.GetLineAndCharacter(snapshotSpan.Span.End, out endLineNumber, out endCharacterIndex);
        }

        public LinePositionSpan ToLinePositionSpan()
        {
            snapshotSpan.GetLinesAndCharacters(out var startLine, out var startChar, out var endLine, out var endChar);
            return new LinePositionSpan(new LinePosition(startLine, startChar), new LinePosition(endLine, endChar));
        }

        public bool IntersectsWith(TextSpan textSpan)
            => snapshotSpan.IntersectsWith(textSpan.ToSpan());

        public bool IntersectsWith(int position)
            => snapshotSpan.Span.IntersectsWith(position);
    }
}
