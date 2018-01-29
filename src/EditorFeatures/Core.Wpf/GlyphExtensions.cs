// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Wpf
{
    internal static class GlyphExtensions
    {
        public static ImageMoniker GetImageMoniker(this Glyph glyph)
        {
            var imageId = glyph.GetImageId();
            return new ImageMoniker()
            {
                Guid = imageId.Guid,
                Id = imageId.Id
            };
        }
    }
}
