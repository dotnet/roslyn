// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using Roslyn.Utilities;

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

        /// <summary>
        /// Get the path to the dotnet executable. In the case the .NET SDK did not provide this information
        /// in the environment this tries to find "dotnet" on the PATH. In the case it is not found,
        /// this will return simply "dotnet".
        /// </summary>
        internal static string GetDotNetPathOrDefault()
        {
            if (GetDotNetHostPath() is { } pathToDotNet)
            {
                return pathToDotNet;
            }

            var (fileName, sep) = PlatformInformation.IsWindows
                ? ("dotnet.exe", new char[] { ';' })
                : ("dotnet", new char[] { ':' });

            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var item in path.Split(sep, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var filePath = Path.Combine(item, fileName);
                    if (File.Exists(filePath))
                    {
                        return filePath;
                    }
                }
                catch
                {
                    // If we can't read a directory for any reason just skip it
                }
            }

            return fileName;
        }

        internal static string GetDotNetExecCommandLine(string toolFilePath, string commandLineArguments) =>
            $@"exec ""{toolFilePath}"" {commandLineArguments}";
    }
}
