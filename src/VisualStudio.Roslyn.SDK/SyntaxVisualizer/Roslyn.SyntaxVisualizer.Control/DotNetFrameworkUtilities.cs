// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Win32;

namespace Roslyn.SyntaxVisualizer.Control
{
    internal static class DotNetFrameworkUtilities
    {
        private static string frameworkReleaseRegKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full";
        private static int net47Version = 460805; // Highest .NET 47 release version
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
                        int version = key != null ? (int)(key.GetValue("Release") ?? int.MinValue) : int.MinValue;
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
