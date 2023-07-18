// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private static class RegistryItemConverter
        {
            private const uint FullyOpaqueAlpha = 0xFF000000;
            private const uint RegistryVersion = 0x0B;

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
                //|-------------Header----------------|--Categories...
                //|-Byte Count|--Version--|CategoryCnt|--
                //:ca,05,00,00,0b,00,00,00,01,00,00,00,--

                // Initialize with a generous initial capacity.
                var bytes = new MemoryStream(4096);

                // Reserve space to write the total length.
                WriteDWord(bytes, 0);

                // Write the Version into the header.
                WriteDWord(bytes, RegistryVersion);

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
                // |------------------------Category---------------------------|--Items...
                // |----------------Category GUID------------------|-Item Count|--
                // ,85,56,a0,75,a8,00,ed,4d,ba,e5,e7,a5,0b,fa,92,9a,2e,00,00,00,--

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

                bytes.Write(Encoding.ASCII.GetBytes(color.Name), 0, color.Name.Length);

                bytes.WriteByte((byte)color.BackgroundType);

                if (color.Background.HasValue)
                {
                    var background = color.Background.Value;

                    // Add back a fully opaque alpha value for RGB colors
                    if (color.BackgroundType == __VSCOLORTYPE.CT_RAW)
                    {
                        background |= FullyOpaqueAlpha;
                    }

                    WriteDWord(bytes, background);
                }

                bytes.WriteByte((byte)color.ForegroundType);

                if (color.Foreground.HasValue)
                {
                    var foreground = color.Foreground.Value;

                    // Add back a fully opaque alpha value for RGB colors
                    if (color.ForegroundType == __VSCOLORTYPE.CT_RAW)
                    {
                        foreground |= FullyOpaqueAlpha;
                    }

                    WriteDWord(bytes, foreground);
                }

                return;
            }
        }
    }
}
