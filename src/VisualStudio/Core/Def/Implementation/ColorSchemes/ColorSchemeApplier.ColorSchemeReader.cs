// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private static class ColorSchemeReader
        {
            public static ColorScheme ReadColorScheme(Stream schemeStream)
            {
                var schemeDocument = XDocument.Load(schemeStream);

                var themes = schemeDocument
                    .Descendants("Theme")
                    .Select(ReadColorTheme);

                return new ColorScheme(themes.ToImmutableArray());
            }

            private static ColorTheme ReadColorTheme(XElement themeElement)
            {
                var themeName = (string)themeElement.Attribute("Name");
                var themeGuid = Guid.Parse((string)themeElement.Attribute("GUID"));

                var categoryElement = themeElement.Descendants("Category").Single();
                var category = ReadColorCategory(categoryElement);

                return new ColorTheme(themeName, themeGuid, category);
            }

            private static ColorCategory ReadColorCategory(XElement categoryElement)
            {
                var categoryName = (string)categoryElement.Attribute("Name");
                var categoryGuid = Guid.Parse((string)categoryElement.Attribute("GUID"));

                var colorItems = categoryElement
                    .Descendants("Color")
                    .Select(ReadColorItem)
                    .OfType<ColorItem>();

                return new ColorCategory(categoryName, categoryGuid, colorItems.ToImmutableArray());
            }

            private static ColorItem? ReadColorItem(XElement colorElement)
            {
                var name = (string)colorElement.Attribute("Name");

                var backgroundElement = colorElement.Descendants("Background").SingleOrDefault();
                (var backgroundType, var backgroundColor) = ReadColor(backgroundElement);

                var foregroundElement = colorElement.Descendants("Foreground").SingleOrDefault();
                (var foregroundType, var foregroundColor) = ReadColor(foregroundElement);

                if (backgroundElement is null && foregroundElement is null)
                {
                    return null;
                }

                return new ColorItem(name, backgroundType, backgroundColor, foregroundType, foregroundColor);
            }

            private static (int Type, uint? Color) ReadColor(XElement colorElement)
            {
                if (colorElement is null)
                {
                    return (0, null);
                }

                var colorType = (string)colorElement.Attribute("Type");
                var sourceColor = (string)colorElement.Attribute("Source");

                int type;
                uint? color;

                if (colorType == "CT_RAW")
                {
                    type = (int)__VSCOLORTYPE.CT_RAW;

                    // The ColorableItemInfo returned by the FontAndColorStorage retuns RGB color information as 0x00BBGGRR.
                    // Optimize for color comparisons by converting ARGB to BGR by ignoring the alpha channel and reversing byte order.
                    var r = sourceColor.Substring(2, 2);
                    var g = sourceColor.Substring(4, 2);
                    var b = sourceColor.Substring(6, 2);
                    color = uint.Parse($"{b}{g}{r}", NumberStyles.HexNumber);
                }
                else if (colorType == "CT_SYSCOLOR")
                {
                    type = (int)__VSCOLORTYPE.CT_SYSCOLOR;

                    color = uint.Parse(sourceColor, NumberStyles.HexNumber);
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(colorType);
                }

                return (type, color);
            }
        }
    }
}
