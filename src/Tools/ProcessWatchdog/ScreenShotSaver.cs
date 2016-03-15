// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ProcessWatchdog

{
    public static class ScreenshotSaver
    {
        public static void SaveScreen(string description, string outputFolder)
        {
            try
            {
                var fileName = GenerateScreenshotFileName(description, outputFolder);
                ConsoleUtils.LogMessage(Resources.InfoSavedScreenshot, fileName);
                SaveScreenToFile(fileName);
            }
            catch (Win32Exception ex)
            {
                // System.ComponentModel.Win32Exception (0x80004005): The handle is invalid. This
                // means we're not running in a console session, hence there's no UI to take a
                // screenshot of. This is perfectly normal on the server.
                ConsoleUtils.LogError(Resources.ErrorCannotTakeScreenshotNoConsoleSession, ex);
            }
            catch (Exception ex)
            {
                // This is something else, we'd better know about this.
                ConsoleUtils.LogError(Resources.ErrorCannotTakeScreenshotUnexpectedError, ex);
            }
        }

        private static void SaveScreenToFile(string fileName)
        {
            int width = SystemInformation.VirtualScreen.Width;
            int height = SystemInformation.VirtualScreen.Height;
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenTop = SystemInformation.VirtualScreen.Top;

            using (Bitmap screenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(screenLeft, screenTop, 0, 0, new Size(width, height));
                }

                screenshot.Save(fileName, ImageFormat.Png);
            }
        }

        private static string GenerateScreenshotFileName(string description, string outputFolder)
        {
            var outputFolderInfo = new DirectoryInfo(outputFolder);
            if (!outputFolderInfo.Exists)
            {
                outputFolderInfo.Create();
                ConsoleUtils.LogMessage(Resources.InfoCreatedOutputFolder, outputFolderInfo.FullName);
            }

            var fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_ff") + "-" + description + ".png";
            fileName = Path.Combine(outputFolderInfo.FullName, fileName);
            return fileName;
        }
    }
}