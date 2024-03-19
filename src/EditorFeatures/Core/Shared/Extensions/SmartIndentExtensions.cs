// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class IndentationResultExtensions
{
    public static int GetIndentation(this Indentation.IndentationResult result, ITextView textView, ITextSnapshotLine lineToBeIndented)
    {
        var position = new SnapshotPoint(lineToBeIndented.Snapshot, result.BasePosition);
        var pointInSurfaceSnapshot = textView.BufferGraph.MapUpToSnapshot(position, PointTrackingMode.Positive, PositionAffinity.Successor, textView.TextSnapshot);
        if (!pointInSurfaceSnapshot.HasValue)
        {
            return position.GetContainingLine().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(textView.Options);
        }

        var lineInSurfaceSnapshot = pointInSurfaceSnapshot.Value.Snapshot.GetLineFromPosition(pointInSurfaceSnapshot.Value.Position);
        var offsetInLine = pointInSurfaceSnapshot.Value.Position - lineInSurfaceSnapshot.Start.Position;
        return lineInSurfaceSnapshot.GetColumnFromLineOffset(offsetInLine, textView.Options) + result.Offset;
    }
}
