using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    [Export(typeof(IDeferredQuickInfoContentToFrameworkElementConverter))]
    class QuickInfoDisplayDeferredContentConverter : IDeferredQuickInfoContentToFrameworkElementConverter
    {
        public FrameworkElement CreateFrameworkElement(IDeferredQuickInfoContent deferredContent, DeferredContentFrameworkElementFactory factory)
        {
            var quickInfoDisplay = (QuickInfoDisplayDeferredContent)deferredContent;
            FrameworkElement warningGlyphElement = null;
            if (quickInfoDisplay.WarningGlyph != null)
            {
                warningGlyphElement = factory.CreateElement(quickInfoDisplay.WarningGlyph);
            }

            FrameworkElement symbolGlyphElement = null;
            if (quickInfoDisplay.SymbolGlyph != null)
            {
                symbolGlyphElement = factory.CreateElement(quickInfoDisplay.SymbolGlyph);
            }

            return new QuickInfoDisplayPanel(
                symbolGlyphElement,
                warningGlyphElement,
                factory.CreateElement(quickInfoDisplay.MainDescription),
                factory.CreateElement(quickInfoDisplay.Documentation),
                factory.CreateElement(quickInfoDisplay.TypeParameterMap),
                factory.CreateElement(quickInfoDisplay.AnonymousTypes),
                factory.CreateElement(quickInfoDisplay.UsageText),
                factory.CreateElement(quickInfoDisplay.ExceptionText));
        }

        public Type GetApplicableType()
        {
            return typeof(QuickInfoDisplayDeferredContent);
        }
    }
}
