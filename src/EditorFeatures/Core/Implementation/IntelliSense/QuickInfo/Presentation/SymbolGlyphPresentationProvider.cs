// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Roslyn.Utilities;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Text;
using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    [ExportQuickInfoPresentationProvider(QuickInfoElementKinds.Symbol, QuickInfoElementKinds.Warning)]
    internal class SymbolGlyphPresentationProvider : QuickInfoPresentationProvider
    {
        [ImportingConstructor]
        public SymbolGlyphPresentationProvider()
        {
        }

        public override FrameworkElement CreatePresentation(QuickInfoElement element, ITextSnapshot snapshot)
        {
            var image = new CrispImage
            {
                Moniker = element.Tags.GetGlyph().GetImageMoniker()
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
    }
}
