// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Tags
{
    [ExportImageMonikerService(Name = Name)]
    internal class DefaultImageMonikerService : IImageMonikerService
    {
        public const string Name = nameof(DefaultImageMonikerService);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultImageMonikerService()
        {
        }

        public bool TryGetImageMoniker(ImmutableArray<string> tags, out ImageMoniker imageMoniker)
        {
            var glyph = tags.GetFirstGlyph();

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
