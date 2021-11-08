// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Xunit.Harness
{
    using System;
    using System.IO;
    using System.Text;
    using Xunit.InProcess;

    internal static class IdeStateCollector
    {
        internal static void TryWriteIdeStateToFile(string filePath)
        {
            try
            {
                var content = VisualStudio_InProc.GetIdeState();
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                File.WriteAllText(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                File.WriteAllText(filePath, ex.ToString(), Encoding.UTF8);
            }
        }
    }
}
