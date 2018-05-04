using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    [QuickInfoConverterMetadata(typeof(SymbolGlyphDeferredContent))]
    class SymbolGlyphDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var symbolDeferredContent = (SymbolGlyphDeferredContent)deferredContent;

            var image = new CrispImage
            {
                Moniker = symbolDeferredContent.Glyph.GetImageMoniker(),
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

        public Type GetApplicableType()
        {
            return typeof(SymbolGlyphDeferredContent);
        }
    }
}
