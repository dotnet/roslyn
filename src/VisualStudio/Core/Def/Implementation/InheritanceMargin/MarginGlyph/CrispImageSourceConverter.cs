// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    internal class CrispImageSourceConverter : MultiValueConverter<ImageMoniker, double, double, double, Color, ImageSource>
    {
        protected override ImageSource Convert(
            ImageMoniker imageMoniker,
            double height,
            double width,
            double dpi,
            Color background,
            object parameter,
            CultureInfo culture)
        {
            var attributes = new ImageAttributes
            {
                StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
                Flags = unchecked((uint)(_ImageAttributesFlags.IAF_Size | _ImageAttributesFlags.IAF_Dpi | _ImageAttributesFlags.IAF_Type | _ImageAttributesFlags.IAF_Format | _ImageAttributesFlags.IAF_Background)),
                LogicalHeight = (int)height,
                LogicalWidth = (int)width,
                Dpi = (int)dpi,
                Format = (int)_UIDataFormat.DF_WPF,
                ImageType = (int)_UIImageType.IT_Bitmap,
                Background = background.ToRgba(),
            };

            return CrispImageCache.GetBitmapSource(imageMoniker, attributes);
        }
    }
}
