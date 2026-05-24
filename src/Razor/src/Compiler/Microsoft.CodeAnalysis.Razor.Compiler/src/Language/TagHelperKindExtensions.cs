// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public static class TagHelperKindExtensions
{
    extension(TagHelperKind kind)
    {
        public bool IsAnyComponentKind
            => kind is >= TagHelperKind.Component and <= TagHelperKind.RenderMode;

        public bool IsComponentOrChildContentKind
            => kind is TagHelperKind.Component or TagHelperKind.ChildContent;
    }
}
