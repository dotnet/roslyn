// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    internal static class CrispImageCache
    {
        private static ImageAttributes? s_attributes;
        private static readonly Dictionary<ImageMoniker, BitmapSource> s_ImageCache = new();
        private static object s_updateLock = new();

        public static BitmapSource GetBitmapSource(ImageMoniker imageMoniker, ImageAttributes currentAttributes)
        {
            lock (s_updateLock)
            {
                if (!currentAttributes.Equals(s_attributes))
                {
                    s_attributes = currentAttributes;
                    var imageLibrary = CrispImage.DefaultImageLibrary;
                    s_ImageCache[KnownMonikers.Implementing] = (BitmapSource)imageLibrary.GetImage(KnownMonikers.Implementing, currentAttributes);
                    s_ImageCache[KnownMonikers.Implemented] = (BitmapSource)imageLibrary.GetImage(KnownMonikers.Implemented, currentAttributes);
                    s_ImageCache[KnownMonikers.Overridden] = (BitmapSource)imageLibrary.GetImage(KnownMonikers.Overridden, currentAttributes);
                    s_ImageCache[KnownMonikers.Overriding] = (BitmapSource)imageLibrary.GetImage(KnownMonikers.Overriding, currentAttributes);
                    s_ImageCache[KnownMonikers.ImplementingOverridden] = (BitmapSource)imageLibrary.GetImage(KnownMonikers.ImplementingOverridden, currentAttributes);
                    s_ImageCache[KnownMonikers.ImplementingOverriding] = (BitmapSource)imageLibrary.GetImage(KnownMonikers.ImplementingOverriding, currentAttributes);
                }

                if (s_ImageCache.TryGetValue(imageMoniker, out var bitmapSource))
                {
                    return bitmapSource;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(imageMoniker);
        }
    }
}
