// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Runtime.Versioning;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This class provides simple properties for determining whether the current platform is Windows or Unix-based.
    /// </summary>
    /// <remarks>
    /// When running on Mono 5.8 or older runtime on non-Windows, it is possible to deploy a Windows version of the DLL containing class RuntimeInformation.
    /// This can cause a Windows-specific result to be reported, even if, for example, the program is running on Linux.
    /// i.e.: 'RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true' and 'RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == false'.
    /// Such a runtime+package configuration has been superseded since February 2018. It is unlikely that new code needs to use
    /// <see cref="IsWindows"/> and <see cref="IsUnix"/> helpers in this type instead of 'RuntimeInformation.IsOSPlatform'.
    /// At some point, we may wish to consider replacing these APIs accordingly.
    /// </remarks>
    internal static class PlatformInformation
    {
#if NET5_0_OR_GREATER
        [SupportedOSPlatformGuard("windows")]
#endif
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
        /// <summary>
        /// Are we running on .NET 5 or later using the Mono runtime?
        /// Will also return true when running on Mono itself; if necessary
        /// we can use IsRunningOnMono to distinguish.
        /// </summary>
        public static bool IsUsingMonoRuntime
        {
            get
            {
                try
                {
                    return !(Type.GetType("Mono.RuntimeStructs", throwOnError: false) is null);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string ExeExtension => IsWindows ? ".exe" : string.Empty;
    }
}
