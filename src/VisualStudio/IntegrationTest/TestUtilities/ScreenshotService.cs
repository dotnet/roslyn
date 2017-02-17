// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    internal class ScreenshotService
    {
        /// <summary>
        /// Takes a picture of the screen and saves it to the location specified by
        /// <paramref name="fullPath"/>. Files are always saved in PNG format, regardless of the
        /// file extension.
        /// </summary>
        public static void TakeScreenshot(string fullPath)
        {
            using (var bitmap = CaptureFullScreen())
            {
                bitmap.Save(fullPath, ImageFormat.Png);
            }
        }

        /// <summary>
        /// Captures the full screen including the mouse cursor.
        /// </summary>
        /// <returns>A <see cref="Bitmap"/> containing the screen capture of the desktop.</returns>
        private static Bitmap CaptureFullScreen()
        {
            IntPtr desktop = IntPtr.Zero;
            IntPtr srcDC = IntPtr.Zero;
            IntPtr destDC = IntPtr.Zero;
            IntPtr bmp = IntPtr.Zero;

            try
            {
                Size size = Screen.PrimaryScreen.Bounds.Size;
                desktop = NativeMethods.GetDesktopWindow();
                srcDC = NativeMethods.GetWindowDC(desktop);
                destDC = NativeMethods.CreateCompatibleDC(srcDC);
                bmp = NativeMethods.CreateCompatibleBitmap(srcDC, size.Width, size.Height);
                var oldBmp = NativeMethods.SelectObject(destDC, bmp);
                var b = NativeMethods.BitBlt(destDC, 0, 0, size.Width, size.Height, srcDC, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                var bitmap = Bitmap.FromHbitmap(bmp);
                NativeMethods.SelectObject(destDC, oldBmp);

                return bitmap;
            }
            finally
            {
                if (bmp != IntPtr.Zero)
                {
                    NativeMethods.DeleteObject(bmp);
                }

                if (destDC != IntPtr.Zero)
                {
                    NativeMethods.DeleteDC(destDC);
                }

                if (srcDC != IntPtr.Zero)
                {
                    NativeMethods.ReleaseDC(desktop, srcDC);
                }
            }
        }
    }
}
