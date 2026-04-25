// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public static class TagHelperDescriptorExtensions
{
    extension(TagHelperDescriptor tagHelper)
    {
        /// <summary>
        /// Indicates whether a <see cref="TagHelperDescriptor"/> represents a view component.
        /// </summary>
        /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/> to check.</param>
        /// <returns>Whether a <see cref="TagHelperDescriptor"/> represents a view component.</returns>
        public bool IsViewComponentKind
            => tagHelper.Kind == TagHelperKind.ViewComponent;

        public string? ViewComponentName
            => tagHelper.Metadata is ViewComponentMetadata { Name: var result }
                ? result
                : null;
    }
}
