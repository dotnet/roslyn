// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;

namespace Roslyn.SyntaxVisualizer.Control
{
    internal static class DotNetFrameworkUtilities
    {
        private const string frameworkReleaseRegKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full";
        private const int net47Version = 460805; // Highest .NET 47 release version
        private static bool? net471OrAboveInstalled;

        public static bool IsInstalledFramework471OrAbove()
        {
            try
            {
                if (!net471OrAboveInstalled.HasValue)
                {
                    net471OrAboveInstalled = false;
                    using (var key = Registry.LocalMachine.OpenSubKey(frameworkReleaseRegKey))
                    {
                        var version = key != null ? (int)(key.GetValue("Release") ?? int.MinValue) : int.MinValue;
                        net471OrAboveInstalled = version > net47Version ? true : false;
                    }

                    return net471OrAboveInstalled ?? false;
                }
            }
            catch
            {
                // Intentionally blank
            }

            return false;
        }
    }
}
