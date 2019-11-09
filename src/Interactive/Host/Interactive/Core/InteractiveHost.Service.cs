// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias Scripting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;

    internal partial class InteractiveHost
    {
        /// <summary>
        /// A remote singleton server-activated object that lives in the interactive host process and controls it.
        /// </summary>
        internal sealed class Service : MarshalByRefObject, IDisposable
        {
            private static readonly ManualResetEventSlim s_clientExited = new ManualResetEventSlim(false);

            private static Control s_control;

            private InteractiveAssemblyLoader _assemblyLoader;
            private MetadataShadowCopyProvider _metadataFileProvider;
            private ReplServiceProvider _replServiceProvider;
            private InteractiveScriptGlobals _globals;
            private CultureInfo _culture;

            // Session is not thread-safe by itself, and the compilation
            // and execution of scripts are asynchronous operations.
            // However since the operations are executed serially, it
            // is sufficient to lock when creating the async tasks.
            private readonly object _lastTaskGuard = new object();
            private Task<EvaluationState> _lastTask;

            private struct EvaluationState
            {
                internal ImmutableArray<string> SourceSearchPaths;
                internal ImmutableArray<string> ReferenceSearchPaths;
                internal string WorkingDirectory;
                internal readonly ScriptState<object> ScriptStateOpt;
                internal readonly ScriptOptions ScriptOptions;

                internal EvaluationState(
                    ScriptState<object> scriptStateOpt,
                    ScriptOptions scriptOptions,
                    ImmutableArray<string> sourceSearchPaths,
                    ImmutableArray<string> referenceSearchPaths,
                    string workingDirectory)
                {
                    Debug.Assert(scriptOptions != null);
                    Debug.Assert(!sourceSearchPaths.IsDefault);
                    Debug.Assert(!referenceSearchPaths.IsDefault);
                    Debug.Assert(workingDirectory != null);

                    ScriptStateOpt = scriptStateOpt;
                    ScriptOptions = scriptOptions;
                    SourceSearchPaths = sourceSearchPaths;
                    ReferenceSearchPaths = referenceSearchPaths;
                    WorkingDirectory = workingDirectory;
                }

                internal EvaluationState WithScriptState(ScriptState<object> state)
                {
                    Debug.Assert(state != null);

                    return new EvaluationState(
                        state,
                        ScriptOptions,
                        SourceSearchPaths,
                        ReferenceSearchPaths,
                        WorkingDirectory);
                }

                internal EvaluationState WithOptions(ScriptOptions options)
                {
                    Debug.Assert(options != null);

                    return new EvaluationState(
                        ScriptStateOpt,
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
                    scriptStateOpt: null,
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
                _metadataFileProvider.Dispose();
            }

            public override object InitializeLifetimeService()
            {
                return null;
            }

            public void Initialize(Type replServiceProviderType, string cultureName)
            {
                Debug.Assert(replServiceProviderType != null);
                Debug.Assert(cultureName != null);

                Debug.Assert(_metadataFileProvider == null);
                Debug.Assert(_assemblyLoader == null);
                Debug.Assert(_replServiceProvider == null);

                _culture = new CultureInfo(cultureName);

                // TODO (tomat): we should share the copied files with the host
                _metadataFileProvider = new MetadataShadowCopyProvider(
                    Path.Combine(Path.GetTempPath(), "InteractiveHostShadow"),
                    noShadowCopyDirectories: s_systemNoShadowCopyDirectories,
                    documentationCommentsCulture: _culture);

                _assemblyLoader = new InteractiveAssemblyLoader(_metadataFileProvider);

                _replServiceProvider = (ReplServiceProvider)Activator.CreateInstance(replServiceProviderType);

                _globals = new InteractiveScriptGlobals(Console.Out, _replServiceProvider.ObjectFormatter);
            }

            private MetadataReferenceResolver CreateMetadataReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
            {
                return new RuntimeMetadataReferenceResolver(
                    new RelativePathResolver(searchPaths, baseDirectory),
                    packageResolver: null,
                    gacFileResolver: GacFileResolver.IsAvailable ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                    useCoreResolver: !GacFileResolver.IsAvailable,
                    fileReferenceProvider: (path, properties) => new ShadowCopyReference(_metadataFileProvider, path, properties));
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
                clientProcess.Exited += new EventHandler((_, __) =>
                {
                    s_clientExited.Set();
                });

                return clientProcess.IsAlive();
            }

            // for testing purposes
            public void EmulateClientExit()
            {
                s_clientExited.Set();
            }

            internal static void RunServer(string[] args)
            {
                if (args.Length != 3)
                {
                    throw new ArgumentException("Expecting arguments: <server port> <semaphore name> <client process id>");
                }

                RunServer(args[0], args[1], int.Parse(args[2], CultureInfo.InvariantCulture));
            }

            /// <summary>
            /// Implements remote server.
            /// </summary>
            private static void RunServer(string serverPort, string semaphoreName, int clientProcessId)
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

                IpcServerChannel serverChannel = null;
                IpcClientChannel clientChannel = null;
                try
                {
                    using (var semaphore = Semaphore.OpenExisting(semaphoreName))
                    {
                        // DEBUG: semaphore.WaitOne();

                        var serverProvider = new BinaryServerFormatterSinkProvider();
                        serverProvider.TypeFilterLevel = TypeFilterLevel.Full;

                        var clientProvider = new BinaryClientFormatterSinkProvider();

                        clientChannel = new IpcClientChannel(GenerateUniqueChannelLocalName(), clientProvider);
                        ChannelServices.RegisterChannel(clientChannel, ensureSecurity: false);

                        serverChannel = new IpcServerChannel(GenerateUniqueChannelLocalName(), serverPort, serverProvider);
                        ChannelServices.RegisterChannel(serverChannel, ensureSecurity: false);

                        RemotingConfiguration.RegisterWellKnownServiceType(
                            typeof(Service),
                            ServiceName,
                            WellKnownObjectMode.Singleton);

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

                        // the client can instantiate interactive host now:
                        semaphore.Release();
                    }

                    s_clientExited.Wait();
                }
                finally
                {
                    if (serverChannel != null)
                    {
                        ChannelServices.UnregisterChannel(serverChannel);
                    }

                    if (clientChannel != null)
                    {
                        ChannelServices.UnregisterChannel(clientChannel);
                    }
                }

                // force exit even if there are foreground threads running:
                Environment.Exit(0);
            }

            internal static string ServiceName
            {
                get { return typeof(Service).Name; }
            }

            private static string GenerateUniqueChannelLocalName()
            {
                return typeof(Service).FullName + Guid.NewGuid();
            }

            #endregion

            #region Remote Async Entry Points

            // Used by ResetInteractive - consider improving (we should remember the parameters for auto-reset, e.g.)

            [OneWay]
            public void SetPathsAsync(
                RemoteAsyncOperation<RemoteExecutionResult> operation,
                string[] referenceSearchPaths,
                string[] sourceSearchPaths,
                string baseDirectory)
            {
                Debug.Assert(operation != null);
                Debug.Assert(referenceSearchPaths != null);
                Debug.Assert(sourceSearchPaths != null);
                Debug.Assert(baseDirectory != null);

                lock (_lastTaskGuard)
                {
                    _lastTask = SetPathsAsync(_lastTask, operation, referenceSearchPaths, sourceSearchPaths, baseDirectory);
                }
            }

            private async Task<EvaluationState> SetPathsAsync(
                Task<EvaluationState> lastTask,
                RemoteAsyncOperation<RemoteExecutionResult> operation,
                string[] referenceSearchPaths,
                string[] sourceSearchPaths,
                string baseDirectory)
            {
                var state = await ReportUnhandledExceptionIfAny(lastTask).ConfigureAwait(false);

                try
                {
                    Directory.SetCurrentDirectory(baseDirectory);

                    _globals.ReferencePaths.Clear();
                    _globals.ReferencePaths.AddRange(referenceSearchPaths);

                    _globals.SourcePaths.Clear();
                    _globals.SourcePaths.AddRange(sourceSearchPaths);
                }
                finally
                {
                    state = CompleteExecution(state, operation, success: true);
                }

                return state;
            }

            /// <summary>
            /// Reads given initialization file (.rsp) and loads and executes all assembly references and files, respectively specified in it.
            /// Execution is performed on the UI thread.
            /// </summary>
            [OneWay]
            public void InitializeContextAsync(RemoteAsyncOperation<RemoteExecutionResult> operation, string initializationFile, bool isRestarting)
            {
                Debug.Assert(operation != null);

                lock (_lastTaskGuard)
                {
                    _lastTask = InitializeContextAsync(_lastTask, operation, initializationFile, isRestarting);
                }
            }

            /// <summary>
            /// Adds an assembly reference to the current session.
            /// </summary>
            [OneWay]
            public void AddReferenceAsync(RemoteAsyncOperation<bool> operation, string reference)
            {
                Debug.Assert(operation != null);
                Debug.Assert(reference != null);

                lock (_lastTaskGuard)
                {
                    _lastTask = AddReferenceAsync(_lastTask, operation, reference);
                }
            }

            private async Task<EvaluationState> AddReferenceAsync(Task<EvaluationState> lastTask, RemoteAsyncOperation<bool> operation, string reference)
            {
                var state = await ReportUnhandledExceptionIfAny(lastTask).ConfigureAwait(false);
                bool success = false;

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
                    operation.Completed(success);
                }

                return state;
            }

            /// <summary>
            /// Executes given script snippet on the UI thread in the context of the current session.
            /// </summary>
            [OneWay]
            public void ExecuteAsync(RemoteAsyncOperation<RemoteExecutionResult> operation, string text)
            {
                Debug.Assert(operation != null);
                Debug.Assert(text != null);

                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteAsync(_lastTask, operation, text);
                }
            }

            private async Task<EvaluationState> ExecuteAsync(Task<EvaluationState> lastTask, RemoteAsyncOperation<RemoteExecutionResult> operation, string text)
            {
                var state = await ReportUnhandledExceptionIfAny(lastTask).ConfigureAwait(false);

                bool success = false;
                try
                {
                    Script<object> script = TryCompile(state.ScriptStateOpt?.Script, text, null, state.ScriptOptions);
                    if (script != null)
                    {
                        // successful if compiled
                        success = true;

                        // remove references and imports from the options, they have been applied and will be inherited from now on:
                        state = state.WithOptions(state.ScriptOptions.RemoveImportsAndReferences());

                        var newScriptState = await ExecuteOnUIThread(script, state.ScriptStateOpt, displayResult: true).ConfigureAwait(false);
                        state = state.WithScriptState(newScriptState);
                    }
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    state = CompleteExecution(state, operation, success);
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
                    Console.Error.Write(_replServiceProvider.ObjectFormatter.FormatException(e));
                }
            }

            /// <summary>
            /// Executes given script file on the UI thread in the context of the current session.
            /// </summary>
            [OneWay]
            public void ExecuteFileAsync(RemoteAsyncOperation<RemoteExecutionResult> operation, string path)
            {
                Debug.Assert(operation != null);
                Debug.Assert(path != null);

                lock (_lastTaskGuard)
                {
                    _lastTask = ExecuteFileAsync(operation, _lastTask, path);
                }
            }

            private EvaluationState CompleteExecution(EvaluationState state, RemoteAsyncOperation<RemoteExecutionResult> operation, bool success)
            {
                // send any updates to the host object and current directory back to the client:
                var currentSourcePaths = _globals.SourcePaths.ToArray();
                var currentReferencePaths = _globals.ReferencePaths.ToArray();
                var currentWorkingDirectory = Directory.GetCurrentDirectory();

                var changedSourcePaths = currentSourcePaths.SequenceEqual(state.SourceSearchPaths) ? null : currentSourcePaths;
                var changedReferencePaths = currentReferencePaths.SequenceEqual(state.ReferenceSearchPaths) ? null : currentReferencePaths;
                var changedWorkingDirectory = currentWorkingDirectory == state.WorkingDirectory ? null : currentWorkingDirectory;

                operation.Completed(new RemoteExecutionResult(success, changedSourcePaths, changedReferencePaths, changedWorkingDirectory));

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
                    state.ScriptStateOpt,
                    newOptions,
                    newSourcePaths,
                    newReferencePaths,
                    workingDirectory: newWorkingDirectory);
            }

            private static async Task<EvaluationState> ReportUnhandledExceptionIfAny(Task<EvaluationState> lastTask)
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
                RemoteAsyncOperation<RemoteExecutionResult> operation,
                string initializationFileOpt,
                bool isRestarting)
            {
                Debug.Assert(initializationFileOpt == null || PathUtilities.IsAbsolute(initializationFileOpt));

                var state = await ReportUnhandledExceptionIfAny(lastTask).ConfigureAwait(false);

                try
                {
                    // TODO (tomat): this is also done in CommonInteractiveEngine, perhaps we can pass the parsed command lines to here?

                    if (!isRestarting)
                    {
                        Console.Out.WriteLine(_replServiceProvider.Logo);
                    }

                    if (File.Exists(initializationFileOpt))
                    {
                        Console.Out.WriteLine(string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(initializationFileOpt)));
                        var parser = _replServiceProvider.CommandLineParser;

                        // The base directory for relative paths is the directory that contains the .rsp file.
                        // Note that .rsp files included by this .rsp file will share the base directory (Dev10 behavior of csc/vbc).
                        var rspDirectory = Path.GetDirectoryName(initializationFileOpt);
                        var args = parser.Parse(new[] { "@" + initializationFileOpt }, rspDirectory, RuntimeEnvironment.GetRuntimeDirectory(), null);

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
                                state.ScriptStateOpt,
                                state.ScriptOptions.
                                    WithFilePath(scriptPathOpt).
                                    WithReferences(metadataReferences).
                                    WithImports(CommandLineHelpers.GetImports(args)).
                                    WithMetadataResolver(metadataResolver).
                                    WithSourceResolver(sourceResolver),
                                args.SourcePaths,
                                args.ReferencePaths,
                                rspDirectory);

                            _globals.ReferencePaths.Clear();
                            _globals.ReferencePaths.AddRange(args.ReferencePaths);

                            _globals.SourcePaths.Clear();
                            _globals.SourcePaths.AddRange(args.SourcePaths);

                            _globals.Args.AddRange(args.ScriptArguments);

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
                    state = CompleteExecution(state, operation, success: true);
                }

                return state;
            }

            private string ResolveRelativePath(string path, string baseDirectory, ImmutableArray<string> searchPaths, bool displayPath)
            {
                List<string> attempts = new List<string>();
                bool fileExists(string file)
                {
                    attempts.Add(file);
                    return File.Exists(file);
                }

                string fullPath = FileUtilities.ResolveRelativePath(path, null, baseDirectory, searchPaths, fileExists);
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

            private Script<object> TryCompile(Script previousScript, string code, string path, ScriptOptions options)
            {
                Script script;

                var scriptOptions = options.WithFilePath(path);

                if (previousScript != null)
                {
                    script = previousScript.ContinueWith(code, scriptOptions);
                }
                else
                {
                    script = _replServiceProvider.CreateScript<object>(code, scriptOptions, _globals.GetType(), _assemblyLoader);
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
                RemoteAsyncOperation<RemoteExecutionResult> operation,
                Task<EvaluationState> lastTask,
                string path)
            {
                var state = await ReportUnhandledExceptionIfAny(lastTask).ConfigureAwait(false);
                string fullPath = ResolveRelativePath(path, state.WorkingDirectory, state.SourceSearchPaths, displayPath: false);
                if (fullPath != null)
                {
                    var newScriptState = await TryExecuteFileAsync(state, fullPath).ConfigureAwait(false);
                    if (newScriptState != null)
                    {
                        return CompleteExecution(state.WithScriptState(newScriptState), operation, success: newScriptState.Exception == null);
                    }
                }

                return CompleteExecution(state, operation, success: false);
            }

            /// <summary>
            /// Executes specified script file as a submission.
            /// </summary>
            /// <remarks>
            /// All errors are written to the error output stream.
            /// Uses source search paths to resolve unrooted paths.
            /// </remarks>
            private async Task<ScriptState<object>> TryExecuteFileAsync(EvaluationState state, string fullPath)
            {
                Debug.Assert(PathUtilities.IsAbsolute(fullPath));

                string content = null;
                try
                {
                    using (var reader = File.OpenText(fullPath))
                    {
                        content = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    // file read errors:
                    Console.Error.WriteLine(e.Message);
                    return null;
                }

                Script<object> script = TryCompile(state.ScriptStateOpt?.Script, content, fullPath, state.ScriptOptions);
                if (script == null)
                {
                    // compilation errors:
                    return null;
                }

                return await ExecuteOnUIThread(script, state.ScriptStateOpt, displayResult: false).ConfigureAwait(false);
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

            private async Task<ScriptState<object>> ExecuteOnUIThread(Script<object> script, ScriptState<object> stateOpt, bool displayResult)
            {
                return await ((Task<ScriptState<object>>)s_control.Invoke(
                    (Func<Task<ScriptState<object>>>)(async () =>
                    {
                        var task = (stateOpt == null) ?
                            script.RunAsync(_globals, catchException: e => true, cancellationToken: CancellationToken.None) :
                            script.RunFromAsync(stateOpt, catchException: e => true, cancellationToken: CancellationToken.None);

                        var newState = await task.ConfigureAwait(false);

                        if (newState.Exception != null)
                        {
                            DisplayException(newState.Exception);
                        }
                        else if (displayResult && newState.Script.HasReturnValue())
                        {
                            _globals.Print(newState.ReturnValue);
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

                var formatter = _replServiceProvider.DiagnosticFormatter;

                foreach (var diagnostic in displayedDiagnostics)
                {
                    output.WriteLine(formatter.Format(diagnostic, _culture));
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

            public void RemoteConsoleWrite(byte[] data, bool isError)
            {
                using (var stream = isError ? Console.OpenStandardError() : Console.OpenStandardOutput())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
            }

            public bool IsShadowCopy(string path)
            {
                return _metadataFileProvider.IsShadowCopy(path);
            }

            #endregion
        }
    }
}
