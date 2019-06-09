// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Text.Shared.Extensions
{
    internal static class SnapshotPointExtensions
    {
        public static void Deconstruct(this SnapshotPoint snapshotPoint, out ITextSnapshot snapshot, out int position)
        {
            snapshot = snapshotPoint.Snapshot;
            position = snapshotPoint.Position;
        }
    }
}
