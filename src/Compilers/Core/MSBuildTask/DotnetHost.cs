// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal class DotnetHost
    {
        public string ToolName { get; }

        public string PathToToolOpt { get; }

        public string CommandLineArgs { get; }

        public DotnetHost(string toolName, string pathToTool, string commandLineArgs)
        {
            ToolName = toolName;
            PathToToolOpt = pathToTool;
            CommandLineArgs = commandLineArgs;
        }

        public DotnetHost(string toolNameWithoutExtension, string commandLineArgs)
        {
            string pathToTool;
            string toolName;
            if (IsCliHost(out string pathToDotnet))
            {
                toolName = $"{toolNameWithoutExtension}.dll";
                pathToTool = Utilities.GenerateFullPathToTool(toolName);
                if (pathToTool != null)
                {
                    commandLineArgs = $"\"{pathToTool}\" {commandLineArgs}";
                    pathToTool = pathToDotnet;
                }
            }
            else
            {
                // Desktop executes tool directly
                toolName = $"{toolNameWithoutExtension}.exe";
                pathToTool = Utilities.GenerateFullPathToTool(toolName);
            }

            PathToToolOpt = pathToTool;
            CommandLineArgs = commandLineArgs;
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
    }
}
