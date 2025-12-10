// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Text;
    using Xunit.InProcess;

    public static class IdeStateCollector
    {
        private static ImmutableList<KeyValuePair<string, Func<string>>> _customIdeStateCollectors = ImmutableList<KeyValuePair<string, Func<string>>>.Empty;

        public static void RegisterCustomState(string title, Func<string> callback)
        {
            ImmutableInterlocked.Update(
                ref _customIdeStateCollectors,
                (loggers, newLogger) => loggers.Add(newLogger),
                new KeyValuePair<string, Func<string>>(title, callback));
        }

        internal static void TryWriteIdeStateToFile(string filePath)
        {
            try
            {
                var content = VisualStudio_InProc.GetIdeState(_customIdeStateCollectors);
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
