// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal sealed class DotnetHost
    {
        public string ToolNameOpt { get; }

        public string PathToToolOpt { get; }

        public string CommandLineArgs { get; }

        /// <summary>
        /// True if this tool is a managed executable, and will be invoked with `dotnet`.
        /// False if this executable is being invoked directly.
        /// </summary>
        public bool IsManagedTool { get; }

        private DotnetHost(string toolNameOpt, string pathToToolOpt, string commandLineArgs, bool isManagedTool)
        {
            ToolNameOpt = toolNameOpt;
            PathToToolOpt = pathToToolOpt;
            CommandLineArgs = commandLineArgs;
            IsManagedTool = isManagedTool;
        }

        public static DotnetHost CreateUnmanagedToolInvocation(string pathToTool, string commandLineArgs)
        {
            return new DotnetHost(null, pathToTool, commandLineArgs, isManagedTool: false);
        }

        public static DotnetHost CreateManagedToolInvocation(string toolNameWithoutExtension, string commandLineArgs)
        {
            string pathToToolOpt;
            string toolName;
            if (IsCliHost(out string pathToDotnet))
            {
                toolName = $"{toolNameWithoutExtension}.dll";
                pathToToolOpt = Utilities.GenerateFullPathToTool(toolName);
                if (pathToToolOpt != null)
                {
                    commandLineArgs = PrependFileToArgs(pathToToolOpt, commandLineArgs);
                    pathToToolOpt = pathToDotnet;
                }
            }
            else
            {
                // Desktop executes tool directly
                toolName = $"{toolNameWithoutExtension}.exe";
                pathToToolOpt = Utilities.GenerateFullPathToTool(toolName);
            }

            return new DotnetHost(toolName, pathToToolOpt, commandLineArgs, isManagedTool: true);
        }

        private static bool IsCliHost(out string pathToDotnet)
        {
            if (CoreClrShim.IsRunningOnCoreClr)
            {
                pathToDotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
                return !string.IsNullOrEmpty(pathToDotnet);
            }
            else
            {
                pathToDotnet = null;
                return false;
            }
        }

        private static string PrependFileToArgs(string pathToTool, string commandLineArgs)
        {
            var builder = new CommandLineBuilderExtension();
            builder.AppendFileNameIfNotNull(pathToTool);
            builder.AppendTextUnquoted(" ");
            builder.AppendTextUnquoted(commandLineArgs);
            return builder.ToString();
        }
    }
}
