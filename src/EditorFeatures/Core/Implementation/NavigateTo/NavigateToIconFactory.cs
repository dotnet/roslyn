// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal class NavigateToIconFactory : IDisposable
    {
        private readonly IGlyphService _glyphService;
        private readonly Dictionary<Glyph, Icon> _iconCache = new Dictionary<Glyph, Icon>();

        public NavigateToIconFactory(IGlyphService glyphService)
        {
            Contract.ThrowIfNull(glyphService);

            _glyphService = glyphService;
        }

        private static Icon ConvertBitmapSourceToIcon(BitmapSource bmpSource)
        {
            if (bmpSource == null)
            {
                throw new ArgumentNullException(nameof(bmpSource));
            }

            // We shall convert this BitmapSource to an icon. Sadly, there's no direct API to do this. The approach
            // that was previously used here was to save the image as a PNG and then reload that into a GDI+ Bitmap.
            // From there, we would call GetHICON(), but this conversion function wouldn't properly handle partial
            // transparency pixels. As it turns out, a semi-transparent icon file is nothing more than a header +
            // the PNG data, so we can avoid the buggy GDI+ conversion code by simply writing out a .ico file into
            // memory and then loading that back in.

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmpSource));

            using (var icoStream = SerializableBytes.CreateWritableStream())
            {
                long offsetOfSize;
                long offsetOfPng;

                using (var binaryWriter = new BinaryWriter(icoStream, Encoding.UTF8, leaveOpen: true))
                {
                    binaryWriter.Write((short)0); // ICONDIR: Reserved, must be zero
                    binaryWriter.Write((short)1); // ICONDIR: Image type (1 = icon)
                    binaryWriter.Write((short)1); // ICONDIR: Number of images in icon file

                    // ICONDIRENTRY: Dimensions
                    binaryWriter.Write((byte)bmpSource.PixelWidth);
                    binaryWriter.Write((byte)bmpSource.PixelHeight);

                    binaryWriter.Write((byte)0);  // ICONDIRENTRY: Palette (none)
                    binaryWriter.Write((byte)0);  // ICONDIRENTRY: Reserved
                    binaryWriter.Write((short)0); // ICONDIRENTRY: Color planes (zero since PNG)
                    binaryWriter.Write((short)0); // ICONDIRENTRY: Bits per pixel (zero since PNG)
                    binaryWriter.Flush();

                    // ICONDIRENTRY: Size. We'll come back to this one once we know what it actually is.
                    offsetOfSize = icoStream.Position;
                    binaryWriter.Write((int)0);

                    // ICONDIRENTRY: offset
                    offsetOfPng = icoStream.Length + sizeof(int);
                    binaryWriter.Write((int)offsetOfPng);
                }

                encoder.Save(icoStream);

                // Now that we have the total size, go back and write it
                icoStream.Position = offsetOfSize;

                using (var binaryWriter = new BinaryWriter(icoStream, Encoding.UTF8, leaveOpen: true))
                {
                    binaryWriter.Write((int)(icoStream.Length - offsetOfPng));
                }

                icoStream.Position = 0;
                return new Icon(icoStream);
            }
        }

        public Icon GetIcon(Glyph glyph)
        {
            Icon value;
            if (!_iconCache.TryGetValue(glyph, out value))
            {
                var bitmapSource = glyph.GetImageSource(_glyphService) as BitmapSource;
                if (bitmapSource != null)
                {
                    value = ConvertBitmapSourceToIcon(bitmapSource);
                    _iconCache.Add(glyph, value);
                }
                else
                {
                    value = null;
                }
            }

            return value;
        }

        public void Dispose()
        {
            foreach (var icon in _iconCache.Values)
            {
                icon.Dispose();
            }

            _iconCache.Clear();
        }
    }
}
