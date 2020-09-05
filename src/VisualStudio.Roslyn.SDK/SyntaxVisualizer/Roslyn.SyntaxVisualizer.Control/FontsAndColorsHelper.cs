using System;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.SyntaxVisualizer.Control
{
    public static class FontsAndColorsHelper
    {
        private static readonly Guid TextEditorMEFItemsColorCategory = new Guid("75a05685-00a8-4ded-bae5-e7a50bfa929a");

        public static Color? GetColorForClassification(ClassifiedSpan classifiedSpan)
        {
            if (string.IsNullOrEmpty(classifiedSpan.ClassificationType))
            {
                return null;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            var fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();

            if (fontsAndColorStorage is null)
            {
                return null;
            }

            // Open Text Editor category for readonly access.
            if (fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return null;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (fontsAndColorStorage.GetItem(classifiedSpan.ClassificationType, colorItems) != VSConstants.S_OK)
                {
                    return null;
                }

                var colorItem = colorItems[0];

                var fontAndColorUtilities = (IVsFontAndColorUtilities)fontsAndColorStorage;
                var foregroundColorRef = colorItem.crForeground;

                if (fontAndColorUtilities.GetColorType(foregroundColorRef, out var foregroundColorType) != VSConstants.S_OK)
                {
                    // Without being able to check color type, we cannot make a determination.
                    return null;
                }

                // If the color is not CT_RAW then foregroundColorRef doesn't
                // regpresent the RGB values for the color and we can't interpret them.
                if (foregroundColorType != (int)__VSCOLORTYPE.CT_RAW)
                {
                    return null;
                }

                var colorBytes = BitConverter.GetBytes(foregroundColorRef);
                return Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]);
            }
            finally
            {
                fontsAndColorStorage.CloseCategory();
            }
        }

        internal static void UpdateClassificationColor(ClassifiedSpan classifiedSpan, Color selectedColor)
        {
            if (string.IsNullOrEmpty(classifiedSpan.ClassificationType))
            {
                return;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            var fontsAndColorStorage = ServiceProvider.GlobalProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();

            if (fontsAndColorStorage is null)
            {
                return;
            }

            // Open Text Editor to make changes. Make sure LOADDEFAULTS is passed so any default 
            // values can be modified as well.
            if (fontsAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)(__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS)) != VSConstants.S_OK)
            {
                // We were unable to access color information.
                return;
            }

            try
            {
                var colorItems = new ColorableItemInfo[1];
                if (fontsAndColorStorage.GetItem(classifiedSpan.ClassificationType, colorItems) != VSConstants.S_OK)
                {
                    return;
                }

                var colorItem = colorItems[0];

                colorItem.crForeground = BitConverter.ToUInt32(
                    new byte[] {
                        selectedColor.R, 
                        selectedColor.G, 
                        selectedColor.B,  
                        0 // Alpha
                    }, 0);

                if (fontsAndColorStorage.SetItem(classifiedSpan.ClassificationType, new[] { colorItem }) != VSConstants.S_OK)
                {
                    throw new Exception();
                }
            }
            finally
            {
                fontsAndColorStorage.CloseCategory();
            }
        }
    }
}
