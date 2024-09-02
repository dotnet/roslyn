// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Scripting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        /// <summary>
        /// A remote singleton server-activated object that lives in the interactive host process and controls it.
        /// </summary>
        internal sealed class Service : IDisposable
        {
            private static readonly ManualResetEventSlim s_clientExited = new ManualResetEventSlim(false);

            private readonly Func<Func<object>, object> _invokeOnMainThread;

            private ServiceState? _serviceState;

            // Session is not thread-safe by itself, and the compilation
            // and execution of scripts are asynchronous operations.
            // However since the operations are executed serially, it
            // is sufficient to lock when creating the async tasks.
            private readonly object _lastTaskGuard = new object();
            private Task<EvaluationState> _lastTask;

            private static readonly InteractiveHostPlatformInfo s_currentPlatformInfo = InteractiveHostPlatformInfo.GetCurrentPlatformInfo();

            private sealed class ServiceState : IDisposable
            {
                public readonly InteractiveAssemblyLoader AssemblyLoader;
                public readonly MetadataShadowCopyProvider MetadataFileProvider;
                public readonly ReplServiceProvider ReplServiceProvider;
                public readonly InteractiveScriptGlobals Globals;

                public ServiceState(InteractiveAssemblyLoader assemblyLoader, MetadataShadowCopyProvider metadataFileProvider, ReplServiceProvider replServiceProvider, InteractiveScriptGlobals globals)
                {
                    AssemblyLoader = assemblyLoader;
                    MetadataFileProvider = metadataFileProvider;
                    ReplServiceProvider = replServiceProvider;
                    Globals = globals;
                }

                public void Dispose()
                    => MetadataFileProvider.Dispose();
            }

            private readonly struct EvaluationState
            {
                internal readonly ImmutableArray<string> SourceSearchPaths;
                internal readonly string WorkingDirectory;
                internal readonly ScriptState<object>? ScriptState;
                internal readonly ScriptOptions ScriptOptions;

                internal EvaluationState(
                    ScriptState<object>? scriptState,
                    ScriptOptions scriptOptions,
                    ImmutableArray<string> sourceSearchPaths,
                    string workingDirectory)
                {
                    Debug.Assert(!sourceSearchPaths.IsDefault);
                    Debug.Assert(scriptOptions.MetadataResolver is ScriptMetadataResolver);

                    ScriptState = scriptState;
                    ScriptOptions = scriptOptions;
                    SourceSearchPaths = sourceSearchPaths;
                    WorkingDirectory = workingDirectory;
                }

                public ScriptMetadataResolver MetadataReferenceResolver
                    => (ScriptMetadataResolver)ScriptOptions.MetadataResolver;

                internal EvaluationState WithScriptState(ScriptState<object> state)
                {
                    return new EvaluationState(
                        state,
                        ScriptOptions,
                        SourceSearchPaths,
                        WorkingDirectory);
                }

                internal EvaluationState WithOptions(ScriptOptions options)
                {
                    return new EvaluationState(
                        ScriptState,
                        options,
                        SourceSearchPaths,
                        WorkingDirectory);
                }
            }

            private static readonly ImmutableArray<string> s_systemNoShadowCopyDirectories = ImmutableArray.Create(
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
                FileUtilities.NormalizeDirectoryPath(RuntimeEnvironment.GetRuntimeDirectory()));

            #region Setup

            private Service(Func<Func<object>, object> invokeOnMainThread)
            {
                _invokeOnMainThread = invokeOnMainThread;

                // The initial working directory is set when the process is created.
                var workingDirectory = Directory.GetCurrentDirectory();

                var referenceResolver = new RuntimeMetadataReferenceResolver(
                    searchPaths: ImmutableArray<string>.Empty,
                    baseDirectory: workingDirectory,
                    packageResolver: null,
                    gacFileResolver: s_currentPlatformInfo.HasGlobalAssemblyCache ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                    s_currentPlatformInfo.PlatformAssemblyPaths,
                    (path, properties) => new ShadowCopyReference(GetServiceState().MetadataFileProvider, path, properties));

                var initialState = new EvaluationState(
                    scriptState: null,
                    scriptOptions: ScriptOptions.Default.WithMetadataResolver(new ScriptMetadataResolver(referenceResolver)),
                    ImmutableArray<string>.Empty,
                    workingDirectory);

                _lastTask = Task.FromResult(initialState);

                Console.OutputEncoding = OutputEncoding;

                // We want to be sure to delete the shadow-copied files when the process goes away. Frankly
                // there's nothing we can do if the process is forcefully quit or goes down in a completely
                // uncontrolled manner (like a stack overflow). When the process goes down in a controlled
                // manned, we should generally expect this event to be called.
                AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
            }

            private void HandleProcessExit(object sender, EventArgs e)
            {
                Dispose();
                AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
            }

            public void Dispose()
            {
                _serviceState?.Dispose();
                _serviceState = null;
            }

            public Task<InteractiveHostPlatformInfo.Data> InitializeAsync(string replServiceProviderTypeName)
            {
                // TODO (tomat): we should share the copied files with the host
                var metadataFileProvider = new MetadataShadowCopyProvider(
                    Path.Combine(Path.GetTempPath(), "InteractiveHostShadow"),
                    noShadowCopyDirectories: s_systemNoShadowCopyDirectories,
                    documentationCommentsCulture: CultureInfo.CurrentUICulture);

                var assemblyLoader = new InteractiveAssemblyLoader(metadataFileProvider);
                var replServiceProviderType = Type.GetType(replServiceProviderTypeName);
                var replServiceProvider = (ReplServiceProvider)Activator.CreateInstance(replServiceProviderType);
                var globals = new InteractiveScriptGlobals(Console.Out, replServiceProvider.ObjectFormatter);

                _serviceState = new ServiceState(assemblyLoader, metadataFileProvider, replServiceProvider, globals);

                return Task.FromResult(s_currentPlatformInfo.Serialize());
            }

            private ServiceState GetServiceState()
            {
                Contract.ThrowIfNull(_serviceState, "Service not initialized");
                return _serviceState;
            }

            private SourceReferenceResolver CreateSourceReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
            {
                return new SourceFileResolver(searchPaths, baseDirectory);
            }

            private static bool AttachToClientProcess(int clientProcessId)
            {
                Process clientProcess;
                try
                {
                    clientProcess = Process.GetProcessById(clientProcessId);
                }
                catch (ArgumentException)
                {
                    return false;
                }

                clientProcess.EnableRaisingEvents = true;
                clientProcess.Exited += new EventHandler((_, __) => s_clientExited.Set());

                return clientProcess.IsAlive();
            }

            // for testing purposes
            public void EmulateClientExit()
            {
                s_clientExited.Set();
            }

            /// <summary>
            /// Implements remote server.
            /// </summary>
            public static async Task RunServerAsync(string pipeName, int clientProcessId, Func<Func<object>, object> invokeOnMainThread)
            {
                if (!AttachToClientProcess(clientProcessId))
                {
                    return;
                }

                var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await serverStream.WaitForConnectionAsync().ConfigureAwait(false);

                var jsonRpc = CreateRpc(serverStream, incomingCallTarget: new Service(invokeOnMainThread));

                await jsonRpc.Completion.ConfigureAwait(false);

                // the client can instantiate interactive host now:
                s_clientExited.Wait();
            }

            #endregion

            #region Remote Async Entry Points

            // Used by ResetInteractive - consider improving (we should remember the parameters for auto-reset, e.g.)

            public async Task<RemoteExecutionResult.Data> SetPathsAsync(
                string[] referenceSearchPaths,
                string[] sourceSearchPaths,
                string? baseDirectory)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();
                lock (_lastTaskGuard)
                {
                    _lastTask = SetPathsAsync(_lastTask, completionSource, referenceSearchPaths, sourceSearchPaths, baseDirectory);
                }

                return (await completionSource.Task.ConfigureAwait(false)).Serialize();
            }

            private async Task<EvaluationState> SetPathsAsync(
                Task<EvaluationState> lastTask,
                TaskCompletionSource<RemoteExecutionResult> completionSource,
                string[]? referenceSearchPaths,
                string[]? sourceSearchPaths,
                string? baseDirectory)
            {
                var serviceState = GetServiceState();
                var state = await ReportUnhandledExceptionIfAnyAsync(lastTask).ConfigureAwait(false);

                try
                {
                    Directory.SetCurrentDirectory(baseDirectory);

                    var referencePaths = serviceState.Globals.ReferencePaths;
                    referencePaths.Clear();
                    referencePaths.AddRange(referenceSearchPaths);

                    var sourcePaths = serviceState.Globals.SourcePaths;
                    sourcePaths.Clear();
                    sourcePaths.AddRange(sourceSearchPaths);
                }
                finally
                {
                    state = CompleteExecution(state, completionSource, success: true);
                }

                return state;
            }

            /// <summary>
            /// Reads given initialization file (.rsp) and loads and executes all assembly references and files, respectively specified in it.
            /// Execution is performed on the UI thread.
            /// </summary>
            public async Task<RemoteExecutionResult.Data> InitializeContextAsync(string? initializationFilePath, bool isRestarting)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();
                lock (_lastTaskGuard)
                {
                    _lastTask = InitializeContextAsync(_lastTask, completionSource, initializationFilePath, isRestarting);
                }

                return (await completionSource.Task.ConfigureAwait(false)).Serialize();
            }

            /// <summary>
            /// Adds an assembly reference to the current session.
            /// </summary>
            public async Task<bool> AddReferenceAsync(string reference)
            {
                var completionSource = new TaskCompletionSource<bool>();
                lock (_lastTaskGuard)
                {
                    _lastTask = AddReferenceAsync(_lastTask, completionSource, reference);
                }

                return await completionSource.Task.ConfigureAwait(false);
            }

            private async Task<EvaluationState> AddReferenceAsync(Task<EvaluationState> lastTask, TaskCompletionSource<bool> completionSource, string reference)
            {
                var state = await ReportUnhandledExceptionIfAnyAsync(lastTask).ConfigureAwait(false);
                var success = false;
                try
                {
                    var resolvedReferences = state.ScriptOptions.MetadataResolver.ResolveReference(reference, baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                    if (!resolvedReferences.IsDefaultOrEmpty)
                    {
                        state = state.WithOptions(state.ScriptOptions.AddReferences(resolvedReferences));
                        success = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(string.Format(InteractiveHostResources.Cannot_resolve_reference_0, reference));
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    completionSource.SetResult(success);
                }

                return state;
            }

            /// <summary>
            /// Executes given script snippet on the UI thread in the context of the current session.
            /// </summary>
            public async Task<RemoteExecutionResult.Data> ExecuteAsync(string text)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();
                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteAsync(completionSource, _lastTask, text);
                }

                return (await completionSource.Task.ConfigureAwait(false)).Serialize();
            }

            private async Task<EvaluationState> ExecuteAsync(TaskCompletionSource<RemoteExecutionResult> completionSource, Task<EvaluationState> lastTask, string text)
            {
                var state = await ReportUnhandledExceptionIfAnyAsync(lastTask).ConfigureAwait(false);

                var success = false;
                try
                {
                    Script<object>? script = TryCompile(state.ScriptState?.Script, text, null, state.ScriptOptions);
                    if (script != null)
                    {
                        // successful if compiled
                        success = true;

                        // remove references and imports from the options, they have been applied and will be inherited from now on:
                        state = state.WithOptions(state.ScriptOptions.RemoveImportsAndReferences());

                        var newScriptState = await ExecuteOnUIThreadAsync(script, state.ScriptState, displayResult: true).ConfigureAwait(false);
                        state = state.WithScriptState(newScriptState);
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    state = CompleteExecution(state, completionSource, success);
                }

                return state;
            }

            private void DisplayException(Exception e)
            {
                if (e is FileLoadException && e.InnerException is InteractiveAssemblyLoaderException)
                {
                    Console.Error.WriteLine(e.InnerException.Message);
                }
                else
                {
                    Console.Error.Write(GetServiceState().ReplServiceProvider.ObjectFormatter.FormatException(e));
                }
            }

            /// <summary>
            /// Remote API. Executes given script file on the UI thread in the context of the current session.
            /// </summary>
            public async Task<RemoteExecutionResult.Data> ExecuteFileAsync(string path)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();

                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteFileAsync(completionSource, _lastTask, path);
                }

                return (await completionSource.Task.ConfigureAwait(false)).Serialize();
            }

            private EvaluationState CompleteExecution(EvaluationState state, TaskCompletionSource<RemoteExecutionResult> completionSource, bool success, RemoteInitializationResult? initResult = null)
            {
                // send any updates to the host object and current directory back to the client:
                var globals = GetServiceState().Globals;
                var newSourcePaths = globals.SourcePaths.ToImmutableArray();
                var newReferencePaths = globals.ReferencePaths.ToImmutableArray();
                var newWorkingDirectory = Directory.GetCurrentDirectory();

                completionSource.TrySetResult(new RemoteExecutionResult(success, newSourcePaths, newReferencePaths, newWorkingDirectory, initResult));

                var metadataResolver = state.MetadataReferenceResolver;
                var sourcePathsChanged = !newSourcePaths.SequenceEqual(state.SourceSearchPaths);
                var referencePathsChanged = !newReferencePaths.SequenceEqual(metadataResolver.SearchPaths);
                var workingDirectoryChanged = newWorkingDirectory != state.WorkingDirectory;

                // no changes in resolvers:
                if (!sourcePathsChanged && !referencePathsChanged && !workingDirectoryChanged)
                {
                    return state;
                }

                var newOptions = state.ScriptOptions;
                if (referencePathsChanged || workingDirectoryChanged)
                {
                    if (referencePathsChanged)
                    {
                        metadataResolver = metadataResolver.WithSearchPaths(newReferencePaths);
                    }

                    if (workingDirectoryChanged)
                    {
                        metadataResolver = metadataResolver.WithBaseDirectory(newWorkingDirectory);
                    }

                    newOptions = newOptions.WithMetadataResolver(metadataResolver);
                }

                if (sourcePathsChanged || workingDirectoryChanged)
                {
                    newOptions = newOptions.WithSourceResolver(CreateSourceReferenceResolver(newSourcePaths, newWorkingDirectory));
                }

                return new EvaluationState(
                    state.ScriptState,
                    newOptions,
                    newSourcePaths,
                    newWorkingDirectory);
            }

            private static async Task<EvaluationState> ReportUnhandledExceptionIfAnyAsync(Task<EvaluationState> lastTask)
            {
                try
                {
                    return await lastTask.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                    return lastTask.Result;
                }
            }

            private static void ReportUnhandledException(Exception e)
            {
                Console.Error.WriteLine("Unexpected error:");
                Console.Error.WriteLine(e);
                Debug.Fail("Unexpected error");
                Debug.WriteLine(e);
            }

            #endregion

            #region Operations

            /// <summary>
            /// Loads references, set options and execute files specified in the initialization file.
            /// Also prints logo unless <paramref name="isRestarting"/> is true.
            /// </summary>
            private async Task<EvaluationState> InitializeContextAsync(
                Task<EvaluationState> lastTask,
                TaskCompletionSource<RemoteExecutionResult> completionSource,
                string? initializationFilePath,
                bool isRestarting)
            {
                Contract.ThrowIfFalse(initializationFilePath == null || PathUtilities.IsAbsolute(initializationFilePath));
                var serviceState = GetServiceState();
                var state = await ReportUnhandledExceptionIfAnyAsync(lastTask).ConfigureAwait(false);

                string? initializationScriptPath = null;
                var initialImports = ImmutableArray<string>.Empty;
                var metadataReferencePaths = ImmutableArray.CreateBuilder<string>();

                try
                {
                    metadataReferencePaths.Add(typeof(object).Assembly.Location);
                    metadataReferencePaths.Add(typeof(InteractiveScriptGlobals).Assembly.Location);

                    if (!isRestarting)
                    {
                        Console.Out.WriteLine(serviceState.ReplServiceProvider.Logo);
                    }

                    if (File.Exists(initializationFilePath))
                    {
                        Console.Out.WriteLine(string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(initializationFilePath)));
                        var parser = serviceState.ReplServiceProvider.CommandLineParser;

                        // Add the Framework runtime directory to reference search paths when running on .NET Framework (PlatformAssemblyPaths list is empty).
                        // Otherwise, platform assemblies are looked up in PlatformAssemblyPaths directly.
                        var sdkDirectory = s_currentPlatformInfo.PlatformAssemblyPaths.IsEmpty ? RuntimeEnvironment.GetRuntimeDirectory() : null;

                        var rspDirectory = Path.GetDirectoryName(initializationFilePath);
                        var args = parser.Parse(new[] { "@" + initializationFilePath }, baseDirectory: rspDirectory, sdkDirectory, additionalReferenceDirectories: null);

                        foreach (var error in args.Errors)
                        {
                            var writer = (error.Severity == DiagnosticSeverity.Error) ? Console.Error : Console.Out;
                            writer.WriteLine(error.GetMessage(CultureInfo.CurrentUICulture));
                        }

                        if (args.Errors.Length == 0)
                        {
                            var referencePaths = args.ReferencePaths;
                            var sourcePaths = args.SourcePaths;

                            // TODO: Workaround for https://github.com/dotnet/roslyn/issues/45346
                            var referencePathsWithoutRspDir = referencePaths.Remove(rspDirectory);
                            var metadataResolver = state.MetadataReferenceResolver.WithSearchPaths(referencePathsWithoutRspDir);
                            var rspMetadataResolver = state.MetadataReferenceResolver.WithSearchPaths(referencePaths).WithBaseDirectory(rspDirectory);

                            var sourceResolver = CreateSourceReferenceResolver(sourcePaths, rspDirectory);

                            var metadataReferences = new List<PortableExecutableReference>();
                            foreach (var cmdLineReference in args.MetadataReferences)
                            {
                                // interactive command line parser doesn't accept modules or linked assemblies
                                Debug.Assert(cmdLineReference.Properties.Kind == MetadataImageKind.Assembly && !cmdLineReference.Properties.EmbedInteropTypes);

                                var resolvedReferences = rspMetadataResolver.ResolveReference(cmdLineReference.Reference, baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                                if (!resolvedReferences.IsDefaultOrEmpty)
                                {
                                    metadataReferences.AddRange(resolvedReferences);
                                    metadataReferencePaths.AddRange(resolvedReferences.Where(r => r.FilePath != null).Select(r => r.FilePath!));
                                }
                            }

                            initializationScriptPath = args.SourceFiles.IsEmpty ? null : args.SourceFiles[0].Path;
                            initialImports = CommandLineHelpers.GetImports(args);

                            var rspState = new EvaluationState(
                                state.ScriptState,
                                state.ScriptOptions.
                                    WithFilePath(initializationScriptPath).
                                    WithReferences(metadataReferences).
                                    WithImports(initialImports).
                                    WithMetadataResolver(metadataResolver).
                                    WithSourceResolver(sourceResolver),
                                args.SourcePaths,
                                rspDirectory);

                            var globals = serviceState.Globals;
                            globals.ReferencePaths.Clear();
                            globals.ReferencePaths.AddRange(referencePathsWithoutRspDir);

                            globals.SourcePaths.Clear();
                            globals.SourcePaths.AddRange(sourcePaths);

                            globals.Args.AddRange(args.ScriptArguments);

                            if (initializationScriptPath != null)
                            {
                                var newScriptState = await TryExecuteFileAsync(rspState, initializationScriptPath).ConfigureAwait(false);
                                if (newScriptState != null)
                                {
                                    // remove references and imports from the options, they have been applied and will be inherited from now on:
                                    rspState = rspState.
                                        WithScriptState(newScriptState).
                                        WithOptions(rspState.ScriptOptions.RemoveImportsAndReferences());
                                }
                            }

                            state = rspState;
                        }
                    }

                    if (!isRestarting)
                    {
                        Console.Out.WriteLine(InteractiveHostResources.Type_Sharphelp_for_more_information);
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    var initResult = new RemoteInitializationResult(initializationScriptPath, metadataReferencePaths.ToImmutableArray(), initialImports);
                    state = CompleteExecution(state, completionSource, success: true, initResult);
                }

                return state;
            }

            private string? ResolveRelativePath(string path, string baseDirectory, ImmutableArray<string> searchPaths, bool displayPath)
            {
                var attempts = new List<string>();
                bool fileExists(string file)
                {
                    attempts.Add(file);
                    return File.Exists(file);
                }

                var fullPath = FileUtilities.ResolveRelativePath(path, null, baseDirectory, searchPaths, fileExists);
                if (fullPath == null)
                {
                    if (displayPath)
                    {
                        Console.Error.WriteLine(InteractiveHostResources.Specified_file_not_found_colon_0, path);
                    }
                    else
                    {
                        Console.Error.WriteLine(InteractiveHostResources.Specified_file_not_found);
                    }

                    if (attempts.Count > 0)
                    {
                        DisplaySearchPaths(Console.Error, attempts);
                    }
                }

                return fullPath;
            }

            private Script<object>? TryCompile(Script? previousScript, string code, string? path, ScriptOptions options)
            {
                var serviceState = GetServiceState();

                Script script;

                var scriptOptions = options.WithFilePath(path);

                if (previousScript != null)
                {
                    script = previousScript.ContinueWith(code, scriptOptions);
                }
                else
                {
                    script = serviceState.ReplServiceProvider.CreateScript<object>(code, scriptOptions, serviceState.Globals.GetType(), serviceState.AssemblyLoader);
                }

                var diagnostics = script.Compile();
                if (diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error))
                {
                    DisplayInteractiveErrors(diagnostics, Console.Error);
                    return null;
                }

                return (Script<object>)script;
            }

            private async Task<EvaluationState> ExecuteFileAsync(
                TaskCompletionSource<RemoteExecutionResult> completionSource,
                Task<EvaluationState> lastTask,
                string path)
            {
                var state = await ReportUnhandledExceptionIfAnyAsync(lastTask).ConfigureAwait(false);
                var fullPath = ResolveRelativePath(path, state.WorkingDirectory, state.SourceSearchPaths, displayPath: false);
                if (fullPath != null)
                {
                    var newScriptState = await TryExecuteFileAsync(state, fullPath).ConfigureAwait(false);
                    if (newScriptState != null)
                    {
                        return CompleteExecution(state.WithScriptState(newScriptState), completionSource, success: newScriptState.Exception == null);
                    }
                }

                return CompleteExecution(state, completionSource, success: false);
            }

            /// <summary>
            /// Executes specified script file as a submission.
            /// </summary>
            /// <remarks>
            /// All errors are written to the error output stream.
            /// Uses source search paths to resolve unrooted paths.
            /// </remarks>
            private async Task<ScriptState<object>?> TryExecuteFileAsync(EvaluationState state, string fullPath)
            {
                Debug.Assert(PathUtilities.IsAbsolute(fullPath));

                string? content = null;
                try
                {
                    using var reader = File.OpenText(fullPath);
                    content = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // file read errors:
                    Console.Error.WriteLine(e.Message);
                    return null;
                }

                Script<object>? script = TryCompile(state.ScriptState?.Script, content, fullPath, state.ScriptOptions);
                if (script == null)
                {
                    // compilation errors:
                    return null;
                }

                return await ExecuteOnUIThreadAsync(script, state.ScriptState, displayResult: false).ConfigureAwait(false);
            }

            private static void DisplaySearchPaths(TextWriter writer, List<string> attemptedFilePaths)
            {
                var directories = attemptedFilePaths.Select(path => Path.GetDirectoryName(path)).ToArray();
                var uniqueDirectories = new HashSet<string>(directories);

                writer.WriteLine(uniqueDirectories.Count == 1
                    ? InteractiveHostResources.Searched_in_directory_colon
                    : InteractiveHostResources.Searched_in_directories_colon);

                foreach (string directory in directories)
                {
                    if (uniqueDirectories.Remove(directory))
                    {
                        writer.Write("  ");
                        writer.WriteLine(directory);
                    }
                }
            }

            private Task<ScriptState<object>> ExecuteOnUIThreadAsync(Script<object> script, ScriptState<object>? state, bool displayResult)
            {
                return (Task<ScriptState<object>>)_invokeOnMainThread((Func<Task<ScriptState<object>>>)(async () =>
                {
                    var serviceState = GetServiceState();

                    var task = (state == null)
                        ? script.RunAsync(serviceState.Globals, catchException: e => true, cancellationToken: CancellationToken.None)
                        : script.RunFromAsync(state, catchException: e => true, cancellationToken: CancellationToken.None);

                    var newState = await task.ConfigureAwait(false);

                    if (newState.Exception != null)
                    {
                        DisplayException(newState.Exception);
                    }
                    else if (displayResult && newState.Script.HasReturnValue())
                    {
                        serviceState.Globals.Print(newState.ReturnValue);
                    }

                    return newState;
                }));
            }

            private void DisplayInteractiveErrors(ImmutableArray<Diagnostic> diagnostics, TextWriter output)
            {
                var displayedDiagnostics = new List<Diagnostic>();
                const int MaxErrorCount = 5;
                for (int i = 0, n = Math.Min(diagnostics.Length, MaxErrorCount); i < n; i++)
                {
                    displayedDiagnostics.Add(diagnostics[i]);
                }

                displayedDiagnostics.Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

                var formatter = GetServiceState().ReplServiceProvider.DiagnosticFormatter;

                foreach (var diagnostic in displayedDiagnostics)
                {
                    output.WriteLine(formatter.Format(diagnostic, output.FormatProvider as CultureInfo));
                }

                if (diagnostics.Length > MaxErrorCount)
                {
                    int notShown = diagnostics.Length - MaxErrorCount;
                    output.WriteLine(string.Format(output.FormatProvider, InteractiveHostResources.plus_additional_0_1, notShown, (notShown == 1) ? "error" : "errors"));
                }
            }

            #endregion

            #region Testing

            // TODO(tomat): remove when the compiler supports events
            // For testing purposes only!
            public void HookMaliciousAssemblyResolve()
            {
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler((_, __) =>
                {
                    int i = 0;
                    while (true)
                    {
                        if (i < 10)
                        {
                            i = i + 1;
                        }
                        else if (i == 10)
                        {
                            Console.Error.WriteLine("in the loop");
                            i = i + 1;
                        }
                    }
                });
            }

            /// <summary>
            /// Remote API for testing purposes.
            /// </summary>
            public Task RemoteConsoleWriteAsync(byte[] data, bool isError)
            {
                using (var stream = isError ? Console.OpenStandardError() : Console.OpenStandardOutput())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }

                return Task.CompletedTask;
            }

            /// <summary>
            /// Remote API for testing purposes.
            /// </summary>
            public Task<string> GetRuntimeDirectoryAsync()
                => Task.FromResult(RuntimeEnvironment.GetRuntimeDirectory());

            #endregion
        }
    }
}
