// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Scripting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        /// <summary>
        /// A remote singleton server-activated object that lives in the interactive host process and controls it.
        /// </summary>
        internal sealed class Service : MarshalByRefObject
        {
            private static readonly ManualResetEventSlim s_clientExited = new ManualResetEventSlim(false);

            // Signaled when UI thread is ready to process messages.
            private static readonly ManualResetEventSlim s_uiReady = new ManualResetEventSlim(false);

            // A WinForms control that enables us to execute code on UI thread.
            // TODO (tomat): consider removing dependency on WinForms.
            private static Control s_ui;

            internal static readonly ImmutableArray<string> DefaultSourceSearchPaths =
                ImmutableArray.Create<string>(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            private readonly InteractiveAssemblyLoader _assemblyLoader;
            private readonly MetadataShadowCopyProvider _metadataFileProvider;

            // the search paths - updated from the hostObject
            private ImmutableArray<string> _sourceSearchPaths;

            private ObjectFormatter _objectFormatter;
            private IRepl _repl;
            private InteractiveHostObject _hostObject;
            private ObjectFormattingOptions _formattingOptions;

            // Session is not thread-safe by itself, 
            // so we need to lock whenever we compile a submission or add a reference:
            private readonly object _sessionGuard = new object();
            private ScriptOptions _options = ScriptOptions.Default;
            private ScriptState _lastResult;

            #region Setup

            public Service()
            {
                // TODO (tomat): we should share the copied files with the host
                _metadataFileProvider = new MetadataShadowCopyProvider();
                _assemblyLoader = new InteractiveAssemblyLoader(_metadataFileProvider);
                _sourceSearchPaths = DefaultSourceSearchPaths;
                _formattingOptions = new ObjectFormattingOptions(
                    memberFormat: MemberDisplayFormat.Inline,
                    quoteStrings: true,
                    useHexadecimalNumbers: false,
                    maxOutputLength: 200,
                    memberIndentation: "  ");
            }

            public override object InitializeLifetimeService()
            {
                return null;
            }

            public void Initialize(Type replType)
            {
                Contract.ThrowIfNull(replType);

                _repl = (IRepl)Activator.CreateInstance(replType);
                _objectFormatter = _repl.CreateObjectFormatter();

                _hostObject = new InteractiveHostObject();

                _options = _options
                                   .WithBaseDirectory(Directory.GetCurrentDirectory())
                                   .AddReferences(_hostObject.GetType().Assembly);

                _hostObject.ReferencePaths.AddRange(_options.SearchPaths);
                _hostObject.SourcePaths.AddRange(_sourceSearchPaths);

                Console.OutputEncoding = Encoding.UTF8;
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
                if (Environment.OSVersion.Version >= new System.Version(6, 1, 0, 0))
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

                        var uiThread = new Thread(UIThread);
                        uiThread.SetApartmentState(ApartmentState.STA);
                        uiThread.IsBackground = true;
                        uiThread.Start();
                        s_uiReady.Wait();

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

            private static void UIThread()
            {
                s_ui = new Control();
                s_ui.CreateControl();

                s_uiReady.Set();
                Application.Run();
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
                RemoteAsyncOperation<object> operation,
                string[] referenceSearchPaths,
                string[] sourceSearchPaths,
                string baseDirectory)
            {
                Debug.Assert(operation != null);
                Debug.Assert(referenceSearchPaths != null);
                Debug.Assert(sourceSearchPaths != null);
                Debug.Assert(baseDirectory != null);

                lock (_sessionGuard)
                {
                    _hostObject.ReferencePaths.Clear();
                    _hostObject.ReferencePaths.AddRange(referenceSearchPaths);
                    _options = _options.WithSearchPaths(referenceSearchPaths).WithBaseDirectory(baseDirectory);

                    _hostObject.SourcePaths.Clear();
                    _hostObject.SourcePaths.AddRange(sourceSearchPaths);
                    _sourceSearchPaths = sourceSearchPaths.AsImmutable();

                    Directory.SetCurrentDirectory(baseDirectory);
                }

                operation.Completed(null);
            }

            /// <summary>
            /// Reads given initialization file (.rsp) and loads and executes all assembly references and files, respectively specified in it.
            /// Execution is performed on the UI thread.
            /// </summary>
            [OneWay]
            public void InitializeContextAsync(RemoteAsyncOperation<RemoteExecutionResult> operation, string initializationFile, bool isRestarting)
            {
                Debug.Assert(operation != null);

                var success = false;
                try
                {
                    InitializeContext(initializationFile, isRestarting);

                    success = true;
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    CompleteExecution(operation, success);
                }
            }

            private string ResolveReferencePath(string reference, string baseFilePath)
            {
                var references = _options.ReferenceResolver.ResolveReference(reference, baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                if (references.IsDefaultOrEmpty)
                {
                    return null;
                }

                return references.Single().FilePath;
            }

            /// <summary>
            /// Adds an assembly reference to the current session.
            /// </summary>
            [OneWay]
            public void AddReferenceAsync(RemoteAsyncOperation<bool> operation, string reference)
            {
                Debug.Assert(operation != null);
                Debug.Assert(reference != null);

                var success = false;
                try
                {
                    // TODO (tomat): This lock blocks all other session operations. 
                    // We should be able to run multiple assembly resolutions and code execution in parallel.
                    string fullPath;
                    lock (_sessionGuard)
                    {
                        fullPath = ResolveReferencePath(reference, baseFilePath: null);
                        if (fullPath != null)
                        {
                            success = LoadReference(fullPath, suppressWarnings: false, addReference: true);
                        }
                    }

                    if (fullPath == null)
                    {
                        Console.Error.WriteLine(string.Format(FeaturesResources.CannotResolveReference, reference));
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
            }

            /// <summary>
            /// Executes given script snippet on the UI thread in the context of the current session.
            /// </summary>
            [OneWay]
            public void ExecuteAsync(RemoteAsyncOperation<RemoteExecutionResult> operation, string text)
            {
                Debug.Assert(operation != null);
                Debug.Assert(text != null);

                var success = false;
                try
                {
                    success = Execute(text);
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    CompleteExecution(operation, success);
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

                string fullPath = null;
                bool success = false;
                try
                {
                    fullPath = ResolveRelativePath(path, _options.BaseDirectory, displayPath: false);
                    success = fullPath != null && ExecuteFile(fullPath);
                }
                catch (Exception e)
                {
                    ReportUnhandledException(e);
                }
                finally
                {
                    CompleteExecution(operation, success, fullPath);
                }
            }

            private void CompleteExecution(RemoteAsyncOperation<RemoteExecutionResult> operation, bool success, string resolvedPath = null)
            {
                // TODO (tomat): we should be resetting this info just before the execution to ensure that the services see the same
                // as the next execution.

                // send any updates to the host object and current directory back to the client:
                var newSourcePaths = _hostObject.SourcePaths.List.GetNewContent();
                var newReferencePaths = _hostObject.ReferencePaths.List.GetNewContent();
                var currentDirectory = Directory.GetCurrentDirectory();
                var oldWorkingDirectory = _options.BaseDirectory;
                var newWorkingDirectory = (oldWorkingDirectory != currentDirectory) ? currentDirectory : null;

                // update local search paths, the client updates theirs on operation completion:

                if (newSourcePaths != null)
                {
                    _sourceSearchPaths = newSourcePaths.AsImmutable();
                }

                if (newReferencePaths != null)
                {
                    _options = _options.WithSearchPaths(newReferencePaths);
                }

                _options = _options.WithBaseDirectory(currentDirectory);

                operation.Completed(new RemoteExecutionResult(success, newSourcePaths, newReferencePaths, newWorkingDirectory, resolvedPath));
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

            public ObjectFormattingOptions ObjectFormattingOptions
            {
                get
                {
                    return _formattingOptions;
                }

                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }

                    _formattingOptions = value;
                }
            }

            /// <summary>
            /// Loads references, set options and execute files specified in the initialization file.
            /// Also prints logo unless <paramref name="isRestarting"/> is true.
            /// </summary>
            private void InitializeContext(string initializationFileOpt, bool isRestarting)
            {
                Debug.Assert(initializationFileOpt == null || PathUtilities.IsAbsolute(initializationFileOpt));

                // TODO (tomat): this is also done in CommonInteractiveEngine, perhaps we can pass the parsed command lines to here?

                if (!isRestarting)
                {
                    Console.Out.WriteLine(_repl.GetLogo());
                }

                if (File.Exists(initializationFileOpt))
                {
                    Console.Out.WriteLine(string.Format(FeaturesResources.LoadingContextFrom, Path.GetFileName(initializationFileOpt)));
                    var parser = _repl.GetCommandLineParser();

                    // The base directory for relative paths is the directory that contains the .rsp file.
                    // Note that .rsp files included by this .rsp file will share the base directory (Dev10 behavior of csc/vbc).
                    var args = parser.Parse(new[] { "@" + initializationFileOpt }, Path.GetDirectoryName(initializationFileOpt), RuntimeEnvironment.GetRuntimeDirectory(), null /* TODO: pass a valid value*/);

                    foreach (var error in args.Errors)
                    {
                        var writer = (error.Severity == DiagnosticSeverity.Error) ? Console.Error : Console.Out;
                        writer.WriteLine(error.GetMessage(CultureInfo.CurrentCulture));
                    }

                    if (args.Errors.Length == 0)
                    {
                        // TODO (tomat): other arguments
                        // TODO (tomat): parse options

                        lock (_sessionGuard)
                        {
                            // TODO (tomat): consolidate with other reference resolving
                            foreach (CommandLineReference cmdLineReference in args.MetadataReferences)
                            {
                                // interactive command line parser doesn't accept modules or linked assemblies
                                Debug.Assert(cmdLineReference.Properties.Kind == MetadataImageKind.Assembly && !cmdLineReference.Properties.EmbedInteropTypes);

                                string fullPath = ResolveReferencePath(cmdLineReference.Reference, baseFilePath: null);
                                LoadReference(fullPath, suppressWarnings: true, addReference: true);
                            }
                        }

                        var rspDirectory = Path.GetDirectoryName(initializationFileOpt);
                        foreach (CommandLineSourceFile file in args.SourceFiles)
                        {
                            // execute all files as scripts (matches csi/vbi semantics)

                            string fullPath = ResolveRelativePath(file.Path, rspDirectory, displayPath: true);
                            if (fullPath != null)
                            {
                                ExecuteFile(fullPath);
                            }
                        }
                    }
                }

                if (!isRestarting)
                {
                    Console.Out.WriteLine(FeaturesResources.TypeHelpForMoreInformation);
                }
            }

            private string ResolveRelativePath(string path, string baseDirectory, bool displayPath)
            {
                List<string> attempts = new List<string>();
                Func<string, bool> fileExists = file =>
                {
                    attempts.Add(file);
                    return File.Exists(file);
                };

                string fullPath = FileUtilities.ResolveRelativePath(path, null, baseDirectory, _sourceSearchPaths, fileExists);
                if (fullPath == null)
                {
                    if (displayPath)
                    {
                        Console.Error.WriteLine(FeaturesResources.SpecifiedFileNotFoundFormat, path);
                    }
                    else
                    {
                        Console.Error.WriteLine(FeaturesResources.SpecifiedFileNotFound);
                    }

                    if (attempts.Count > 0)
                    {
                        DisplaySearchPaths(Console.Error, attempts);
                    }
                }

                return fullPath;
            }

            private bool LoadReference(string fullOriginalPath, bool suppressWarnings, bool addReference)
            {
                AssemblyLoadResult result;
                try
                {
                    result = LoadFromPathThrowing(fullOriginalPath, addReference);
                }
                catch (FileNotFoundException e)
                {
                    Console.Error.WriteLine(e.Message);
                    return false;
                }
                catch (ArgumentException e)
                {
                    Console.Error.WriteLine((e.InnerException ?? e).Message);
                    return false;
                }
                catch (TargetInvocationException e)
                {
                    // The user might have hooked AssemblyResolve event, which might have thrown an exception.
                    // Display stack trace in this case.
                    Console.Error.WriteLine(e.InnerException.ToString());
                    return false;
                }

                if (!result.IsSuccessful && !suppressWarnings)
                {
                    Console.Out.WriteLine(string.Format(CultureInfo.CurrentCulture, FeaturesResources.RequestedAssemblyAlreadyLoaded, result.OriginalPath));
                }

                return true;
            }

            // Testing utility.
            // TODO (tomat): needed since MetadataReference is not serializable . 
            // Has to be public to be callable via remoting.
            public AssemblyLoadResult LoadReferenceThrowing(string reference, bool addReference)
            {
                var fullPath = ResolveReferencePath(reference, baseFilePath: null);
                if (fullPath == null)
                {
                    throw new FileNotFoundException(message: null, fileName: reference);
                }

                return LoadFromPathThrowing(fullPath, addReference);
            }

            private AssemblyLoadResult LoadFromPathThrowing(string fullOriginalPath, bool addReference)
            {
                var result = _assemblyLoader.LoadFromPath(fullOriginalPath);
                if (addReference && result.IsSuccessful)
                {
                    var reference = _metadataFileProvider.GetReference(fullOriginalPath);
                    _options = _options.AddReferences(reference);
                }

                return result;
            }

            public ObjectHandle ExecuteAndWrap(string text)
            {
                return new ObjectHandle(ExecuteInner(Compile(text)));
            }

            private Script Compile(string text, string path = null)
            {
                // note that the actual submission execution runs on the UI thread, not under this lock:
                lock (_sessionGuard)
                {
                    Script script = _repl.CreateScript(text).WithOptions(_options);

                    if (_lastResult != null)
                    {
                        script = script.WithPrevious(_lastResult.Script);
                    }
                    else
                    {
                        script = script.WithGlobalsType(_hostObject.GetType());
                    }

                    if (path != null)
                    {
                        script = script.WithPath(path).WithOptions(script.Options.WithIsInteractive(false));
                    }

                    // force build so exception is thrown now if errors are found.
                    script.Build();

                    // load all references specified in #r's -- they will all be PE references (may be shadow copied):
                    foreach (PortableExecutableReference reference in script.GetCompilation().DirectiveReferences)
                    {
                        // FullPath refers to the original reference path, not the copy:
                        LoadReference(reference.FilePath, suppressWarnings: false, addReference: false);
                    }

                    return script;
                }
            }

            /// <summary>
            /// Executes specified script file as a submission.
            /// </summary>
            /// <param name="fullPath">Full source path.</param>
            /// <returns>True if the code has been executed. False if the code doesn't compile.</returns>
            /// <remarks>
            /// All errors are written to the error output stream.
            /// Uses source search paths to resolve unrooted paths.
            /// </remarks>
            private bool ExecuteFile(string fullPath)
            {
                Debug.Assert(PathUtilities.IsAbsolute(fullPath));

                string content;
                try
                {
                    content = File.ReadAllText(fullPath);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    return false;
                }

                // TODO (tomat): engine.CompileSubmission shouldn't throw
                Script script;
                try
                {
                    script = Compile(content, fullPath);
                }
                catch (CompilationErrorException e)
                {
                    DisplayInteractiveErrors(e.Diagnostics, Console.Error);
                    return false;
                }

                object result;
                ExecuteOnUIThread(script, out result);
                return true;
            }

            private void DisplaySearchPaths(TextWriter writer, List<string> attemptedFilePaths)
            {
                writer.WriteLine(attemptedFilePaths.Count == 1 ?
                    FeaturesResources.SearchedInDirectory :
                    FeaturesResources.SearchedInDirectories);

                foreach (string path in attemptedFilePaths)
                {
                    writer.Write("  ");
                    writer.WriteLine(Path.GetDirectoryName(path));
                }
            }

            /// <summary>
            /// Executes specified code.
            /// </summary>
            /// <param name="text">Source code.</param>
            /// <returns>True if the code has been executed. False if the code doesn't compile.</returns>
            /// <remarks>
            /// All errors are written to the error output stream.
            /// The resulting value (if any) is formatted and printed to the output stream.
            /// </remarks>
            private bool Execute(string text)
            {
                Script script;
                try
                {
                    script = Compile(text);
                }
                catch (CompilationErrorException e)
                {
                    DisplayInteractiveErrors(e.Diagnostics, Console.Error);
                    return false;
                }

                object result;
                if (!ExecuteOnUIThread(script, out result))
                {
                    return true;
                }

                bool hasValue;
                var resultType = script.GetCompilation().GetSubmissionResultType(out hasValue);
                if (hasValue)
                {
                    if (resultType != null && resultType.SpecialType == SpecialType.System_Void)
                    {
                        Console.Out.WriteLine(_objectFormatter.VoidDisplayString);
                    }
                    else
                    {
                        Console.Out.WriteLine(_objectFormatter.FormatObject(result, _formattingOptions));
                    }
                }

                return true;
            }

            private class ExecuteSubmissionError
            {
                public readonly Exception Exception;

                public ExecuteSubmissionError(Exception exception)
                {
                    this.Exception = exception;
                }
            }

            private bool ExecuteOnUIThread(Script script, out object result)
            {
                result = s_ui.Invoke(new Func<object>(() =>
                {
                    try
                    {
                        return ExecuteInner(script);
                    }
                    catch (Exception e)
                    {
                        return new ExecuteSubmissionError(e);
                    }
                }));

                var error = result as ExecuteSubmissionError;
                if (error != null)
                {
                    // TODO (tomat): format exception
                    Console.Error.WriteLine(error.Exception);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            private object ExecuteInner(Script script)
            {
                var globals = _lastResult != null ? (object)_lastResult : (object)_hostObject;
                var result = script.Run(globals);
                _lastResult = result;
                return result.ReturnValue;
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

                var formatter = _repl.GetDiagnosticFormatter();

                foreach (var diagnostic in displayedDiagnostics)
                {
                    output.WriteLine(formatter.Format(diagnostic, output.FormatProvider as CultureInfo));
                }

                if (diagnostics.Length > MaxErrorCount)
                {
                    int notShown = diagnostics.Length - MaxErrorCount;
                    output.WriteLine(string.Format(output.FormatProvider, FeaturesResources.PlusAdditional, notShown, (notShown == 1) ? "error" : "errors"));
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
