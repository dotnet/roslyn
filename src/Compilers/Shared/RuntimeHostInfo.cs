// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
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

        /// <summary>
        /// This gets information about invoking a tool on the current runtime. This will attempt to 
        /// execute a tool as an EXE when on desktop and using dotnet when on CoreClr.
        /// </summary>
        internal static (string processFilePath, string commandLineArguments, string toolFilePath) GetProcessInfo(string toolFilePathWithoutExtension, string commandLineArguments)
        {
#if NETCOREAPP
            // First check for an app host file and return that if it's available.
            var appHostSuffix = PlatformInformation.IsWindows ? ".exe" : "";
            var appFilePath = $"{toolFilePathWithoutExtension}{appHostSuffix}";
            if (File.Exists(appFilePath))
            {
                return (appFilePath, commandLineArguments, appFilePath);
            }

            // Fallback to the dotnet exec path if there is no apphost
            var toolFilePath = $"{toolFilePathWithoutExtension}.dll";
            var dotnetFilePath = GetDotNetPathOrDefault();
            commandLineArguments = $@"exec ""{toolFilePath}"" {commandLineArguments}";
            return (dotnetFilePath, commandLineArguments, toolFilePath);
#else
            var toolFilePath = $"{toolFilePathWithoutExtension}.exe";
            return (toolFilePath, commandLineArguments, toolFilePath);
#endif
        }

#if NETCOREAPP

        internal static bool IsCoreClrRuntime => true;

        private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        /// <summary>
        /// Get the path to the dotnet executable. In the case the .NET SDK did not provide this information
        /// in the environment this tries to find "dotnet" on the PATH. In the case it is not found,
        /// this will return simply "dotnet".
        /// </summary>
        internal static string GetDotNetPathOrDefault()
        {
            if (Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName) is string pathToDotNet)
            {
                return pathToDotNet;
            }

            var (fileName, sep) = PlatformInformation.IsWindows
                ? ("dotnet.exe", ';')
                : ("dotnet", ':');

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

#else

        internal static bool IsCoreClrRuntime => false;

#endif
    }
}
