// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This class provides simple properties for determining whether the current platform is Windows or Unix-based.
    /// We intentionally do not use System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(...) because
    /// it incorrectly reports 'true' for 'Windows' in desktop builds running on Unix-based platforms via Mono.
    /// </summary>
    internal static class PlatformInformation
    {
        public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        public static bool IsUnix => Path.DirectorySeparatorChar == '/';
        public static bool IsRunningOnMono
        {
            get
            {
                try
                {
                    return !(Type.GetType("Mono.Runtime") is null);
                }
                catch
                {
                    // Arbitrarily assume we're not running on Mono.
                    return false;
                }
            }
        }
    }
}
