﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Tags
{
    [ExportImageMonikerService(Name = Name), Shared]
    internal class DefaultImageMonikerService : IImageMonikerService
    {
        public const string Name = nameof(DefaultImageMonikerService);

        public bool TryGetImageMoniker(ImmutableArray<string> tags, out ImageMoniker imageMoniker)
        {
            var glyph = tags.GetGlyph();

            // We can't do the compositing of these glyphs at the editor layer.  So just map them
            // to the non-add versions.
            switch (glyph)
            {
                case Glyph.AddReference:
                    glyph = Glyph.Reference;
                    break;
            }

            imageMoniker = glyph.GetImageMoniker();
            return !imageMoniker.IsNullImage();
        }
    }
}
