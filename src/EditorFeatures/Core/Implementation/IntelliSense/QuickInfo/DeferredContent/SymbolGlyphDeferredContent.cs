// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class SymbolGlyphDeferredContent : IDeferredQuickInfoContent
    {
        private readonly IGlyphService _glyphService;
        private readonly Glyph _glyph;

        public SymbolGlyphDeferredContent(Glyph glyph, IGlyphService glyphService)
        {
            Contract.ThrowIfNull(glyphService);

            _glyph = glyph;
            _glyphService = glyphService;
        }

        public FrameworkElement Create()
        {
            var image = new CrispImage
            {
                Moniker = _glyph.GetImageMoniker(),
            };

            // Inform the ImageService of the background color so that images have the correct background.
            var binding = new Binding("Background")
            {
                Converter = new BrushToColorConverter(),
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(QuickInfoDisplayPanel), 1)
            };

            image.SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, binding);
            return image;
        }

        // For Testing.
        internal Glyph Glyph
        {
            get { return _glyph; }
        }
    }
}
