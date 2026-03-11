// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Windows.Forms;
    using System.Windows.Media.Imaging;
    using PixelFormats = System.Windows.Media.PixelFormats;

    internal static class ScreenshotService
    {
        private static readonly object Gate = new object();

        /// <summary>
        /// Takes a picture of the screen and saves it to the location specified by
        /// <paramref name="fullPath"/>. Files are always saved in PNG format, regardless of the
        /// file extension.
        /// </summary>
        public static void TakeScreenshot(string fullPath)
        {
            // This gate prevents concurrency for two reasons:
            //
            // 1. Only one screenshot is held in memory at a time to prevent running out of memory for large displays
            // 2. Only one screenshot is written to disk at a time to avoid exceptions if concurrent calls are writing
            //    to the same file
            lock (Gate)
            {
                var bitmap = TryCaptureFullScreen();
                if (bitmap == null)
                {
                    return;
                }

                var directory = Path.GetDirectoryName(fullPath);
                Directory.CreateDirectory(directory);

                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }
            }
        }

        /// <summary>
        /// Captures the full screen to a <see cref="Bitmap"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="Bitmap"/> containing the screen capture of the desktop, or <see langword="null"/> if a screen
        /// capture can't be created.
        /// </returns>
        private static BitmapSource? TryCaptureFullScreen()
        {
            var width = Screen.PrimaryScreen.Bounds.Width;
            var height = Screen.PrimaryScreen.Bounds.Height;

            if (width <= 0 || height <= 0)
            {
                // Don't try to take a screenshot if there is no screen.
                // This may not be an interactive session.
                return null;
            }

            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    sourceX: Screen.PrimaryScreen.Bounds.X,
                    sourceY: Screen.PrimaryScreen.Bounds.Y,
                    destinationX: 0,
                    destinationY: 0,
                    blockRegionSize: bitmap.Size,
                    copyPixelOperation: CopyPixelOperation.SourceCopy);

                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);
                try
                {
                    return BitmapSource.Create(
                        bitmapData.Width,
                        bitmapData.Height,
                        bitmap.HorizontalResolution,
                        bitmap.VerticalResolution,
                        PixelFormats.Bgra32,
                        null,
                        bitmapData.Scan0,
                        bitmapData.Stride * bitmapData.Height,
                        bitmapData.Stride);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }
    }
}
