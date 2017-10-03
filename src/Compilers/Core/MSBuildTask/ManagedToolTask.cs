// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public abstract class ManagedToolTask : ToolTask
    {
        protected abstract bool IsManagedTask { get; }

        protected abstract string CommandLineArguments { get; }

        protected abstract string PathToManagedTool { get; }

        /// <summary>
        /// Note: "Native" here does not neccesarily mean "native binary".
        /// "Native" in this context means "native invocation", and running the executable directly.
        /// </summary>
        protected abstract string PathToNativeTool { get; }

        protected sealed override string GenerateCommandLineCommands()
        {
            var commandLineArguments = CommandLineArguments;
            if (IsManagedTask && IsCliHost(out string pathToDotnet))
            {
                var pathToTool = PathToManagedTool;
                if (pathToTool is null)
                {
                    Log.LogErrorWithCodeFromResources("General_ToolFileNotFound", ToolName);
                }
                commandLineArguments = PrependFileToArgs(pathToTool, commandLineArguments);
            }

            return commandLineArguments;
        }

        protected sealed override string GenerateFullPathToTool()
        {
            if (IsManagedTask)
            {
                if (IsCliHost(out string pathToDotnet))
                {
                    return pathToDotnet;
                }
                else
                {
                    return PathToManagedTool;
                }
            }
            else
            {
                return PathToNativeTool;
            }
        }

        protected abstract string ToolNameWithoutExtension { get; }

        protected sealed override string ToolName
        {
            get
            {
                if (CoreClrShim.IsRunningOnCoreClr)
                {
                    return $"{ToolNameWithoutExtension}.dll";
                }
                else
                {
                    return $"{ToolNameWithoutExtension}.exe";
                }
            }
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
