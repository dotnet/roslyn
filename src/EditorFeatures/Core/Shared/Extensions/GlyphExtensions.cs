// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class GlyphExtensions
{
    // hardcode ImageCatalogGuid locally rather than calling KnownImageIds.ImageCatalogGuid
    // So it does not have dependency for Microsoft.VisualStudio.ImageCatalog.dll
    // https://github.com/dotnet/roslyn/issues/26642
    private static readonly Guid s_imageCatalogGuid = Guid.Parse("ae27a6b0-e345-4288-96df-5eaf394ee369");

    public static ImageId GetImageCatalogImageId(int imageId)
        => new(s_imageCatalogGuid, imageId);

    public static ImageId GetImageId(this Glyph glyph)
    {
        var (guid, id) = glyph.GetVsImageData();

        return new(guid, id);
    }

    public static ImageElement GetImageElement(this Glyph glyph)
        => new(glyph.GetImageId());
}
