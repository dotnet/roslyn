// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public abstract class ManagedToolTask : ToolTask
    {
        /// <summary>
        /// Is the standard tool being used here? When false the developer has specified a custom tool
        /// to be run by this task
        /// </summary>
        /// <remarks>
        /// ToolExe delegates back to ToolName if the override is not
        /// set.  So, if ToolExe == ToolName, we know ToolExe is not
        /// explicitly overridden.  So, if both ToolPath is unset and
        /// ToolExe == ToolName, we know nothing is overridden, and
        /// we can use our own csc.
        /// </remarks>
        protected bool IsManagedTool => string.IsNullOrEmpty(ToolPath) && ToolExe == ToolName;

        /// <summary>
        /// ToolArguments are the arguments intended to be passed to the tool,
        /// without taking into account any runtime-specific modifications.
        /// </summary>
        /// <remarks>
        /// This will be the same value whether the build occurs on .NET Core or .NET Framework. 
        /// There will be no 'dotnet' or 'exec' values inserted here for those purposes.
        /// </remarks>
        protected string ToolArguments
        {
            get
            {
                var builder = new CommandLineBuilderExtension();
                AddCommandLineCommands(builder);
                return builder.ToString();
            }
        }

        protected internal string PathToManagedTool => Utilities.GenerateFullPathToTool(ToolName);

        protected string PathToManagedToolWithoutExtension
        {
            get
            {
                var extension = Path.GetExtension(PathToManagedTool);
                return PathToManagedTool.Substring(0, PathToManagedTool.Length - extension.Length);
            }
        }

        protected ManagedToolTask(ResourceManager resourceManager)
            : base(resourceManager)
        {
        }

        /// <summary>
        /// GenerateCommandLineCommands generates the actual OS-level arguments:
        /// if dotnet needs to be executed and the managed assembly is the first argument,
        /// then this will contain the managed assembly followed by ToolArguments
        /// </summary>
        protected sealed override string GenerateCommandLineCommands()
        {
            var commandLineArguments = ToolArguments;
            if (IsManagedTool)
            {
                (_, commandLineArguments, _) = RuntimeHostInfo.GetProcessInfo(PathToManagedToolWithoutExtension, commandLineArguments);
            }

            return commandLineArguments;
        }

        protected sealed override string GenerateResponseFileCommands()
        {
            var commandLineBuilder = new CommandLineBuilderExtension();
            AddResponseFileCommands(commandLineBuilder);
            return commandLineBuilder.ToString();
        }

        internal string GenerateCommandLineContents() => GenerateCommandLineCommands();

        internal string GenerateResponseFileContents() => GenerateResponseFileCommands();

        /// <summary>
        /// This generates the path to the executable that is directly ran.
        /// This could be the managed assembly itself (on desktop .NET on Windows),
        /// or a runtime such as dotnet.
        /// </summary>
        protected sealed override string GenerateFullPathToTool() =>
            IsManagedTool
                ? RuntimeHostInfo.GetProcessInfo(PathToManagedToolWithoutExtension, string.Empty).processFilePath
                : Path.Combine(ToolPath ?? "", ToolExe);

        protected abstract string ToolNameWithoutExtension { get; }

        protected abstract void AddCommandLineCommands(CommandLineBuilderExtension commandLine);

        protected abstract void AddResponseFileCommands(CommandLineBuilderExtension commandLine);

        /// <summary>
        /// ToolName is only used in cases where <see cref="IsManagedTool"/> returns true.
        /// It returns the name of the managed assembly, which might not be the path returned by
        /// GenerateFullPathToTool, which can return the path to e.g. the dotnet executable.
        /// </summary>
        /// <remarks>
        /// We *cannot* actually call IsManagedTool in the implementation of this method,
        /// as the implementation of IsManagedTool calls this property. See the comment in
        /// <see cref="ManagedToolTask.IsManagedTool"/>.
        /// </remarks>
        protected sealed override string ToolName => $"{ToolNameWithoutExtension}.{RuntimeHostInfo.ToolExtension}";

        /// <summary>
        /// This generates the command line arguments passed to the tool.
        /// </summary>
        /// <remarks>
        /// This does not include any runtime specific arguments like 'dotnet' or 'exec'.
        /// </remarks>
        protected List<string> GenerateCommandLineArgsList(string responseFileCommands)
        {
            var argumentList = new List<string>();
            var builder = new StringBuilder();
            CommandLineUtilities.SplitCommandLineIntoArguments(ToolArguments.AsSpan(), removeHashComments: true, builder, argumentList, out _);
            CommandLineUtilities.SplitCommandLineIntoArguments(responseFileCommands.AsSpan(), removeHashComments: true, builder, argumentList, out _);
            return argumentList;
        }

        /// <summary>
        /// Generates the <see cref="ITaskItem"/> entries for the CommandLineArgs output ItemGroup
        /// for our tool tasks
        /// </summary>
        protected internal ITaskItem[] GenerateCommandLineArgsTaskItems(string responseFileCommands) =>
            GenerateCommandLineArgsTaskItems(GenerateCommandLineArgsList(responseFileCommands));

        protected static ITaskItem[] GenerateCommandLineArgsTaskItems(List<string> commandLineArgs)
        {
            var items = new ITaskItem[commandLineArgs.Count];
            for (var i = 0; i < commandLineArgs.Count; i++)
            {
                items[i] = new TaskItem(commandLineArgs[i]);
            }

            return items;
        }
    }
}
