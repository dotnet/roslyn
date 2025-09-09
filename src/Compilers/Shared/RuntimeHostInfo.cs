// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This type provides information about the runtime which is hosting application. It must be included in a concrete 
    /// target framework to be used.
    /// </summary>
    internal static class RuntimeHostInfo
    {
        internal static bool IsDesktopRuntime => !IsCoreClrRuntime;

        internal static bool IsCoreClrRuntime =>
#if NET
            true;
#else
            false;
#endif

        internal const string DotNetRootEnvironmentName = "DOTNET_ROOT";
        private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";
        private const string DotNetExperimentalHostPathEnvironmentName = "DOTNET_EXPERIMENTAL_HOST_PATH";

        /// <summary>
        /// The <c>DOTNET_ROOT</c> that should be used when launching executable tools.
        /// </summary>
        internal static string? GetToolDotNetRoot()
        {
            if (GetDotNetHostPath() is { } dotNetHostPath)
            {
                return Path.GetDirectoryName(dotNetHostPath);
            }

            return null;
        }

        private static string? GetDotNetHostPath()
        {
            if (Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName) is { Length: > 0 } pathToDotNet)
            {
                return pathToDotNet;
            }

            if (Environment.GetEnvironmentVariable(DotNetExperimentalHostPathEnvironmentName) is { Length: > 0 } pathToDotNetExperimental)
            {
                return pathToDotNetExperimental;
            }

            return null;
        }
    }
}
