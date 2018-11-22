// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public abstract class ManagedToolTask : ToolTask
    {
        protected abstract bool IsManagedTool { get; }

        /// <summary>
        /// ToolArguments are the arguments intended to be passed to the tool,
        /// without taking into account any runtime-specific modifications.
        /// </summary>
        protected abstract string ToolArguments { get; }

        protected abstract string PathToManagedTool { get; }

        /// <summary>
        /// Note: "Native" here does not necessarily mean "native binary".
        /// "Native" in this context means "native invocation", and running the executable directly.
        /// </summary>
        protected abstract string PathToNativeTool { get; }

        /// <summary>
        /// GenerateCommandLineCommands generates the actual OS-level arguments:
        /// if dotnet needs to be executed and the managed assembly is the first argument,
        /// then this will contain the managed assembly followed by ToolArguments
        /// </summary>
        protected sealed override string GenerateCommandLineCommands()
        {
            var commandLineArguments = ToolArguments;
            if (IsManagedTool && IsCliHost(out string pathToDotnet))
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

        /// <summary>
        /// This generates the path to the executable that is directly ran.
        /// This could be the managed assembly itself (on desktop .net on Windows),
        /// or a runtime such as dotnet.
        /// </summary>
        protected sealed override string GenerateFullPathToTool()
        {
            if (IsManagedTool)
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

        /// <summary>
        /// ToolName is only used in cases where <see cref="IsManagedTool"/> returns true.
        /// It returns the name of the managed assembly, which might not be the path returned by
        /// GenerateFullPathToTool, which can return the path to e.g. the dotnet executable.
        /// </summary>
        /// <remarks>
        /// We *cannot* actually call IsManagedTool in the implementation of this method,
        /// as the implementation of IsManagedTool calls this property. See the comment in
        /// <see cref="ManagedCompiler.HasToolBeenOverridden"/>.
        /// </remarks>
        protected sealed override string ToolName
            => $"{ToolNameWithoutExtension}.{(CoreClrShim.IsRunningOnCoreClr ? "dll" : "exe")}";

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
