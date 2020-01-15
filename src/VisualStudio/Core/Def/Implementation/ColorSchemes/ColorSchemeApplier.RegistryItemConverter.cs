// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private static class RegistryItemConverter
        {
            public static ImmutableArray<RegistryItem> Convert(ColorScheme scheme)
            {
                return scheme.Themes
                    .Select(ToRegistryItem)
                    .ToImmutableArray();
            }

            private static RegistryItem ToRegistryItem(ColorTheme theme)
            {
                var sectionName = $"Themes\\{theme.Guid:B}\\{theme.Category.Name}";
                var data = ToData(theme);

                return new RegistryItem(sectionName, data);
            }

            private static byte[] ToData(ColorTheme theme)
            {
                //   |-------------Header----------------|------------------------Category---------------------------|----------------------------Item:Preprocessor Text------------------------------|-------------------------Item:Punctuation---------------------|
                //   |-Byte Count|--Version--|CategoryCnt|----------------Category GUID------------------|-Item Count|Name Length|---------------------Name-------------------------|CT|CT|-ColorRef--|Name Length|-------------Name---------------|CT|CT|-ColorRef--|
                //hex:ca,05,00,00,0b,00,00,00,01,00,00,00,85,56,a0,75,a8,00,ed,4d,ba,e5,e7,a5,0b,fa,92,9a,2e,00,00,00,11,00,00,00,70,72,65,70,72,6f,63,65,73,73,6f,72,20,74,65,78,74,00,01,00,00,00,ff,0b,00,00,00,70,75,6e,63,74,75,61,74,69,6f,6e,00,01,00,00,00,ff,

                // Initialize with a generous initial capacity.
                var bytes = new MemoryStream(4096);

                // Reserve space to write the total length.
                WriteDWord(bytes, 0);

                // Write the Version into the header.
                WriteDWord(bytes, 11);

                // Write the category count into the header.
                WriteDWord(bytes, 1);

                WriteCategory(bytes, theme.Category);

                // Write the data length into the space that we reserved.
                bytes.Seek(0, SeekOrigin.Begin);
                WriteDWord(bytes, (uint)bytes.Length);

                return bytes.ToArray();
            }

            private static void WriteDWord(MemoryStream bytes, uint dword)
            {
                bytes.WriteByte((byte)dword);
                bytes.WriteByte((byte)(dword >> 8));
                bytes.WriteByte((byte)(dword >> 16));
                bytes.WriteByte((byte)(dword >> 24));
            }

            private static void WriteCategory(MemoryStream bytes, ColorCategory category)
            {
                // |------------------------Category---------------------------|
                // |----------------Category GUID------------------|-Item Count|
                // ,85,56,a0,75,a8,00,ed,4d,ba,e5,e7,a5,0b,fa,92,9a,2e,00,00,00,

                WriteGuid(bytes, category.Guid);

                WriteDWord(bytes, (uint)category.Colors.Length);

                foreach (var color in category.Colors)
                {
                    WriteColor(bytes, color);
                }

                return;

                static void WriteGuid(MemoryStream bytes, Guid guid)
                {
                    bytes.Write(guid.ToByteArray(), 0, 16);
                }
            }

            private static void WriteColor(MemoryStream bytes, ColorItem color)
            {
                // |-------------------------Item:Punctuation---------------------|
                // |Name Length|-------------Name---------------|CT|CT|-ColorRef--|
                // ,0b,00,00,00,70,75,6e,63,74,75,61,74,69,6f,6e,00,01,00,00,00,ff,

                WriteDWord(bytes, (uint)color.Name.Length);

                WriteString(bytes, color.Name);

                bytes.WriteByte((byte)color.BackgroundType);

                if (color.Background.HasValue)
                {
                    var background = color.Background.Value;

                    // Add back a fully opaque alpha value for RGB colors
                    if (color.BackgroundType == (int)__VSCOLORTYPE.CT_RAW)
                    {
                        background |= 0xFF000000;
                    }

                    WriteDWord(bytes, background);
                }

                bytes.WriteByte((byte)color.ForegroundType);

                if (color.Foreground.HasValue)
                {
                    var foreground = color.Foreground.Value;

                    // Add back a fully opaque alpha value for RGB colors
                    if (color.ForegroundType == (int)__VSCOLORTYPE.CT_RAW)
                    {
                        foreground |= 0xFF000000;
                    }

                    WriteDWord(bytes, foreground);
                }

                return;

                static void WriteString(MemoryStream bytes, string ascii)
                {
                    bytes.Write(Encoding.ASCII.GetBytes(ascii), 0, ascii.Length);
                }
            }
        }
    }
}
