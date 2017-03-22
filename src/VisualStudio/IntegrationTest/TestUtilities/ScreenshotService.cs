// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

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
                var directory = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directory);

                bitmap.Save(fullPath, ImageFormat.Png);
            }
        }

        /// <summary>
        /// Captures the full screen to a <see cref="Bitmap"/>.
        /// </summary>
        /// <returns>A <see cref="Bitmap"/> containing the screen capture of the desktop.</returns>
        private static Bitmap CaptureFullScreen()
        {
            var bitmap = new Bitmap(
                width: Screen.PrimaryScreen.Bounds.Width,
                height: Screen.PrimaryScreen.Bounds.Height);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    sourceX: Screen.PrimaryScreen.Bounds.X,
                    sourceY: Screen.PrimaryScreen.Bounds.Y,
                    destinationX: 0,
                    destinationY: 0,
                    blockRegionSize: bitmap.Size,
                    copyPixelOperation: CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }
    }
}
