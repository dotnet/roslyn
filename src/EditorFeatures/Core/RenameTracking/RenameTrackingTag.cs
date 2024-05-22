// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

internal class RenameTrackingTag : TextMarkerTag
{
    internal const string TagId = "RenameTrackingTag";

    public static readonly RenameTrackingTag Instance = new();

    private RenameTrackingTag()
        : base(TagId)
    {
    }
}
