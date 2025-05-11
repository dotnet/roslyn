﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static bool DefaultIsSdkFrameworkToCoreBridgeTask { get; } = CalculateIsSdkFrameworkToCoreBridgeTask();

        /// <summary>
        /// Is the builtin tool being used here? When false the developer has specified a custom tool
        /// to be run by this task
        /// </summary>
        /// <remarks>
        /// ToolExe delegates back to ToolName if the override is not
        /// set.  So, if ToolExe == ToolName, we know ToolExe is not
        /// explicitly overridden.  So, if both ToolPath is unset and
        /// ToolExe == ToolName, we know nothing is overridden, and
        /// we can use our own csc.
        /// </remarks>
        protected bool UsingBuiltinTool => string.IsNullOrEmpty(ToolPath) && ToolExe == ToolName;

        /// <summary>
        /// A copy of this task, compiled for .NET Framework, is deployed into the .NET SDK. It is a bridge task
        /// that is loaded into .NET Framework MSBuild but launches the .NET Core compiler. This task necessarily
        /// has different behaviors than the standard build task compiled for .NET Framework and loaded into the 
        /// .NET Framework MSBuild.
        /// </summary>
        /// <remarks>
        /// This is mutable to facilitate testing
        /// </remarks>
        internal bool IsSdkFrameworkToCoreBridgeTask { get; init; } = DefaultIsSdkFrameworkToCoreBridgeTask;

        /// <summary>
        /// Is the builtin tool executed by this task running on .NET Core?
        /// </summary>
        internal bool IsBuiltinToolRunningOnCoreClr => RuntimeHostInfo.IsCoreClrRuntime || IsSdkFrameworkToCoreBridgeTask;

        internal string PathToBuiltInTool => Path.Combine(GetToolDirectory(), ToolName);

        protected ManagedToolTask(ResourceManager resourceManager)
            : base(resourceManager)
        {
        }

        /// <summary>
        /// Generate the arguments to pass directly to the buitin tool. These do not include
        /// arguments in the response file.
        /// </summary>
        /// <remarks>
        /// This will be the same value whether the build occurs on .NET Core or .NET Framework. 
        /// </remarks>
        internal string GenerateToolArguments()
        {
            var builder = new CommandLineBuilderExtension();
            AddCommandLineCommands(builder);
            return builder.ToString();
        }

        /// <summary>
        /// <see cref="GenerateCommandLineContents" />
        /// </summary>
        protected sealed override string GenerateCommandLineCommands()
        {
            var commandLineArguments = GenerateToolArguments();
            if (UsingBuiltinTool && IsBuiltinToolRunningOnCoreClr)
            {
                commandLineArguments = RuntimeHostInfo.GetDotNetExecCommandLine(PathToBuiltInTool, commandLineArguments);
            }

            return commandLineArguments;
        }

        /// <summary>
        /// <see cref="GenerateResponseFileContents"/>
        /// </summary>
        protected sealed override string GenerateResponseFileCommands()
        {
            var commandLineBuilder = new CommandLineBuilderExtension();
            AddResponseFileCommands(commandLineBuilder);
            return commandLineBuilder.ToString();
        }

        /// <summary>
        /// Generate the arguments to pass directly to the buitin tool. These do not include
        /// arguments in the response file.
        /// </summary>
        /// <remarks>
        /// This will include target specific arguments like 'exec'
        /// </remarks>
        internal string GenerateCommandLineContents() => GenerateCommandLineCommands();

        /// <summary>
        /// Generate the arguments to pass via a response file. 
        /// </summary>
        /// <remarks>
        /// This will be the same value whether the build occurs on .NET Core or .NET Framework. 
        /// </remarks>
        internal string GenerateResponseFileContents() => GenerateResponseFileCommands();

        /// <summary>
        /// This generates the path to the executable that is directly ran.
        /// This could be the managed assembly itself (on desktop .NET on Windows),
        /// or a runtime such as dotnet.
        /// </summary>
        protected sealed override string GenerateFullPathToTool() => (UsingBuiltinTool, IsBuiltinToolRunningOnCoreClr) switch
        {
            (true, true) => RuntimeHostInfo.GetDotNetPathOrDefault(),
            (true, false) => PathToBuiltInTool,
            (false, _) => Path.Combine(ToolPath ?? "", ToolExe)
        };

        protected abstract string ToolNameWithoutExtension { get; }

        protected abstract void AddCommandLineCommands(CommandLineBuilderExtension commandLine);

        protected abstract void AddResponseFileCommands(CommandLineBuilderExtension commandLine);

        /// <summary>
        /// This is the file name of the builtin tool that will be executed.
        /// </summary>
        /// <remarks>
        /// ToolName is only used in cases where <see cref="UsingBuiltinTool"/> returns true.
        /// It returns the name of the managed assembly, which might not be the path returned by
        /// GenerateFullPathToTool, which can return the path to e.g. the dotnet executable.
        /// </remarks>
        protected sealed override string ToolName =>
            IsBuiltinToolRunningOnCoreClr
                ? $"{ToolNameWithoutExtension}.dll"
                : $"{ToolNameWithoutExtension}.exe";

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
            CommandLineUtilities.SplitCommandLineIntoArguments(GenerateToolArguments().AsSpan(), removeHashComments: true, builder, argumentList, out _);
            CommandLineUtilities.SplitCommandLineIntoArguments(responseFileCommands.AsSpan(), removeHashComments: true, builder, argumentList, out _);
            return argumentList;
        }

        /// <summary>
        /// Generates the <see cref="ITaskItem"/> entries for the CommandLineArgs output ItemGroup
        /// for our tool tasks
        /// </summary>
        /// <remarks>
        /// This does not include any runtime specific arguments like 'dotnet' or 'exec'.
        /// </remarks>
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

        private string GetToolDirectory()
        {
            var buildTask = typeof(ManagedToolTask).Assembly;
            var buildTaskDirectory = GetBuildTaskDirectory();
#if NET
            return Path.Combine(buildTaskDirectory, "bincore");
#else
            return IsSdkFrameworkToCoreBridgeTask
                ? Path.Combine(buildTaskDirectory, "..", "bincore")
                : buildTaskDirectory;
#endif
        }

        /// <summary>
        /// <see cref="IsSdkFrameworkToCoreBridgeTask"/>
        /// </summary>
        /// <remarks>
        /// Using the file system as a way to differentiate between the two tasks is not ideal, but it is effective
        /// and allows us to avoid significantly complicating the build process. The alternative is another parameter
        /// to the Csc / Vbc / etc ... tasks that all invocations would need to pass along.
        /// </remarks>
        internal static bool CalculateIsSdkFrameworkToCoreBridgeTask()
        {
#if NET
            return false;
#else
            // This logic needs to be updated when this issue is fixed. That moves csc.exe out to a subdirectory
            // and hence the check below will need to change
            //
            // https://github.com/dotnet/roslyn/issues/78001

            var buildTaskDirectory = GetBuildTaskDirectory();
            var buildTaskDirectoryName = Path.GetFileName(buildTaskDirectory);
            return
                string.Equals(buildTaskDirectoryName, "binfx", StringComparison.OrdinalIgnoreCase) &&
                !File.Exists(Path.Combine(buildTaskDirectory, "csc.exe")) &&
                Directory.Exists(Path.Combine(buildTaskDirectory, "..", "bincore"));
#endif
        }

        internal static string GetBuildTaskDirectory()
        {
            var buildTask = typeof(ManagedToolTask).Assembly;
            var buildTaskDirectory = Path.GetDirectoryName(buildTask.Location);
            if (buildTaskDirectory is null)
            {
                // This should not happen in supported product scenarios but could happen if 
                // a non-supported scenario tried to load our task (like AOT) and call
                // through these members.
                throw new InvalidOperationException("Unable to determine the location of the build task assembly.");
            }

            return buildTaskDirectory;
        }
    }
}
