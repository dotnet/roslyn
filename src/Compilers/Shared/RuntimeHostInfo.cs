// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipes;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This type provides information about the runtime which is hosting application. It must be included in a concrete 
    /// target framework to be used.
    /// </summary>
    internal static class RuntimeHostInfo
    {
        internal static bool IsCoreClrRuntime => !IsDesktopRuntime;

        internal static string ToolExtension => IsCoreClrRuntime ? "dll" : "exe";

        /// <summary>
        /// This gets information about invoking a tool on the current runtime. This will attempt to 
        /// execute a tool as an EXE when on desktop and using dotnet when on CoreClr.
        /// </summary>
        internal static (string processFilePath, string commandLineArguments, string toolFilePath) GetProcessInfo(string toolFilePathWithoutExtension, string commandLineArguments)
        {
            Debug.Assert(!toolFilePathWithoutExtension.EndsWith(".dll") && !toolFilePathWithoutExtension.EndsWith(".exe"));

            var toolFilePath = $"{toolFilePathWithoutExtension}.{ToolExtension}";
            if (IsDotNetHost(out string pathToDotNet))
            {
                commandLineArguments = $@"exec ""{toolFilePath}"" {commandLineArguments}";
                return (pathToDotNet, commandLineArguments, toolFilePath);
            }
            else
            {
                return (toolFilePath, commandLineArguments, toolFilePath);
            }
        }

#if NET472
        internal static bool IsDesktopRuntime => true;

        internal static bool IsDotNetHost(out string pathToDotNet)
        {
            pathToDotNet = null;
            return false;
        }

        internal static NamedPipeClientStream CreateNamedPipeClient(string serverName, string pipeName, PipeDirection direction, PipeOptions options) =>
            new NamedPipeClientStream(serverName, pipeName, direction, options);

#elif NETCOREAPP2_1
        internal static bool IsDesktopRuntime => false;

        private static string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        private static bool IsDotNetHost(out string pathToDotNet)
        {
            pathToDotNet = GetDotNetPathOrDefault();
            return true;
        }

        /// <summary>
        /// Get the path to the dotnet executable. This will throw in the case it is not properly setup 
        /// by the environment.
        /// </summary>
        private static string GetDotNetPath()
        {
            var pathToDotNet = Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName);
            if (string.IsNullOrEmpty(pathToDotNet))
            {
                throw new InvalidOperationException($"{DotNetHostPathEnvironmentName} is not set");
            }
            return pathToDotNet;
        }

        /// <summary>
        /// Get the path to the dotnet executable. In the case the host did not provide this information
        /// in the environment this will return simply "dotnet".
        /// </summary>
        private static string GetDotNetPathOrDefault()
        {
            var pathToDotNet = Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName);
            return pathToDotNet ?? "dotnet";
        }
#else
#error Unsupported configuration
#endif
    }
}
