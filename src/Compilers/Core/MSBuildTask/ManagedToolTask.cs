// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public abstract class ManagedToolTask : ToolTask
    {
        private bool? _useAppHost;
        internal readonly PropertyDictionary _store = new PropertyDictionary();

        /// <summary>
        /// A copy of this task, compiled for .NET Framework, is deployed into the .NET SDK. It is a bridge task
        /// that is loaded into .NET Framework MSBuild but launches the .NET Core compiler. This task necessarily
        /// has different behaviors than the standard build task compiled for .NET Framework and loaded into the 
        /// .NET Framework MSBuild.
        /// </summary>
        /// <remarks>
        /// The reason this task is a different assembly is to allow both the MSBuild and .NET SDK copy to be loaded
        /// into the same MSBuild process.
        /// </remarks>
        internal static bool IsSdkFrameworkToCoreBridgeTask =>
#if NETFRAMEWORK && SDK_TASK
            true;
#else
            false;
#endif

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
        /// <para />
        /// We want to continue using the builtin tool if user sets <see cref="ToolTask.ToolExe"/> = <c>csc.exe</c>,
        /// regardless of whether apphosts will be used or not (in the former case, <see cref="ToolName"/> could be <c>csc.dll</c>),
        /// hence we also compare <see cref="ToolTask.ToolExe"/> with <see cref="AppHostToolName"/> in addition to <see cref="ToolName"/>.
        /// </remarks>
        protected bool UsingBuiltinTool => string.IsNullOrEmpty(ToolPath) && (ToolExe == ToolName || ToolExe == AppHostToolName);

        /// <summary>
        /// Is the builtin tool executed by this task running on .NET Core?
        /// </summary>
        /// <remarks>
        /// Keep in sync with <see cref="Microsoft.CodeAnalysis.CommandLine.BuildServerConnection.IsBuiltinToolRunningOnCoreClr"/>.
        /// </remarks>
        internal static bool IsBuiltinToolRunningOnCoreClr => RuntimeHostInfo.IsCoreClrRuntime || IsSdkFrameworkToCoreBridgeTask;

        internal string PathToBuiltInTool => Path.Combine(GetToolDirectory(), ToolName);

        /// <summary>
        /// We fallback to not use the apphost if it is not present (can happen in compiler toolset scenarios for example).
        /// </summary>
        private bool UseAppHost
        {
            get
            {
                if (_useAppHost is not { } useAppHost)
                {
                    _useAppHost = useAppHost = File.Exists(Path.Combine(GetToolDirectory(), AppHostToolName));
                    Debug.Assert(IsBuiltinToolRunningOnCoreClr || useAppHost);
                }

                return useAppHost;
            }
        }

        internal bool UseAppHost_TestOnly { set => _useAppHost = value; }

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

            if (UsingBuiltinTool && !UseAppHost)
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
        /// This could be the executable apphost or a runtime such as dotnet.
        /// </summary>
        protected sealed override string GenerateFullPathToTool()
        {
            if (UsingBuiltinTool)
            {
                // Even if we return full path to tool as "C:\Program Files\dotnet\dotnet.exe" for example,
                // an MSBuild logic (caller of this function) can turn that into "C:\Program Files\dotnet\csc.exe" if `ToolExe` is set explicitly to "csc.exe".
                // Resetting `ToolExe` to `null` skips that logic. That is a correct thing to do since we already checked `UsingBuiltinTool`
                // which means `ToolExe` is not really overridden by user (yes, the user sets it but basically to its default value).
                ToolExe = null;

                return UseAppHost ? PathToBuiltInTool : RuntimeHostInfo.GetDotNetPathOrDefault();
            }

            return Path.Combine(ToolPath ?? "", ToolExe);
        }

        protected abstract string ToolNameWithoutExtension { get; }

        protected abstract void AddCommandLineCommands(CommandLineBuilderExtension commandLine);

        protected abstract void AddResponseFileCommands(CommandLineBuilderExtension commandLine);

        /// <summary>
        /// This is the file name of the builtin tool that will be executed.
        /// </summary>
        /// <remarks>
        /// ToolName is only used in cases where <see cref="UsingBuiltinTool"/> returns true.
        /// It returns the name of the managed assembly, which might not be the path returned by
        /// <see cref="GenerateFullPathToTool"/>, which can return the path to e.g. the dotnet executable.
        /// </remarks>
        protected sealed override string ToolName
        {
            get
            {
                return UseAppHost ? AppHostToolName : $"{ToolNameWithoutExtension}.dll";
            }
        }

        private string AppHostToolName => $"{ToolNameWithoutExtension}{PlatformInformation.ExeExtension}";

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

        internal static string GetToolDirectory()
        {
            var buildTaskDirectory = GetBuildTaskDirectory();
#if NET
            return Path.Combine(buildTaskDirectory, "bincore");
#else
            return IsSdkFrameworkToCoreBridgeTask
                ? Path.Combine(buildTaskDirectory, "..", "bincore")
                : buildTaskDirectory;
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

        protected override bool ValidateParameters()
        {
            // Set DOTNET_ROOT so that the apphost executables launch properly.
            // Unset all other DOTNET_ROOT* variables so for example DOTNET_ROOT_X64 does not override ours.
            if (IsBuiltinToolRunningOnCoreClr && RuntimeHostInfo.GetToolDotNetRoot(Log.LogMessage) is { } dotNetRoot)
            {
                Log.LogMessage("Setting {0} to '{1}'", RuntimeHostInfo.DotNetRootEnvironmentName, dotNetRoot);
                EnvironmentVariables =
                [
                    .. EnvironmentVariables?.Where(static e => !e.StartsWith(RuntimeHostInfo.DotNetRootEnvironmentName, StringComparison.OrdinalIgnoreCase)) ?? [],
                    .. Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
                        .Where(e => ((string)e.Key).StartsWith(RuntimeHostInfo.DotNetRootEnvironmentName, StringComparison.OrdinalIgnoreCase))
                        .Select(e => $"{e.Key}="),
                    $"{RuntimeHostInfo.DotNetRootEnvironmentName}={dotNetRoot}",
                ];
            }

            if (RuntimeHostInfo.ShouldDisableTieredCompilation && Environment.GetEnvironmentVariable(RuntimeHostInfo.DotNetTieredCompilationEnvironmentName) == null)
            {
                var value = "0";
                Log.LogMessage("Setting {0} to '{1}'", RuntimeHostInfo.DotNetTieredCompilationEnvironmentName, value);
                EnvironmentVariables =
                [
                    .. EnvironmentVariables ?? [],
                    $"{RuntimeHostInfo.DotNetTieredCompilationEnvironmentName}={value}",
                ];
            }

            return base.ValidateParameters();
        }
    }
}
