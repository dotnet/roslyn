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
    /// <summary>
    /// Base class for the MSBuild tasks that run the built-in managed compilers (<c>Csc</c>, <c>Vbc</c>,
    /// <c>Csi</c>).
    /// </summary>
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

        /// <summary>
        /// The full path to the built-in compiler managed assembly (<c>csc.dll</c>) or, when an apphost is
        /// present, the apphost executable (<c>csc.exe</c>) - the file named by <see cref="ToolName"/> inside
        /// <see cref="GetToolDirectory"/>.
        /// </summary>
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
        /// Generate the arguments to pass directly to the built-in tool. These do not include
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
        /// Generate the arguments to pass directly to the built-in tool. These do not include
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

        /// <summary>
        /// Gets the base name used to derive the built-in tool's managed assembly and apphost names.
        /// </summary>
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

        // On-disk layout in the partially-AOT .NET SDK CLI: the runtime muxer (dotnet.exe) lives at the
        // install root and loads <sdk_dir>\dotnet-aot.dll as a native library, so it is not a managed entry
        // point running from the SDK directory - AppContext.BaseDirectory is the install root, not the
        // SDK directory (see GetBuildTaskDirectory for how the SDK directory is recovered):
        //
        // C:\Program Files\dotnet\                     <- install root; the muxer process runs from here
        // ├─ dotnet.exe                                <- the muxer (the process / host)
        // ├─ host\fxr\<ver>\hostfxr.dll
        // └─ sdk\10.0.300\                             <- sdk_dir (passed to dotnet_execute)
        //    ├─ dotnet.dll                             <- managed CLI fallback: Path.Join(sdk_dir, "dotnet.dll")
        //    ├─ dotnet-aot.dll                         <- loaded by the muxer as a native lib; build tasks linked in
        //    ├─ MSBuild.dll
        //    └─ Roslyn\bincore\csc.dll                 <- the compiler the task launches (at <sdk_dir>\Roslyn\bincore)

        /// <summary>
        /// Returns the folder that holds the built-in compiler: on .NET Core the <c>bincore</c> subfolder of
        /// the build task directory; on .NET Framework the build task directory itself, except the SDK
        /// framework-to-core bridge task which reaches the sibling <c>..\bincore</c>.
        /// </summary>
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

        /// <summary>
        /// Answers a single question - "where, on disk, is this build task assembly?" - and is the anchor
        /// from which <see cref="GetToolDirectory"/> locates the sibling compiler.
        /// <para>
        /// In the common loose-file MSBuild deployment this is the directory of
        /// <see cref="System.Reflection.Assembly.Location"/>. In the partially-AOT .NET SDK CLI that path is
        /// empty, so the versioned SDK directory the AOT host publishes as the <c>Microsoft.DotNet.Sdk.Root</c>
        /// <see cref="AppContext"/> value is read first and this task's <c>Roslyn</c> subfolder is appended.
        /// See <see href="https://github.com/dotnet/sdk/pull/55110"/>.
        /// </para>
        /// </summary>
#if NET
        // Assembly.Location is a loose-file-only API flagged by the single-file analyzer (IL3000): it
        // returns an empty string under single-file / AOT. It cannot be annotated with
        // [RequiresAssemblyFiles] because that attribute would have to flow onto the sealed ToolTask
        // overrides (GenerateFullPathToTool, GenerateCommandLineCommands, ToolName, ExecuteTool), whose base
        // declarations are not annotated - and that mismatch is itself an error (IL3003). Under single-file /
        // AOT the Microsoft.DotNet.Sdk.Root AppContext value (checked first) supplies the directory, so the
        // empty Assembly.Location is never used there and the IL3000 is suppressed.
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly file path when publishing as a single file", Justification = "Assembly.Location is the loose-file path; single-file/AOT hosts have no on-disk location and instead read the Microsoft.DotNet.Sdk.Root AppContext value (checked first). [RequiresAssemblyFiles] cannot be used because it would flow onto the unannotated ToolTask overrides (IL3003).")]
#endif
        internal static string GetBuildTaskDirectory()
        {
#if NET
            // Partially-AOT .NET SDK CLI host (the SDK's dotnet-aot.dll, loaded directly by the runtime
            // muxer): Assembly.Location is empty and AppContext.BaseDirectory is the muxer's install root
            // rather than the versioned SDK directory. The AOT bridge publishes the resolved SDK directory as
            // the Microsoft.DotNet.Sdk.Root AppContext value for the assemblies compiled into it; out-of-repo
            // code such as this build task reads it first. The build task ships in the "Roslyn" subfolder
            // beneath the SDK directory. This name must match the constant published by the SDK's
            // Microsoft.DotNet.Cli.Utils.SdkPaths.
            if (AppContext.GetData("Microsoft.DotNet.Sdk.Root") is string { Length: > 0 } sdkDirectory)
            {
                return Path.Combine(sdkDirectory, "Roslyn");
            }
#endif

            // Loose-file MSBuild deployment (the common scenario on both .NET Framework and .NET Core, and
            // the compiler toolset NuGet package): this assembly is on disk, so its own directory is the
            // build task directory (for example <sdk>\Roslyn).
            if (Path.GetDirectoryName(typeof(ManagedToolTask).Assembly.Location) is { Length: > 0 } buildTaskDirectory)
            {
                return buildTaskDirectory;
            }

            throw new InvalidOperationException("Unable to determine the location of the build task assembly.");
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
