// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    internal static class ActivityLogCollector
    {
        internal static void TryWriteActivityLogToFile(string filePath)
        {
            var vsAppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "VisualStudio");
            if (!Directory.Exists(vsAppDataDirectory))
                return;

            var content = new StringBuilder();
            foreach (var folder in Directory.GetDirectories(vsAppDataDirectory, $"{Settings.Default.VsProductVersion}*{Settings.Default.VsRootSuffix}"))
            {
                var activityLog = Path.Combine(folder, "ActivityLog.xml");
                if (File.Exists(activityLog))
                {
                    try
                    {
                        content.AppendLine(File.ReadAllText(activityLog));
                    }
                    catch (Exception e)
                    {
                        content.AppendLine(e.ToString());
                    }
                }
            }

            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);
        }
    }
}
