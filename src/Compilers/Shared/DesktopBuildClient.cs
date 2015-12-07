// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CommandLine.CompilerServerLogger;
using static Microsoft.CodeAnalysis.CommandLine.NativeMethods;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal class DesktopBuildClient : BuildClient
    {
        private const string ServerName = "VBCSCompiler.exe";

        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        private const int TimeOutMsExistingProcess = 1000;

        // Spend up to 20s connecting to a new process, to allow time for it to start.
        private const int TimeOutMsNewProcess = 20000;

        private readonly RequestLanguage _language;
        private readonly CompileFunc _compileFunc;
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;

        protected DesktopBuildClient(RequestLanguage language, CompileFunc compileFunc, IAnalyzerAssemblyLoader analyzerAssemblyLoader)
        {
            _language = language;
            _compileFunc = compileFunc;
            _analyzerAssemblyLoader = analyzerAssemblyLoader;
        }

        internal static int Run(IEnumerable<string> arguments, IEnumerable<string> extraArguments, RequestLanguage language, CompileFunc compileFunc, IAnalyzerAssemblyLoader analyzerAssemblyLoader)
        {
            var client = new DesktopBuildClient(language, compileFunc, analyzerAssemblyLoader);
            var clientDir = AppDomain.CurrentDomain.BaseDirectory;
            var sdkDir = RuntimeEnvironment.GetRuntimeDirectory();
            var workingDir = Directory.GetCurrentDirectory();
            var buildPaths = new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir);
            var originalArguments = BuildClient.GetCommandLineArgs(arguments).Concat(extraArguments).ToArray();
            return client.RunCompilation(originalArguments, buildPaths);
        }

        protected override int RunLocalCompilation(List<string> arguments, string clientDir, string sdkDir)
        {
            return _compileFunc(clientDir, sdkDir, arguments.ToArray(), _analyzerAssemblyLoader);
        }

        protected override Task<BuildResponse> RunServerCompilation(
            List<string> arguments, 
            BuildPaths buildPaths, 
            string keepAlive, 
            string libDirectory, 
            CancellationToken cancellationToken)
        {
            var pipeName = GetPipeName(buildPaths.ClientDirectory);
            return RunServerCompilationCore(_language, arguments, buildPaths, pipeName, keepAlive, libDirectory, TryCreateServer, cancellationToken);
        }

        public static Task<BuildResponse> RunServerCompilation(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string keepAlive,
            string libEnvVariable,
            CancellationToken cancellationToken)
        {
            return RunServerCompilationCore(
                language,
                arguments,
                buildPaths,
                GetPipeNameFromFileInfo(buildPaths.ClientDirectory),
                keepAlive,
                libEnvVariable,
                TryCreateServerCore,
                cancellationToken);
        }

        private static Task<BuildResponse> RunServerCompilationCore(
            RequestLanguage language,
            List<string> arguments,
            BuildPaths buildPaths,
            string pipeName,
            string keepAlive,
            string libEnvVariable,
            Func<string, string, bool> tryCreateServerFunc,
            CancellationToken cancellationToken)
        {

            var clientDir = buildPaths.ClientDirectory;

            var clientMutexName = BuildProtocolConstants.GetClientMutexName(pipeName);
            bool holdsMutex;
            using (var clientMutex = new Mutex(initiallyOwned: true,
                                               name: clientMutexName,
                                               createdNew: out holdsMutex))
            {
                try
                {
                    if (!holdsMutex)
                    {
                        try
                        {
                            holdsMutex = clientMutex.WaitOne(TimeOutMsNewProcess);

                            if (!holdsMutex)
                            {
                                return Task.FromResult<BuildResponse>(null);
                            }
                        }
                        catch (AbandonedMutexException)
                        {
                            holdsMutex = true;
                        }
                    }

                    // Check for an already running server
                    var serverMutexName = BuildProtocolConstants.GetServerMutexName(pipeName);
                    Mutex mutexIgnore;
                    bool wasServerRunning = Mutex.TryOpenExisting(serverMutexName, out mutexIgnore);
                    var timeout = wasServerRunning ? TimeOutMsExistingProcess : TimeOutMsNewProcess;

                    NamedPipeClientStream pipe = null;

                    if (wasServerRunning || tryCreateServerFunc(clientDir, pipeName))
                    {
                        pipe = TryConnectToServer(pipeName,
                                                   timeout,
                                                   cancellationToken);
                    }

                    if (pipe != null)
                    {
                        var request = BuildRequest.Create(language,
                                                          buildPaths.WorkingDirectory,
                                                          arguments,
                                                          keepAlive,
                                                          libEnvVariable);

                        return TryCompile(pipe, request, cancellationToken);
                    }
                }
                finally
                {
                    if (holdsMutex)
                    {
                        clientMutex.ReleaseMutex();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Try to compile using the server. Returns null if a response from the
        /// server cannot be retrieved.
        /// </summary>
        private static async Task<BuildResponse> TryCompile(NamedPipeClientStream pipeStream,
                                                            BuildRequest request,
                                                            CancellationToken cancellationToken)
        {
            BuildResponse response;
            using (pipeStream)
            {
                // Write the request
                try
                {
                    Log("Begin writing request");
                    await request.WriteAsync(pipeStream, cancellationToken).ConfigureAwait(false);
                    Log("End writing request");
                }
                catch (Exception e)
                {
                    LogException(e, "Error writing build request.");
                    return null;
                }

                // Wait for the compilation and a monitor to detect if the server disconnects
                var serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Log("Begin reading response");

                var responseTask = BuildResponse.ReadAsync(pipeStream, serverCts.Token);
                var monitorTask = CreateMonitorDisconnectTask(pipeStream, serverCts.Token);
                await Task.WhenAny(responseTask, monitorTask).ConfigureAwait(false);

                Log("End reading response");

                if (responseTask.IsCompleted)
                {
                    // await the task to log any exceptions
                    try
                    {
                        response = await responseTask.ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        LogException(e, "Error reading response");
                        response = null;
                    }
                }
                else
                {
                    Log("Server disconnect");
                    response = null;
                }

                // Cancel whatever task is still around
                serverCts.Cancel();
                return response;
            }
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection.
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        private static async Task CreateMonitorDisconnectTask(
            NamedPipeClientStream pipeStream,
            CancellationToken cancellationToken)
        {
            // Ignore this warning because the desktop projects don't target 4.6 yet
#pragma warning disable RS0007 // Avoid zero-length array allocations.
            var buffer = new byte[0];
#pragma warning restore RS0007 // Avoid zero-length array allocations.

            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                // Wait a tenth of a second before trying again
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                try
                {
                    Log("Before poking pipe.");
                    await pipeStream.ReadAsync(buffer, 0, 0, cancellationToken).ConfigureAwait(false);
                    Log("After poking pipe.");
                }
                // Ignore cancellation
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    // It is okay for this call to fail.  Errors will be reflected in the
                    // IsConnected property which will be read on the next iteration of the
                    LogException(e, "Error poking pipe");
                }
            }
        }

        /// <summary>
        /// Connect to the pipe for a given directory and return it.
        /// Throws on cancellation.
        /// </summary>
        /// <param name="pipeName">Name of the named pipe to connect to.</param>
        /// <param name="timeoutMs">Timeout to allow in connecting to process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection to server.</param>
        /// <returns>
        /// An open <see cref="NamedPipeClientStream"/> to the server process or null on failure.
        /// </returns>
        private static NamedPipeClientStream TryConnectToServer(
            string pipeName,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            NamedPipeClientStream pipeStream;
            try
            {
                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the SHA1 of the directory the compiler exes live in as the pipe name.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                Log("Attempt to open named pipe '{0}'", pipeName);

                pipeStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                cancellationToken.ThrowIfCancellationRequested();

                Log("Attempt to connect named pipe '{0}'", pipeName);
                pipeStream.Connect(timeoutMs);
                Log("Named pipe '{0}' connected", pipeName);

                cancellationToken.ThrowIfCancellationRequested();

                // Verify that we own the pipe.
                if (!CheckPipeConnectionOwnership(pipeStream))
                {
                    Log("Owner of named pipe is incorrect");
                    return null;
                }

                return pipeStream;
            }
            catch (Exception e) when (!(e is TaskCanceledException))
            {
                LogException(e, "Exception while connecting to process");
                return null;
            }
        }

        /// <summary>
        /// Create a new instance of the server process, returning true on success
        /// and false otherwise.
        /// </summary>
        protected virtual bool TryCreateServer(string clientDir, string pipeName)
        {
            return TryCreateServerCore(clientDir, pipeName);
        }

        internal static bool TryCreateServerCore(string clientDir, string pipeName)
        { 
            // The server should be in the same directory as the client
            string expectedPath = Path.Combine(clientDir, ServerName);

            if (!File.Exists(expectedPath))
                return false;

            // As far as I can tell, there isn't a way to use the Process class to
            // create a process with no stdin/stdout/stderr, so we use P/Invoke.
            // This code was taken from MSBuild task starting code.

            STARTUPINFO startInfo = new STARTUPINFO();
            startInfo.cb = Marshal.SizeOf(startInfo);
            startInfo.hStdError = InvalidIntPtr;
            startInfo.hStdInput = InvalidIntPtr;
            startInfo.hStdOutput = InvalidIntPtr;
            startInfo.dwFlags = STARTF_USESTDHANDLES;
            uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW;

            PROCESS_INFORMATION processInfo;

            Log("Attempting to create process '{0}'", expectedPath);

            var builder = new StringBuilder($@"""{expectedPath}"" ""-pipename:{pipeName}""");

            bool success = CreateProcess(
                lpApplicationName: null,
                lpCommandLine: builder,
                lpProcessAttributes: NullPtr,
                lpThreadAttributes: NullPtr,
                bInheritHandles: false,
                dwCreationFlags: dwCreationFlags,
                lpEnvironment: NullPtr, // Inherit environment
                lpCurrentDirectory: clientDir,
                lpStartupInfo: ref startInfo,
                lpProcessInformation: out processInfo);

            if (success)
            {
                Log("Successfully created process with process id {0}", processInfo.dwProcessId);
                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);
            }
            else
            {
                Log("Failed to create process. GetLastError={0}", Marshal.GetLastWin32Error());
            }
            return success;
        }

        /// <summary>
        /// Check to ensure that the named pipe server we connected to is owned by the same
        /// user.
        /// </summary>
        /// <remarks>
        /// The type is embedded in assemblies that need to run cross platform.  While this particular
        /// code will never be hit when running on non-Windows platforms it does need to work when
        /// on Windows.  To facilitate that we use reflection to make the check here to enable it to
        /// compile into our cross plat assemblies.
        /// </remarks>
        private static bool CheckPipeConnectionOwnership(NamedPipeClientStream pipeStream)
        {
            try
            {
                var currentIdentity = WindowsIdentity.GetCurrent();
                var currentOwner = currentIdentity.Owner;
                var remotePipeSecurity = pipeStream.GetAccessControl();
                var remoteOwner = remotePipeSecurity.GetOwner(typeof(SecurityIdentifier));
                return currentOwner.Equals(remoteOwner);
            }
            catch (Exception ex)
            {
                Log("Exception checking pipe connection: {0}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Given the full path to the directory containing the compiler exes,
        /// retrieves the name of the pipe for client/server communication on
        /// that instance of the compiler.
        /// </summary>
        protected virtual string GetPipeName(string compilerExeDirectory)
        {
            return GetPipeNameFromFileInfo(compilerExeDirectory);
        }

        internal static string GetPipeNameFromFileInfo(string compilerExeDirectory)
        { 
            string basePipeName;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(compilerExeDirectory));
                basePipeName = Convert.ToBase64String(bytes)
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }

            // Prefix with username and elevation
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            var userName = Environment.UserName;
            return $"{userName}.{isAdmin}.{basePipeName}";
        }
    }
}
