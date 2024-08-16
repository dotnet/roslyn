// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.Build.Tasks;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This class defines all of the common stuff that is shared between the Vbc and Csc tasks.
    /// This class is not instantiatable as a Task just by itself.
    /// </summary>
    public abstract class ManagedCompiler : ManagedToolTask
    {
        private enum CompilationKind
        {
            /// <summary>
            /// Compilation occurred using the command line tool by normal processes, typically because 
            /// the customer opted out of the compiler server
            /// </summary>
            Tool,

            /// <summary>
            /// Compilation occurred using the command line tool because the server was unable to complete
            /// the request
            /// </summary>
            ToolFallback,

            /// <summary>
            /// Compilation occurred in the compiler server process
            /// </summary>
            Server,

            /// <summary>
            /// Fatal error caused compilation to not even occur.
            /// </summary>
            FatalError,
        }

        private CancellationTokenSource? _sharedCompileCts;
        internal readonly PropertyDictionary _store = new PropertyDictionary();

        internal abstract RequestLanguage Language { get; }

        public ManagedCompiler()
            : base(ErrorString.ResourceManager)
        {
            // If there is a crash, the runtime error is output to stderr and
            // we want MSBuild to print it out regardless of verbosity.
            LogStandardErrorAsError = true;
        }

        #region Properties

        // Please keep these alphabetized.
        public string[]? AdditionalLibPaths
        {
            set { _store[nameof(AdditionalLibPaths)] = value; }
            get { return (string[]?)_store[nameof(AdditionalLibPaths)]; }
        }

        public string[]? AddModules
        {
            set { _store[nameof(AddModules)] = value; }
            get { return (string[]?)_store[nameof(AddModules)]; }
        }

        public ITaskItem[]? AdditionalFiles
        {
            set { _store[nameof(AdditionalFiles)] = value; }
            get { return (ITaskItem[]?)_store[nameof(AdditionalFiles)]; }
        }

        public ITaskItem[]? EmbeddedFiles
        {
            set { _store[nameof(EmbeddedFiles)] = value; }
            get { return (ITaskItem[]?)_store[nameof(EmbeddedFiles)]; }
        }

        public bool EmbedAllSources
        {
            set { _store[nameof(EmbedAllSources)] = value; }
            get { return _store.GetOrDefault(nameof(EmbedAllSources), false); }
        }

        public ITaskItem[]? Analyzers
        {
            set { _store[nameof(Analyzers)] = value; }
            get { return (ITaskItem[]?)_store[nameof(Analyzers)]; }
        }

        // We do not support BugReport because it always requires user interaction,
        // which will cause a hang.

        public string? ChecksumAlgorithm
        {
            set { _store[nameof(ChecksumAlgorithm)] = value; }
            get { return (string?)_store[nameof(ChecksumAlgorithm)]; }
        }

        public string? CodeAnalysisRuleSet
        {
            set { _store[nameof(CodeAnalysisRuleSet)] = value; }
            get { return (string?)_store[nameof(CodeAnalysisRuleSet)]; }
        }

        public int CodePage
        {
            set { _store[nameof(CodePage)] = value; }
            get { return _store.GetOrDefault(nameof(CodePage), 0); }
        }

        [Output]
        public ITaskItem[]? CommandLineArgs
        {
            set { _store[nameof(CommandLineArgs)] = value; }
            get { return (ITaskItem[]?)_store[nameof(CommandLineArgs)]; }
        }

        public string? DebugType
        {
            set { _store[nameof(DebugType)] = value; }
            get { return (string?)_store[nameof(DebugType)]; }
        }

        public string? SourceLink
        {
            set { _store[nameof(SourceLink)] = value; }
            get { return (string?)_store[nameof(SourceLink)]; }
        }

        public string? DefineConstants
        {
            set { _store[nameof(DefineConstants)] = value; }
            get { return (string?)_store[nameof(DefineConstants)]; }
        }

        public bool DelaySign
        {
            set { _store[nameof(DelaySign)] = value; }
            get { return _store.GetOrDefault(nameof(DelaySign), false); }
        }

        public bool Deterministic
        {
            set { _store[nameof(Deterministic)] = value; }
            get { return _store.GetOrDefault(nameof(Deterministic), false); }
        }

        public bool PublicSign
        {
            set { _store[nameof(PublicSign)] = value; }
            get { return _store.GetOrDefault(nameof(PublicSign), false); }
        }

        public ITaskItem[]? AnalyzerConfigFiles
        {
            set { _store[nameof(AnalyzerConfigFiles)] = value; }
            get { return (ITaskItem[]?)_store[nameof(AnalyzerConfigFiles)]; }
        }

        public bool EmitDebugInformation
        {
            set { _store[nameof(EmitDebugInformation)] = value; }
            get { return _store.GetOrDefault(nameof(EmitDebugInformation), false); }
        }

        public string? ErrorLog
        {
            set { _store[nameof(ErrorLog)] = value; }
            get { return (string?)_store[nameof(ErrorLog)]; }
        }

        public string? Features
        {
            set { _store[nameof(Features)] = value; }
            get { return (string?)_store[nameof(Features)]; }
        }

        public int FileAlignment
        {
            set { _store[nameof(FileAlignment)] = value; }
            get { return _store.GetOrDefault(nameof(FileAlignment), 0); }
        }

        public string? GeneratedFilesOutputPath
        {
            set { _store[nameof(GeneratedFilesOutputPath)] = value; }
            get { return (string?)_store[nameof(GeneratedFilesOutputPath)]; }
        }

        public bool HighEntropyVA
        {
            set { _store[nameof(HighEntropyVA)] = value; }
            get { return _store.GetOrDefault(nameof(HighEntropyVA), false); }
        }

        /// <summary>
        /// Specifies the list of instrumentation kinds to be used during compilation.
        /// </summary>
        public string? Instrument
        {
            set { _store[nameof(Instrument)] = value; }
            get { return (string?)_store[nameof(Instrument)]; }
        }

        public string? KeyContainer
        {
            set { _store[nameof(KeyContainer)] = value; }
            get { return (string?)_store[nameof(KeyContainer)]; }
        }

        public string? KeyFile
        {
            set { _store[nameof(KeyFile)] = value; }
            get { return (string?)_store[nameof(KeyFile)]; }
        }

        public ITaskItem[]? LinkResources
        {
            set { _store[nameof(LinkResources)] = value; }
            get { return (ITaskItem[]?)_store[nameof(LinkResources)]; }
        }

        public string? MainEntryPoint
        {
            set { _store[nameof(MainEntryPoint)] = value; }
            get { return (string?)_store[nameof(MainEntryPoint)]; }
        }

        public bool NoConfig
        {
            set { _store[nameof(NoConfig)] = value; }
            get { return _store.GetOrDefault(nameof(NoConfig), false); }
        }

        public bool NoLogo
        {
            set { _store[nameof(NoLogo)] = value; }
            get { return _store.GetOrDefault(nameof(NoLogo), false); }
        }

        public bool NoWin32Manifest
        {
            set { _store[nameof(NoWin32Manifest)] = value; }
            get { return _store.GetOrDefault(nameof(NoWin32Manifest), false); }
        }

        public bool Optimize
        {
            set { _store[nameof(Optimize)] = value; }
            get { return _store.GetOrDefault(nameof(Optimize), false); }
        }

        [Output]
        public ITaskItem? OutputAssembly
        {
            set { _store[nameof(OutputAssembly)] = value; }
            get { return (ITaskItem?)_store[nameof(OutputAssembly)]; }
        }

        [Output]
        public ITaskItem? OutputRefAssembly
        {
            set { _store[nameof(OutputRefAssembly)] = value; }
            get { return (ITaskItem?)_store[nameof(OutputRefAssembly)]; }
        }

        public string? Platform
        {
            set { _store[nameof(Platform)] = value; }
            get { return (string?)_store[nameof(Platform)]; }
        }

        public ITaskItem[]? PotentialAnalyzerConfigFiles
        {
            set { _store[nameof(PotentialAnalyzerConfigFiles)] = value; }
            get { return (ITaskItem[]?)_store[nameof(PotentialAnalyzerConfigFiles)]; }
        }

        public bool Prefer32Bit
        {
            set { _store[nameof(Prefer32Bit)] = value; }
            get { return _store.GetOrDefault(nameof(Prefer32Bit), false); }
        }

        public string? ProjectName
        {
            set { _store[nameof(ProjectName)] = value; }
            get { return (string?)_store[nameof(ProjectName)]; }
        }

        public bool ProvideCommandLineArgs
        {
            set { _store[nameof(ProvideCommandLineArgs)] = value; }
            get { return _store.GetOrDefault(nameof(ProvideCommandLineArgs), false); }
        }

        public ITaskItem[]? References
        {
            set { _store[nameof(References)] = value; }
            get { return (ITaskItem[]?)_store[nameof(References)]; }
        }

        public bool RefOnly
        {
            set { _store[nameof(RefOnly)] = value; }
            get { return _store.GetOrDefault(nameof(RefOnly), false); }
        }

        public bool ReportAnalyzer
        {
            set { _store[nameof(ReportAnalyzer)] = value; }
            get { return _store.GetOrDefault(nameof(ReportAnalyzer), false); }
        }

        public ITaskItem[]? Resources
        {
            set { _store[nameof(Resources)] = value; }
            get { return (ITaskItem[]?)_store[nameof(Resources)]; }
        }

        public string? RuntimeMetadataVersion
        {
            set { _store[nameof(RuntimeMetadataVersion)] = value; }
            get { return (string?)_store[nameof(RuntimeMetadataVersion)]; }
        }

        public ITaskItem[]? ResponseFiles
        {
            set { _store[nameof(ResponseFiles)] = value; }
            get { return (ITaskItem[]?)_store[nameof(ResponseFiles)]; }
        }

        public string? SharedCompilationId
        {
            set { _store[nameof(SharedCompilationId)] = value; }
            get { return (string?)_store[nameof(SharedCompilationId)]; }
        }

        public bool SkipAnalyzers
        {
            set { _store[nameof(SkipAnalyzers)] = value; }
            get { return _store.GetOrDefault(nameof(SkipAnalyzers), false); }
        }

        public bool SkipCompilerExecution
        {
            set { _store[nameof(SkipCompilerExecution)] = value; }
            get { return _store.GetOrDefault(nameof(SkipCompilerExecution), false); }
        }

        public ITaskItem[]? Sources
        {
            set
            {
                if (UsedCommandLineTool)
                {
                    NormalizePaths(value);
                }

                _store[nameof(Sources)] = value;
            }
            get { return (ITaskItem[]?)_store[nameof(Sources)]; }
        }

        public string? SubsystemVersion
        {
            set { _store[nameof(SubsystemVersion)] = value; }
            get { return (string?)_store[nameof(SubsystemVersion)]; }
        }

        public string? TargetFramework
        {
            set { _store[nameof(TargetFramework)] = value; }
            get { return (string?)_store[nameof(TargetFramework)]; }
        }

        public string? TargetType
        {
            set
            {
                _store[nameof(TargetType)] = value != null
                    ? CultureInfo.InvariantCulture.TextInfo.ToLower(value)
                    : null;
            }
            get { return (string?)_store[nameof(TargetType)]; }
        }

        public bool TreatWarningsAsErrors
        {
            set { _store[nameof(TreatWarningsAsErrors)] = value; }
            get { return _store.GetOrDefault(nameof(TreatWarningsAsErrors), false); }
        }

        public bool Utf8Output
        {
            set { _store[nameof(Utf8Output)] = value; }
            get { return _store.GetOrDefault(nameof(Utf8Output), false); }
        }

        public string? Win32Icon
        {
            set { _store[nameof(Win32Icon)] = value; }
            get { return (string?)_store[nameof(Win32Icon)]; }
        }

        public string? Win32Manifest
        {
            set { _store[nameof(Win32Manifest)] = value; }
            get { return (string?)_store[nameof(Win32Manifest)]; }
        }

        public string? Win32Resource
        {
            set { _store[nameof(Win32Resource)] = value; }
            get { return (string?)_store[nameof(Win32Resource)]; }
        }

        public string? PathMap
        {
            set { _store[nameof(PathMap)] = value; }
            get { return (string?)_store[nameof(PathMap)]; }
        }

        /// <summary>
        /// If this property is true then the task will take every C# or VB
        /// compilation which is queued by MSBuild and send it to the
        /// VBCSCompiler server instance, starting a new instance if necessary.
        /// If false, we will use the values from ToolPath/Exe.
        /// </summary>
        public bool UseSharedCompilation
        {
            set { _store[nameof(UseSharedCompilation)] = value; }
            get { return _store.GetOrDefault(nameof(UseSharedCompilation), false); }
        }

        // Map explicit platform of "AnyCPU" or the default platform (null or ""), since it is commonly understood in the
        // managed build process to be equivalent to "AnyCPU", to platform "AnyCPU32BitPreferred" if the Prefer32Bit
        // property is set.
        internal string? PlatformWith32BitPreference
        {
            get
            {
                string? platform = Platform;
                if ((RoslynString.IsNullOrEmpty(platform) || platform.Equals("anycpu", StringComparison.OrdinalIgnoreCase)) && Prefer32Bit)
                {
                    platform = "anycpu32bitpreferred";
                }
                return platform;
            }
        }

        /// <summary>
        /// Overridable property specifying the encoding of the captured task standard output stream
        /// </summary>
        protected override Encoding StandardOutputEncoding
        {
            get
            {
                return (Utf8Output) ? Encoding.UTF8 : base.StandardOutputEncoding;
            }
        }

        public string? LangVersion
        {
            set { _store[nameof(LangVersion)] = value; }
            get { return (string?)_store[nameof(LangVersion)]; }
        }

        public bool ReportIVTs
        {
            set { _store[nameof(ReportIVTs)] = value; }
            get { return _store.GetOrDefault(nameof(ReportIVTs), false); }
        }

        #endregion

        /// <summary>
        /// Method for testing only
        /// </summary>
        public string GeneratePathToTool()
        {
            return GenerateFullPathToTool();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            using var logger = new CompilerServerLogger($"MSBuild {Process.GetCurrentProcess().Id}");
            return ExecuteTool(pathToTool, responseFileCommands, commandLineCommands, logger);
        }

        internal int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands, ICompilerServerLogger logger)
        {
            if (ProvideCommandLineArgs)
            {
                CommandLineArgs = GenerateCommandLineArgsTaskItems(responseFileCommands);
            }

            if (SkipCompilerExecution)
            {
                return 0;
            }

            try
            {
                var requestId = getRequestId();
                logger.Log($"Compilation request {requestId}, PathToTool={pathToTool}");

                string? tempDirectory = Path.GetTempPath();

                if (!UseSharedCompilation ||
                    !IsManagedTool ||
                    !BuildServerConnection.IsCompilerServerSupported)
                {
                    LogCompilationMessage(logger, requestId, CompilationKind.Tool, $"using command line tool by design '{pathToTool}'");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
                }

                _sharedCompileCts = new CancellationTokenSource();
                logger.Log($"CommandLine = '{commandLineCommands}'");
                logger.Log($"BuildResponseFile = '{responseFileCommands}'");

                var clientDirectory = Path.GetDirectoryName(PathToManagedTool);
                if (clientDirectory is null || tempDirectory is null)
                {
                    LogCompilationMessage(logger, requestId, CompilationKind.Tool, $"using command line tool because we could not find client or temp directory '{PathToManagedTool}'");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
                }

                // Note: using ToolArguments here (the property) since
                // commandLineCommands (the parameter) may have been mucked with
                // (to support using the dotnet cli)
                var buildRequest = BuildServerConnection.CreateBuildRequest(
                    requestId,
                    Language,
                    GenerateCommandLineArgsList(responseFileCommands),
                    workingDirectory: CurrentDirectoryToUse(),
                    tempDirectory: tempDirectory,
                    keepAlive: null,
                    libDirectory: LibDirectoryToUse());

                var pipeName = !string.IsNullOrEmpty(SharedCompilationId)
                    ? SharedCompilationId
                    : BuildServerConnection.GetPipeName(clientDirectory);

                var responseTask = BuildServerConnection.RunServerBuildRequestAsync(
                    buildRequest,
                    pipeName,
                    clientDirectory,
                    logger: logger,
                    cancellationToken: _sharedCompileCts.Token);

                responseTask.Wait(_sharedCompileCts.Token);

                ExitCode = HandleResponse(requestId, responseTask.Result, pathToTool, responseFileCommands, commandLineCommands, logger);
            }
            catch (OperationCanceledException)
            {
                ExitCode = 0;
            }
            catch (Exception e)
            {
                Log.LogErrorWithCodeFromResources("Compiler_UnexpectedException");
                Log.LogErrorFromException(e);
                ExitCode = -1;
            }
            finally
            {
                _sharedCompileCts?.Dispose();
                _sharedCompileCts = null;
            }

            return ExitCode;

            // Construct the friendly name for the compilation. This does not need to be unique. Instead
            // it's used by developers to understand what compilation is running on the server.
            string getRequestId()
            {
                if (!string.IsNullOrEmpty(ProjectName))
                {
                    return string.IsNullOrEmpty(TargetFramework)
                        ? ProjectName
                        : $"{ProjectName} ({TargetFramework})";
                }

                return $"Unnamed compilation {Guid.NewGuid()}";
            }
        }

        /// <summary>
        /// Cancel the in-process build task.
        /// </summary>
        public override void Cancel()
        {
            // This must be cancelled first. Otherwise we risk that MSBuild cancellation logic will take down
            // our pipes and tasks in an order we're not expecting.
            _sharedCompileCts?.Cancel();

            base.Cancel();
        }

        /// <summary>
        /// Get the current directory that the compiler should run in.
        /// </summary>
        private string CurrentDirectoryToUse()
        {
            // ToolTask has a method for this. But it may return null. Use the process directory
            // if ToolTask didn't override. MSBuild uses the process directory.
            string workingDirectory = GetWorkingDirectory();
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }
            return workingDirectory;
        }

        /// <summary>
        /// Get the "LIB" environment variable, or NULL if none.
        /// </summary>
        private string? LibDirectoryToUse()
        {
            // First check the real environment.
            string? libDirectory = Environment.GetEnvironmentVariable("LIB");

            // Now go through additional environment variables.
            string[] additionalVariables = EnvironmentVariables;
            if (additionalVariables != null)
            {
                foreach (string var in EnvironmentVariables)
                {
                    if (var.StartsWith("LIB=", StringComparison.OrdinalIgnoreCase))
                    {
                        libDirectory = var.Substring(4);
                    }
                }
            }

            return libDirectory;
        }

        /// <summary>
        /// The return code of the compilation. Strangely, this isn't overridable from ToolTask, so we need
        /// to create our own.
        /// </summary>
        [Output]
        public new int ExitCode { get; private set; }

        /// <summary>
        /// Handle a response from the server, reporting messages and returning
        /// the appropriate exit code.
        /// </summary>
        private int HandleResponse(string requestId, BuildResponse? response, string pathToTool, string responseFileCommands, string commandLineCommands, ICompilerServerLogger logger)
        {
#if BOOTSTRAP
            if (!ValidateBootstrapResponse(response))
            {
                return 1;
            }
#endif

            if (response is null)
            {
                LogCompilationMessage(logger, requestId, CompilationKind.ToolFallback, "could not launch server");
                return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }

            switch (response.Type)
            {
                case BuildResponse.ResponseType.Completed:
                    var completedResponse = (CompletedBuildResponse)response;
                    LogCompilerOutput(completedResponse.Output, StandardOutputImportanceToUse);
                    LogCompilationMessage(logger, requestId, CompilationKind.Server, "server processed compilation");
                    return completedResponse.ReturnCode;

                case BuildResponse.ResponseType.MismatchedVersion:
                    LogCompilationMessage(logger, requestId, CompilationKind.FatalError, "server reports different protocol version than build task");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

                case BuildResponse.ResponseType.IncorrectHash:
                    LogCompilationMessage(logger, requestId, CompilationKind.FatalError, "server reports different hash version than build task");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

                case BuildResponse.ResponseType.CannotConnect:
                    LogCompilationMessage(logger, requestId, CompilationKind.ToolFallback, $"cannot connect to the server");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

                case BuildResponse.ResponseType.Rejected:
                    var rejectedResponse = (RejectedBuildResponse)response;
                    LogCompilationMessage(logger, requestId, CompilationKind.ToolFallback, $"server rejected the request '{rejectedResponse.Reason}'");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

                case BuildResponse.ResponseType.AnalyzerInconsistency:
                    var analyzerResponse = (AnalyzerInconsistencyBuildResponse)response;
                    var combinedMessage = string.Join(", ", analyzerResponse.ErrorMessages.ToArray());
                    LogCompilationMessage(logger, requestId, CompilationKind.ToolFallback, $"server rejected the request due to analyzer / generator issues '{combinedMessage}'");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

                default:
                    LogCompilationMessage(logger, requestId, CompilationKind.ToolFallback, $"server gave an unrecognized response");
                    return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }
        }

#if BOOTSTRAP
#pragma warning disable IDE0044
        /// <summary>
        /// Keeps track of the number of times the task failed to connect to the compiler 
        /// server. Even in valid builds this can be greater than zero (connect is
        /// inherently a race condition). If this gets too high though in a bootstrap build
        /// it's evidence of a bigger issue the team should be looking at.
        /// </summary>
        private static int s_connectFailedCount;

#pragma warning restore IDE0044


        /// <summary>
        /// In bootstrap builds this validates the response. When this returns false it 
        /// indicates the bootstrap build is incorrect and the compilation should fail.
        /// </summary>
        private bool ValidateBootstrapResponse(BuildResponse? response)
        {
            // This represents the maximum number of failed connection attempts on the server before we will declare
            // that the overall build itself failed. Keeping this at zero is not realistic because even in a fully
            // functioning server connection failures are expected. The server could be too busy to accept connections
            // fast enough. Anything above this count though is considered worth investigating by the compiler team.
            //
            const int maxCannotConnectCount = 2;

            var responseType = response?.Type ?? BuildResponse.ResponseType.CannotConnect;
            switch (responseType)
            {
                case BuildResponse.ResponseType.AnalyzerInconsistency:
                    Log.LogError($"Analyzer inconsistency building");
                    return false;
                case BuildResponse.ResponseType.MismatchedVersion:
                case BuildResponse.ResponseType.IncorrectHash:
                    Log.LogError($"Critical error {responseType} when building");
                    return false;
                case BuildResponse.ResponseType.Rejected:
                    Log.LogError($"Compiler request rejected: {((RejectedBuildResponse)response!).Reason}");
                    return false;
                case BuildResponse.ResponseType.CannotConnect:
                    if (Interlocked.Increment(ref s_connectFailedCount) > maxCannotConnectCount)
                    {
                        Log.LogError("Too many errors connecting to the server");
                        return false;
                    }
                    return true;

                case BuildResponse.ResponseType.Completed:
                case BuildResponse.ResponseType.Shutdown:
                    // Expected messages
                    break;
                default:
                    Log.LogError($"Unexpected response type {responseType}");
                    return false;
            }

            return true;
        }
#endif

        /// <summary>
        /// Log the compiler output to MSBuild. Each language will override this to parse their output and log it
        /// in the language specific manner. This often involves parsing the raw output and formatting it as 
        /// individual messages for MSBuild.
        /// </summary>
        /// <remarks>
        /// Internal for testing only.
        /// </remarks>
        internal abstract void LogCompilerOutput(string output, MessageImportance messageImportance);

        /// <summary>
        /// Used to log a message that should go into both the compiler server log as well as the MSBuild logs
        ///
        /// These are intended to be processed by automation in the binlog hence do not change the structure of
        /// the messages here.
        /// </summary>
        private void LogCompilationMessage(ICompilerServerLogger logger, string requestId, CompilationKind kind, string diagnostic)
        {
            var category = kind switch
            {
                CompilationKind.Server => "server",
                CompilationKind.Tool => "tool",
                CompilationKind.ToolFallback => "server failed",
                CompilationKind.FatalError => "fatal error",
                _ => throw new Exception($"Unexpected value {kind}"),
            };

            var message = $"CompilerServer: {category} - {diagnostic} - {requestId}";
            if (kind == CompilationKind.FatalError)
            {
                logger.LogError(message);
                Log.LogError(message);
            }
            else
            {
                logger.Log(message);
                Log.LogMessage(message);
            }
        }

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can't go into a response file and
        /// must go directly onto the command line.
        /// </summary>
        protected override void AddCommandLineCommands(CommandLineBuilderExtension commandLine)
        {
            commandLine.AppendWhenTrue("/noconfig", _store, nameof(NoConfig));
        }

        /// <summary>
        /// Fills the provided CommandLineBuilderExtension with those switches and other information that can go into a response file.
        /// </summary>
        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            // If outputAssembly is not specified, then an "/out: <name>" option won't be added to
            // overwrite the one resulting from the OutputAssembly member of the CompilerParameters class.
            // In that case, we should set the outputAssembly member based on the first source file.
            if (
                    (OutputAssembly == null) &&
                    (Sources != null) &&
                    (Sources.Length > 0) &&
                    (ResponseFiles == null)    // The response file may already have a /out: switch in it, so don't try to be smart here.
                )
            {
                try
                {
                    OutputAssembly = new TaskItem(Path.GetFileNameWithoutExtension(Sources[0].ItemSpec));
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException(e.Message, "Sources");
                }
                if (string.Compare(TargetType, "library", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    OutputAssembly.ItemSpec += ".dll";
                }
                else if (string.Compare(TargetType, "module", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    OutputAssembly.ItemSpec += ".netmodule";
                }
                else
                {
                    OutputAssembly.ItemSpec += ".exe";
                }
            }

            commandLine.AppendSwitchIfNotNull("/addmodule:", AddModules, ",");
            commandLine.AppendSwitchWithInteger("/codepage:", _store, nameof(CodePage));

            ConfigureDebugProperties();

            // The "DebugType" parameter should be processed after the "EmitDebugInformation" parameter
            // because it's more specific.  Order matters on the command-line, and the last one wins.
            // /debug+ is just a shorthand for /debug:full.  And /debug- is just a shorthand for /debug:none.

            commandLine.AppendPlusOrMinusSwitch("/debug", _store, nameof(EmitDebugInformation));
            commandLine.AppendSwitchIfNotNull("/debug:", DebugType);

            commandLine.AppendPlusOrMinusSwitch("/delaysign", _store, nameof(DelaySign));

            commandLine.AppendWhenTrue("/reportivts", _store, nameof(ReportIVTs));

            commandLine.AppendSwitchWithInteger("/filealign:", _store, nameof(FileAlignment));
            commandLine.AppendSwitchIfNotNull("/generatedfilesout:", GeneratedFilesOutputPath);
            commandLine.AppendSwitchIfNotNull("/keycontainer:", KeyContainer);
            commandLine.AppendSwitchIfNotNull("/keyfile:", KeyFile);
            // If the strings "LogicalName" or "Access" ever change, make sure to search/replace everywhere in vsproject.
            commandLine.AppendSwitchIfNotNull("/linkresource:", LinkResources, new string[] { "LogicalName", "Access" });
            commandLine.AppendWhenTrue("/nologo", _store, nameof(NoLogo));
            commandLine.AppendWhenTrue("/nowin32manifest", _store, nameof(NoWin32Manifest));
            commandLine.AppendPlusOrMinusSwitch("/optimize", _store, nameof(Optimize));
            commandLine.AppendSwitchIfNotNull("/pathmap:", PathMap);
            commandLine.AppendSwitchIfNotNull("/out:", OutputAssembly);
            commandLine.AppendSwitchIfNotNull("/refout:", OutputRefAssembly);
            commandLine.AppendWhenTrue("/refonly", _store, nameof(RefOnly));
            commandLine.AppendSwitchIfNotNull("/ruleset:", CodeAnalysisRuleSet);
            commandLine.AppendSwitchIfNotNull("/errorlog:", ErrorLog);
            commandLine.AppendSwitchIfNotNull("/subsystemversion:", SubsystemVersion);
            commandLine.AppendWhenTrue("/reportanalyzer", _store, nameof(ReportAnalyzer));
            // If the strings "LogicalName" or "Access" ever change, make sure to search/replace everywhere in vsproject.
            commandLine.AppendSwitchIfNotNull("/resource:", Resources, new string[] { "LogicalName", "Access" });
            commandLine.AppendSwitchIfNotNull("/target:", TargetType);
            commandLine.AppendPlusOrMinusSwitch("/warnaserror", _store, nameof(TreatWarningsAsErrors));
            commandLine.AppendWhenTrue("/utf8output", _store, nameof(Utf8Output));
            commandLine.AppendSwitchIfNotNull("/win32icon:", Win32Icon);
            commandLine.AppendSwitchIfNotNull("/win32manifest:", Win32Manifest);

            AddResponseFileCommandsForSwitchesSinceInitialReleaseThatAreNeededByTheHost(commandLine);
            AddAnalyzersToCommandLine(commandLine, Analyzers);
            AddAdditionalFilesToCommandLine(commandLine);

            // Append the sources.
            commandLine.AppendFileNamesIfNotNull(Sources, " ");
        }

        internal void AddResponseFileCommandsForSwitchesSinceInitialReleaseThatAreNeededByTheHost(CommandLineBuilderExtension commandLine)
        {
            commandLine.AppendPlusOrMinusSwitch("/deterministic", _store, nameof(Deterministic));
            commandLine.AppendPlusOrMinusSwitch("/publicsign", _store, nameof(PublicSign));
            commandLine.AppendSwitchIfNotNull("/runtimemetadataversion:", RuntimeMetadataVersion);
            commandLine.AppendSwitchIfNotNull("/checksumalgorithm:", ChecksumAlgorithm);
            commandLine.AppendSwitchWithSplitting("/instrument:", Instrument, ",", ';', ',');
            commandLine.AppendSwitchIfNotNull("/sourcelink:", SourceLink);
            commandLine.AppendSwitchIfNotNull("/langversion:", LangVersion);
            commandLine.AppendPlusOrMinusSwitch("/skipanalyzers", _store, nameof(SkipAnalyzers));

            AddFeatures(commandLine, Features);
            AddEmbeddedFilesToCommandLine(commandLine);
            AddAnalyzerConfigFilesToCommandLine(commandLine);
        }

        /// <summary>
        /// Adds a "/features:" switch to the command line for each provided feature.
        /// </summary>
        internal static void AddFeatures(CommandLineBuilderExtension commandLine, string? features)
        {
            if (RoslynString.IsNullOrEmpty(features))
            {
                return;
            }

            foreach (var feature in CompilerOptionParseUtilities.ParseFeatureFromMSBuild(features))
            {
                commandLine.AppendSwitchIfNotNull("/features:", feature.Trim());
            }
        }

        /// <summary>
        /// Adds a "/analyzer:" switch to the command line for each provided analyzer.
        /// </summary>
        internal static void AddAnalyzersToCommandLine(CommandLineBuilderExtension commandLine, ITaskItem[]? analyzers)
        {
            // If there were no analyzers passed in, don't add any /analyzer: switches
            // on the command-line.
            if (analyzers == null)
            {
                return;
            }

            foreach (ITaskItem analyzer in analyzers)
            {
                commandLine.AppendSwitchIfNotNull("/analyzer:", analyzer.ItemSpec);
            }
        }

        /// <summary>
        /// Adds a "/additionalfile:" switch to the command line for each additional file.
        /// </summary>
        private void AddAdditionalFilesToCommandLine(CommandLineBuilderExtension commandLine)
        {
            if (AdditionalFiles != null)
            {
                foreach (ITaskItem additionalFile in AdditionalFiles)
                {
                    commandLine.AppendSwitchIfNotNull("/additionalfile:", additionalFile.ItemSpec);
                }
            }
        }

        /// <summary>
        /// Adds a "/embed:" switch to the command line for each pdb embedded file.
        /// </summary>
        private void AddEmbeddedFilesToCommandLine(CommandLineBuilderExtension commandLine)
        {
            if (EmbedAllSources)
            {
                commandLine.AppendSwitch("/embed");
            }

            if (EmbeddedFiles != null)
            {
                foreach (ITaskItem embeddedFile in EmbeddedFiles)
                {
                    commandLine.AppendSwitchIfNotNull("/embed:", embeddedFile.ItemSpec);
                }
            }
        }

        /// <summary>
        /// Adds a "/editorconfig:" switch to the command line for each .editorconfig file.
        /// </summary>
        private void AddAnalyzerConfigFilesToCommandLine(CommandLineBuilderExtension commandLine)
        {
            if (AnalyzerConfigFiles != null)
            {
                foreach (ITaskItem analyzerConfigFile in AnalyzerConfigFiles)
                {
                    commandLine.AppendSwitchIfNotNull("/analyzerconfig:", analyzerConfigFile.ItemSpec);
                }
            }
        }

        /// <summary>
        /// Configure the debug switches which will be placed on the compiler command-line.
        /// The matrix of debug type and symbol inputs and the desired results is as follows:
        ///
        /// Debug Symbols              DebugType   Desired Results
        ///          True               Full        /debug+ /debug:full
        ///          True               PdbOnly     /debug+ /debug:PdbOnly
        ///          True               None        /debug-
        ///          True               Blank       /debug+
        ///          False              Full        /debug- /debug:full
        ///          False              PdbOnly     /debug- /debug:PdbOnly
        ///          False              None        /debug-
        ///          False              Blank       /debug-
        ///          Blank              Full                /debug:full
        ///          Blank              PdbOnly             /debug:PdbOnly
        ///          Blank              None        /debug-
        /// Debug:   Blank              Blank       /debug+ //Microsoft.common.targets will set this
        /// Release: Blank              Blank       "Nothing for either switch"
        ///
        /// The logic is as follows:
        /// If debugtype is none  set debugtype to empty and debugSymbols to false
        /// If debugType is blank  use the debugsymbols "as is"
        /// If debug type is set, use its value and the debugsymbols value "as is"
        /// </summary>
        private void ConfigureDebugProperties()
        {
            // If debug type is set we need to take some action depending on the value. If debugtype is not set
            // We don't need to modify the EmitDebugInformation switch as its value will be used as is.
            if (_store[nameof(DebugType)] != null)
            {
                // If debugtype is none then only show debug- else use the debug type and the debugsymbols as is.
                if (string.Compare((string?)_store[nameof(DebugType)], "none", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _store[nameof(DebugType)] = null;
                    _store[nameof(EmitDebugInformation)] = false;
                }
            }
        }

        /// <summary>
        /// Allows tool to handle the return code.
        /// This method will only be called with non-zero exitCode.
        /// </summary>
        protected override bool HandleTaskExecutionErrors()
        {
            // For managed compilers, the compiler should emit the appropriate
            // error messages before returning a non-zero exit code, so we don't
            // normally need to emit any additional messages now.
            //
            // If somehow the compiler DID return a non-zero exit code and didn't log an error, we'd like to log that exit code.
            // We can only do this for the command line compiler: if the inproc compiler was used,
            // we can't tell what if anything it logged as it logs directly to Visual Studio's output window.
            //
            if (!Log.HasLoggedErrors && UsedCommandLineTool)
            {
                // This will log a message "MSB3093: The command exited with code {0}."
                base.HandleTaskExecutionErrors();
            }

            return false;
        }

        /// <summary>
        /// Takes a list of files and returns the normalized locations of these files
        /// </summary>
        private void NormalizePaths(ITaskItem[]? taskItems)
        {
            if (taskItems is null)
                return;

            foreach (var item in taskItems)
            {
                item.ItemSpec = Utilities.GetFullPathNoThrow(item.ItemSpec);
            }
        }

        /// <summary>
        /// Whether the command line compiler was invoked, instead
        /// of the host object compiler.
        /// </summary>
        protected bool UsedCommandLineTool
        {
            get;
            set;
        }

        private bool _hostCompilerSupportsAllParameters;
        protected bool HostCompilerSupportsAllParameters
        {
            get { return _hostCompilerSupportsAllParameters; }
            set { _hostCompilerSupportsAllParameters = value; }
        }

        /// <summary>
        /// Checks the bool result from calling one of the methods on the host compiler object to
        /// set one of the parameters.  If it returned false, that means the host object doesn't
        /// support a particular parameter or variation on a parameter.  So we log a comment,
        /// and set our state so we know not to call the host object to do the actual compilation.
        /// </summary>
        protected void CheckHostObjectSupport
            (
            string parameterName,
            bool resultFromHostObjectSetOperation
            )
        {
            if (!resultFromHostObjectSetOperation)
            {
                Log.LogMessageFromResources(MessageImportance.Normal, "General_ParameterUnsupportedOnHostCompiler", parameterName);
                _hostCompilerSupportsAllParameters = false;
            }
        }

        internal void InitializeHostObjectSupportForNewSwitches(ITaskHost hostObject, ref string param)
        {
            var compilerOptionsHostObject = hostObject as ICompilerOptionsHostObject;

            if (compilerOptionsHostObject != null)
            {
                var commandLineBuilder = new CommandLineBuilderExtension();
                AddResponseFileCommandsForSwitchesSinceInitialReleaseThatAreNeededByTheHost(commandLineBuilder);
                param = "CompilerOptions";
                CheckHostObjectSupport(param, compilerOptionsHostObject.SetCompilerOptions(commandLineBuilder.ToString()));
            }
        }

        /// <summary>
        /// Checks to see whether all of the passed-in references exist on disk before we launch the compiler.
        /// </summary>
        protected bool CheckAllReferencesExistOnDisk()
        {
            if (null == References)
            {
                // No references
                return true;
            }

            bool success = true;

            foreach (ITaskItem reference in References)
            {
                if (!File.Exists(reference.ItemSpec))
                {
                    success = false;
                    Log.LogErrorWithCodeFromResources("General_ReferenceDoesNotExist", reference.ItemSpec);
                }
            }

            return success;
        }

        /// <summary>
        /// The IDE and command line compilers unfortunately differ in how win32
        /// manifests are specified.  In particular, the command line compiler offers a
        /// "/nowin32manifest" switch, while the IDE compiler does not offer analogous
        /// functionality. If this switch is omitted from the command line and no win32
        /// manifest is specified, the compiler will include a default win32 manifest
        /// named "default.win32manifest" found in the same directory as the compiler
        /// executable. Again, the IDE compiler does not offer analogous support.
        ///
        /// We'd like to imitate the command line compiler's behavior in the IDE, but
        /// it isn't aware of the default file, so we must compute the path to it if
        /// noDefaultWin32Manifest is false and no win32Manifest was provided by the
        /// project.
        ///
        /// This method will only be called during the initialization of the host object,
        /// which is only used during IDE builds.
        /// </summary>
        /// <returns>the path to the win32 manifest to provide to the host object</returns>
        internal string? GetWin32ManifestSwitch
        (
            bool noDefaultWin32Manifest,
            string? win32Manifest
        )
        {
            if (!noDefaultWin32Manifest)
            {
                if (string.IsNullOrEmpty(win32Manifest) && string.IsNullOrEmpty(Win32Resource))
                {
                    // We only want to consider the default.win32manifest if this is an executable
                    if (!string.Equals(TargetType, "library", StringComparison.OrdinalIgnoreCase)
                       && !string.Equals(TargetType, "module", StringComparison.OrdinalIgnoreCase))
                    {
                        // We need to compute the path to the default win32 manifest
                        string pathToDefaultManifest = ToolLocationHelper.GetPathToDotNetFrameworkFile
                                                       (
                                                           "default.win32manifest",

                                                           // We are choosing to pass Version46 instead of VersionLatest. TargetDotNetFrameworkVersion
                                                           // is an enum, and VersionLatest is not some sentinel value but rather a constant that is
                                                           // equal to the highest version defined in the enum. Enum values, being constants, are baked
                                                           // into consuming assembly, so specifying VersionLatest means not the latest version wherever
                                                           // this code is running, but rather the latest version of the framework according to the
                                                           // reference assembly with which this assembly was built. As of this writing, we are building
                                                           // our bits on machines with Visual Studio 2015 that know about 4.6.1, so specifying
                                                           // VersionLatest would bake in the enum value for 4.6.1. But we need to run on machines with
                                                           // MSBuild that only know about Version46 (and no higher), so VersionLatest will fail there.
                                                           // Explicitly passing Version46 prevents this problem.
                                                           TargetDotNetFrameworkVersion.Version46
                                                       );

                        if (null == pathToDefaultManifest)
                        {
                            // This is rather unlikely, and the inproc compiler seems to log an error anyway.
                            // So just a message is fine.
                            Log.LogMessageFromResources
                            (
                                "General_ExpectedFileMissing",
                                "default.win32manifest"
                            );
                        }

                        return pathToDefaultManifest;
                    }
                }
            }

            return win32Manifest;
        }
    }
}
