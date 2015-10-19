// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class ProjectionBufferExtensions
    {
        internal static SnapshotSpan GetSourceSpan(this IProjectionSnapshot snapshot, int index)
        {
            return snapshot.GetSourceSpans(index, 1)[0];
        }
    }
}