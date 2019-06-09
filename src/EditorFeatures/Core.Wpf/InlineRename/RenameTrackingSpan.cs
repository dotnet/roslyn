// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal struct RenameTrackingSpan
    {
        public readonly ITrackingSpan TrackingSpan;
        public readonly RenameSpanKind Type;

        public RenameTrackingSpan(ITrackingSpan trackingSpan, RenameSpanKind type)
        {
            this.TrackingSpan = trackingSpan;
            this.Type = type;
        }
    }

    internal enum RenameSpanKind
    {
        None,
        Reference,
        UnresolvedConflict,
        Complexified
    }
}
