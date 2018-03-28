// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class SnapshotPointExtensions
    {
        public static void GetLineAndColumn(this SnapshotPoint point, out int lineNumber, out int columnIndex)
        {
            point.Snapshot.GetLineAndColumn(point.Position, out lineNumber, out columnIndex);
        }

        public static int GetContainingLineNumber(this SnapshotPoint point)
        {
            return point.GetContainingLine().LineNumber;
        }

        public static ITrackingPoint CreateTrackingPoint(this SnapshotPoint point, PointTrackingMode trackingMode)
        {
            return point.Snapshot.CreateTrackingPoint(point, trackingMode);
        }
    }
}
