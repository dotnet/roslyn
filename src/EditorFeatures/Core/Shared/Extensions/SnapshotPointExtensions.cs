// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class SnapshotPointExtensions
    {
        public static void GetLineAndCharacter(this SnapshotPoint point, out int lineNumber, out int characterIndex)
            => point.Snapshot.GetLineAndCharacter(point.Position, out lineNumber, out characterIndex);

        public static int GetContainingLineNumber(this SnapshotPoint point)
            => point.GetContainingLineNumber();

        public static ITrackingPoint CreateTrackingPoint(this SnapshotPoint point, PointTrackingMode trackingMode)
            => point.Snapshot.CreateTrackingPoint(point, trackingMode);
    }
}
