// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal sealed record BoundElementDescriptionInfo(string TagHelperTypeName, string? Documentation = null)
{
    public static BoundElementDescriptionInfo From(TagHelperDescriptor tagHelper)
    {
        var tagHelperTypeName = tagHelper.TypeName;

        return new BoundElementDescriptionInfo(tagHelperTypeName, tagHelper.Documentation);
    }
}
