// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Roslyn.Core.Imaging;
using Roslyn.Text.Adornments;

namespace Microsoft.CodeAnalysis.Razor.Completion.Html;

/// <summary>
/// Image monikers for icons in HTML completion lists.
/// </summary>
internal static class HtmlCompletionImageMonikers
{
    private static readonly Guid s_razorCompletionImagesGuid = new("{77A8B215-415E-495B-BB56-3F34D1105D51}");
    private static readonly Guid s_imageCatalogGuid = new("{ae27a6b0-e345-4288-96df-5eaf394ee369}");

    /// <summary>
    /// Angular directive element icon (e.g., ng-form, ng-include).
    /// </summary>
    internal static readonly ImageElement Angular = new(
        new ImageId(s_razorCompletionImagesGuid, 1),
        "Angular");

    /// <summary>
    /// ARIA accessibility attribute icon (e.g., aria-label, aria-hidden).
    /// Uses KnownImageIds.Accessibility (8) from the VS Image Catalog.
    /// </summary>
    internal static readonly ImageElement AriaAttribute = new(
        new ImageId(s_imageCatalogGuid, 8),
        "AriaAttribute");

    /// <summary>
    /// Custom data attribute group icon (data-…).
    /// Uses KnownImageIds.MethodPublic (1880).
    /// </summary>
    internal static readonly ImageElement DataAttribute = new(
        new ImageId(s_imageCatalogGuid, 1880),
        "DataAttribute");
}
