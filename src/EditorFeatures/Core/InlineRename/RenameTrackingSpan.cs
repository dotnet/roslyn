// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal readonly struct RenameTrackingSpan(ITrackingSpan trackingSpan, RenameSpanKind type)
{
    public readonly ITrackingSpan TrackingSpan = trackingSpan;
    public readonly RenameSpanKind Type = type;
}

internal enum RenameSpanKind
{
    None,
    Reference,
    UnresolvedConflict,
    Complexified
}
