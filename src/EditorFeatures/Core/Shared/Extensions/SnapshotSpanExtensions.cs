// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class SnapshotSpanExtensions
    {
        public static ITrackingSpan CreateTrackingSpan(this SnapshotSpan snapshotSpan, SpanTrackingMode trackingMode)
        {
            return snapshotSpan.Snapshot.CreateTrackingSpan(snapshotSpan.Span, trackingMode);
        }

        public static void GetLinesAndCharacters(
            this SnapshotSpan snapshotSpan,
            out int startLineNumber,
            out int startCharacterIndex,
            out int endLineNumber,
            out int endCharacterIndex)
        {
            snapshotSpan.Snapshot.GetLineAndCharacter(snapshotSpan.Span.Start, out startLineNumber, out startCharacterIndex);
            snapshotSpan.Snapshot.GetLineAndCharacter(snapshotSpan.Span.End, out endLineNumber, out endCharacterIndex);
        }

        public static bool IntersectsWith(this SnapshotSpan snapshotSpan, TextSpan textSpan)
        {
            return snapshotSpan.IntersectsWith(textSpan.ToSpan());
        }

        public static bool IntersectsWith(this SnapshotSpan snapshotSpan, int position)
        {
            return snapshotSpan.Span.IntersectsWith(position);
        }
    }
}
