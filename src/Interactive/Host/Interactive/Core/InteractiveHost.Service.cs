// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;
using StreamJsonRpc;

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

            private static Control? s_control;

            private ServiceState? _serviceState;

            // Session is not thread-safe by itself, and the compilation
            // and execution of scripts are asynchronous operations.
            // However since the operations are executed serially, it
            // is sufficient to lock when creating the async tasks.
            private readonly object _lastTaskGuard = new object();
            private Task<EvaluationState> _lastTask;

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
                internal readonly ImmutableArray<string> ReferenceSearchPaths;
                internal readonly string WorkingDirectory;
                internal readonly ScriptState<object>? ScriptState;
                internal readonly ScriptOptions ScriptOptions;

                internal EvaluationState(
                    ScriptState<object>? scriptState,
                    ScriptOptions scriptOptions,
                    ImmutableArray<string> sourceSearchPaths,
                    ImmutableArray<string> referenceSearchPaths,
                    string workingDirectory)
                {
                    Debug.Assert(!sourceSearchPaths.IsDefault);
                    Debug.Assert(!referenceSearchPaths.IsDefault);

                    ScriptState = scriptState;
                    ScriptOptions = scriptOptions;
                    SourceSearchPaths = sourceSearchPaths;
                    ReferenceSearchPaths = referenceSearchPaths;
                    WorkingDirectory = workingDirectory;
                }

                internal EvaluationState WithScriptState(ScriptState<object> state)
                {
                    return new EvaluationState(
                        state,
                        ScriptOptions,
                        SourceSearchPaths,
                        ReferenceSearchPaths,
                        WorkingDirectory);
                }

                internal EvaluationState WithOptions(ScriptOptions options)
                {
                    return new EvaluationState(
                        ScriptState,
                        options,
                        SourceSearchPaths,
                        ReferenceSearchPaths,
                        WorkingDirectory);
                }
            }

            private static readonly ImmutableArray<string> s_systemNoShadowCopyDirectories = ImmutableArray.Create(
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
                FileUtilities.NormalizeDirectoryPath(RuntimeEnvironment.GetRuntimeDirectory()));

            #region Setup

            public Service()
            {
                var initialState = new EvaluationState(
                    scriptState: null,
                    scriptOptions: ScriptOptions.Default,
                    sourceSearchPaths: ImmutableArray<string>.Empty,
                    referenceSearchPaths: ImmutableArray<string>.Empty,
                    workingDirectory: Directory.GetCurrentDirectory());

                _lastTask = Task.FromResult(initialState);

                Console.OutputEncoding = Encoding.UTF8;

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

            /*public override object? InitializeLifetimeService()
            {
                return null;
            }*/

            public Task InitializeAsync(string replServiceProviderTypeName, string cultureName)
            {
                Debug.Assert(cultureName != null);
                using (var resetEvent = new ManualResetEventSlim(false))
                {
                    var uiThread = new Thread(() =>
                    {
                        s_control = new Control();
                        s_control.CreateControl();
                        resetEvent.Set();
                        Application.Run();
                    });
                    uiThread.SetApartmentState(ApartmentState.STA);
                    uiThread.IsBackground = true;
                    uiThread.Start();
                    resetEvent.Wait();
                }
                // TODO (tomat): we should share the copied files with the host
                var metadataFileProvider = new MetadataShadowCopyProvider(
                    Path.Combine(Path.GetTempPath(), "InteractiveHostShadow"),
                    noShadowCopyDirectories: s_systemNoShadowCopyDirectories,
                    documentationCommentsCulture: new CultureInfo(cultureName));

                var assemblyLoader = new InteractiveAssemblyLoader(metadataFileProvider);
                var replServiceProviderType = Type.GetType(replServiceProviderTypeName);
                var replServiceProvider = (ReplServiceProvider)Activator.CreateInstance(replServiceProviderType);
                var globals = new InteractiveScriptGlobals(Console.Out, replServiceProvider.ObjectFormatter);

                _serviceState = new ServiceState(assemblyLoader, metadataFileProvider, replServiceProvider, globals);

                return Task.CompletedTask;
            }
            private ServiceState GetServiceState()
            {
                Contract.ThrowIfNull(_serviceState, "Service not initialized");
                return _serviceState;
            }

            private MetadataReferenceResolver CreateMetadataReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
            {
                return new RuntimeMetadataReferenceResolver(
                    new RelativePathResolver(searchPaths, baseDirectory),
                    packageResolver: null,
                    gacFileResolver: GacFileResolver.IsAvailable ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                    useCoreResolver: !GacFileResolver.IsAvailable,
                    fileReferenceProvider: (path, properties) => new ShadowCopyReference(GetServiceState().MetadataFileProvider, path, properties));
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

            internal static async Task RunServerAsync(string[] args)
            {
                if (args.Length != 2)
                {
                    throw new ArgumentException("Expecting arguments: <pipe name> <client process id>");
                }

                await RunServerAsync(args[0], int.Parse(args[1], CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            /// <summary>
            /// Implements remote server.
            /// </summary>
            private static async Task RunServerAsync(string pipeName, int clientProcessId)
            {
                if (!AttachToClientProcess(clientProcessId))
                {
                    return;
                }

                // Disables Windows Error Reporting for the process, so that the process fails fast.
                // Unfortunately, this doesn't work on Windows Server 2008 (OS v6.0), Vista (OS v6.0) and XP (OS v5.1)
                // Note that GetErrorMode is not available on XP at all.
                if (Environment.OSVersion.Version >= new Version(6, 1, 0, 0))
                {
                    SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX | ErrorMode.SEM_NOGPFAULTERRORBOX);
                }

                try
                {
                    using (var resetEvent = new ManualResetEventSlim(false))
                    {
                        var uiThread = new Thread(() =>
                        {
                            s_control = new Control();
                            s_control.CreateControl();
                            resetEvent.Set();
                            Application.Run();
                        });
                        uiThread.SetApartmentState(ApartmentState.STA);
                        uiThread.IsBackground = true;
                        uiThread.Start();
                        resetEvent.Wait();
                    }

                    var serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await serverStream.WaitForConnectionAsync().ConfigureAwait(false);
                    var jsonRPC = JsonRpc.Attach(serverStream, new Service());
                    await jsonRPC.Completion.ConfigureAwait(false);
                    // the client can instantiate interactive host now:
                    s_clientExited.Wait();
                }
                finally
                {
                    // TODO:(miziga): delete and make a finally or catch statement for the try
                }                // force exit even if there are foreground threads running:
                Environment.Exit(0);
            }

            internal static string ServiceName
            {
                get { return typeof(Service).Name; }
            }

            #endregion

            #region Remote Async Entry Points

            // Used by ResetInteractive - consider improving (we should remember the parameters for auto-reset, e.g.)

            public async Task<RemoteExecutionResult> SetPathsAsync(
                string[] referenceSearchPaths,
                string[] sourceSearchPaths,
                string? baseDirectory)
            {
                Debug.Assert(referenceSearchPaths != null);
                Debug.Assert(sourceSearchPaths != null);
                Debug.Assert(baseDirectory != null);
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();
                lock (_lastTaskGuard)
                {
                    _lastTask = SetPathsAsync(_lastTask, completionSource, referenceSearchPaths, sourceSearchPaths, baseDirectory);
                }

                return await completionSource.Task.ConfigureAwait(false);
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
            public async Task<RemoteExecutionResult> InitializeContextAsync(string? initializationFile, bool isRestarting)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();
                lock (_lastTaskGuard)
                {
                    _lastTask = InitializeContextAsync(_lastTask, completionSource, initializationFile, isRestarting);
                }
                return await completionSource.Task.ConfigureAwait(false);
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
            public async Task<RemoteExecutionResult> ExecuteAsync(string text)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();
                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteAsync(completionSource, _lastTask, text);
                }
                return await completionSource.Task.ConfigureAwait(false);
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
            public async Task<RemoteExecutionResult> ExecuteFileAsync(string path)
            {
                var completionSource = new TaskCompletionSource<RemoteExecutionResult>();

                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteFileAsync(completionSource, _lastTask, path);
                }
                return await completionSource.Task.ConfigureAwait(false);
            }

            private EvaluationState CompleteExecution(EvaluationState state, TaskCompletionSource<RemoteExecutionResult> completionSource, bool success)
            {
                // send any updates to the host object and current directory back to the client:
                var globals = GetServiceState().Globals;
                var currentSourcePaths = globals.SourcePaths.ToArray();
                var currentReferencePaths = globals.ReferencePaths.ToArray();
                var currentWorkingDirectory = Directory.GetCurrentDirectory();

                var changedSourcePaths = currentSourcePaths.SequenceEqual(state.SourceSearchPaths) ? null : currentSourcePaths;
                var changedReferencePaths = currentReferencePaths.SequenceEqual(state.ReferenceSearchPaths) ? null : currentReferencePaths;
                var changedWorkingDirectory = currentWorkingDirectory == state.WorkingDirectory ? null : currentWorkingDirectory;

                completionSource.TrySetResult(new RemoteExecutionResult(success, changedSourcePaths, changedReferencePaths, changedWorkingDirectory));

                // no changes in resolvers:
                if (changedReferencePaths == null && changedSourcePaths == null && changedWorkingDirectory == null)
                {
                    return state;
                }

                var newSourcePaths = ImmutableArray.CreateRange(currentSourcePaths);
                var newReferencePaths = ImmutableArray.CreateRange(currentReferencePaths);
                var newWorkingDirectory = currentWorkingDirectory;

                ScriptOptions newOptions = state.ScriptOptions;
                if (changedReferencePaths != null || changedWorkingDirectory != null)
                {
                    newOptions = newOptions.WithMetadataResolver(CreateMetadataReferenceResolver(newReferencePaths, newWorkingDirectory));
                }

                if (changedSourcePaths != null || changedWorkingDirectory != null)
                {
                    newOptions = newOptions.WithSourceResolver(CreateSourceReferenceResolver(newSourcePaths, newWorkingDirectory));
                }

                return new EvaluationState(
                    state.ScriptState,
                    newOptions,
                    newSourcePaths,
                    newReferencePaths,
                    workingDirectory: newWorkingDirectory);
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
                string? initializationFile,
                bool isRestarting)
            {
                Contract.ThrowIfFalse(initializationFile == null || PathUtilities.IsAbsolute(initializationFile));
                var serviceState = GetServiceState();
                var state = await ReportUnhandledExceptionIfAnyAsync(lastTask).ConfigureAwait(false);

                try
                {
                    // TODO (tomat): this is also done in CommonInteractiveEngine, perhaps we can pass the parsed command lines to here?

                    if (!isRestarting)
                    {
                        Console.Out.WriteLine(serviceState.ReplServiceProvider.Logo);
                    }

                    if (File.Exists(initializationFile))
                    {
                        Console.Out.WriteLine(string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(initializationFile)));
                        var parser = serviceState.ReplServiceProvider.CommandLineParser;

                        // The base directory for relative paths is the directory that contains the .rsp file.
                        // Note that .rsp files included by this .rsp file will share the base directory (Dev10 behavior of csc/vbc).
                        var rspDirectory = Path.GetDirectoryName(initializationFile);
                        var args = parser.Parse(new[] { "@" + initializationFile }, rspDirectory, RuntimeEnvironment.GetRuntimeDirectory(), null);

                        foreach (var error in args.Errors)
                        {
                            var writer = (error.Severity == DiagnosticSeverity.Error) ? Console.Error : Console.Out;
                            writer.WriteLine(error.GetMessage(CultureInfo.CurrentCulture));
                        }

                        if (args.Errors.Length == 0)
                        {
                            var metadataResolver = CreateMetadataReferenceResolver(args.ReferencePaths, rspDirectory);
                            var sourceResolver = CreateSourceReferenceResolver(args.SourcePaths, rspDirectory);

                            var metadataReferences = new List<PortableExecutableReference>();
                            foreach (CommandLineReference cmdLineReference in args.MetadataReferences)
                            {
                                // interactive command line parser doesn't accept modules or linked assemblies
                                Debug.Assert(cmdLineReference.Properties.Kind == MetadataImageKind.Assembly && !cmdLineReference.Properties.EmbedInteropTypes);

                                var resolvedReferences = metadataResolver.ResolveReference(cmdLineReference.Reference, baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                                if (!resolvedReferences.IsDefaultOrEmpty)
                                {
                                    metadataReferences.AddRange(resolvedReferences);
                                }
                            }

                            var scriptPathOpt = args.SourceFiles.IsEmpty ? null : args.SourceFiles[0].Path;

                            var rspState = new EvaluationState(
                                state.ScriptState,
                                state.ScriptOptions.
                                    WithFilePath(scriptPathOpt).
                                    WithReferences(metadataReferences).
                                    WithImports(CommandLineHelpers.GetImports(args)).
                                    WithMetadataResolver(metadataResolver).
                                    WithSourceResolver(sourceResolver),
                                args.SourcePaths,
                                args.ReferencePaths,
                                rspDirectory);

                            var globals = serviceState.Globals;
                            globals.ReferencePaths.Clear();
                            globals.ReferencePaths.AddRange(args.ReferencePaths);

                            globals.SourcePaths.Clear();
                            globals.SourcePaths.AddRange(args.SourcePaths);

                            globals.Args.AddRange(args.ScriptArguments);

                            if (scriptPathOpt != null)
                            {
                                var newScriptState = await TryExecuteFileAsync(rspState, scriptPathOpt).ConfigureAwait(false);
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
                    state = CompleteExecution(state, completionSource, success: true);
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
                if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
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

                writer.WriteLine(uniqueDirectories.Count == 1 ?
                    InteractiveHostResources.Searched_in_directory_colon :
                    InteractiveHostResources.Searched_in_directories_colon);

                foreach (string directory in directories)
                {
                    if (uniqueDirectories.Remove(directory))
                    {
                        writer.Write("  ");
                        writer.WriteLine(directory);
                    }
                }
            }

            private async Task<ScriptState<object>> ExecuteOnUIThreadAsync(Script<object> script, ScriptState<object>? state, bool displayResult)
            {
                Contract.ThrowIfNull(s_control, "UI thread not initialized");

                return await ((Task<ScriptState<object>>)s_control.Invoke(
                    (Func<Task<ScriptState<object>>>)(async () =>
                    {
                        var serviceState = GetServiceState();

                        var task = (state == null) ?
                            script.RunAsync(serviceState.Globals, catchException: e => true, cancellationToken: CancellationToken.None) :
                            script.RunFromAsync(state, catchException: e => true, cancellationToken: CancellationToken.None);

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

                    }))).ConfigureAwait(false);
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

            #region Win32 API

            [DllImport("kernel32", PreserveSig = true)]
            internal static extern ErrorMode SetErrorMode(ErrorMode mode);

            [DllImport("kernel32", PreserveSig = true)]
            internal static extern ErrorMode GetErrorMode();

            [Flags]
            internal enum ErrorMode : int
            {
                /// <summary>
                /// Use the system default, which is to display all error dialog boxes.
                /// </summary>
                SEM_FAILCRITICALERRORS = 0x0001,

                /// <summary>
                /// The system does not display the critical-error-handler message box. Instead, the system sends the error to the calling process.
                /// Best practice is that all applications call the process-wide SetErrorMode function with a parameter of SEM_FAILCRITICALERRORS at startup. 
                /// This is to prevent error mode dialogs from hanging the application.
                /// </summary>
                SEM_NOGPFAULTERRORBOX = 0x0002,

                /// <summary>
                /// The system automatically fixes memory alignment faults and makes them invisible to the application. 
                /// It does this for the calling process and any descendant processes. This feature is only supported by 
                /// certain processor architectures. For more information, see the Remarks section.
                /// After this value is set for a process, subsequent attempts to clear the value are ignored.
                /// </summary>
                SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,

                /// <summary>
                /// The system does not display a message box when it fails to find a file. Instead, the error is returned to the calling process.
                /// </summary>
                SEM_NOOPENFILEERRORBOX = 0x8000,
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

            #endregion
        }
    }
}
