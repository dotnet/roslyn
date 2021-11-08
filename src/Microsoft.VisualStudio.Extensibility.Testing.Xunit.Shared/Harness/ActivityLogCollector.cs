// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System.IO;
    using System.Text;
    using Xunit.InProcess;

    internal static class ActivityLogCollector
    {
        internal static void TryWriteActivityLogToFile(string filePath)
        {
            var content = VisualStudio_InProc.GetInMemoryActivityLog();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
        }
    }
}
