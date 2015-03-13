// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CompilerServer;
using static Microsoft.CodeAnalysis.BuildTasks.NativeMethods;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;
using static Microsoft.CodeAnalysis.CompilerServer.CompilerServerLogger;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal static class BuildClient
    {
        private const string s_serverName = PipeName + ".exe";
        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        private const int TimeOutMsExistingProcess = 1000;
        // Spend up to 20s connecting to a new process, to allow time for it to start.
        private const int TimeOutMsNewProcess = 20000; 

        /// <summary>
        /// Try to get the directory this assembly is in.
        /// </summary>
        private static string GetExpectedServerExeDir()
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            string assemblyPath = uri.IsFile 
                ? uri.LocalPath
                : Assembly.GetCallingAssembly().Location;
            return Path.GetDirectoryName(assemblyPath);
        }

        /// <summary>
        /// Run a compilation through the compiler server and print the output
        /// to the console. If the compiler server fails, run the fallback
        /// compiler.
        /// </summary>
        public static int RunWithConsoleOutput(
            string[] args,
            RequestLanguage language,
            Func<string[], int> fallbackCompiler)
        {
            var errorMessage = CommandLineParser.CheckClientArgsForErrors(args);

            if (errorMessage != null)
            {
                Console.Out.WriteLine(errorMessage);
                return CommonCompiler.Failed;
            }

            var responseTask = TryRunServerCompilation(
                language,
                Environment.CurrentDirectory,
                args,
                default(CancellationToken),
                libEnvVariable: Environment.GetEnvironmentVariable("LIB"));

            responseTask.Wait();

            int exitCode;
            var response = responseTask.Result;
            if (response != null)
            {
                exitCode = HandleResponse(response);
            }
            else
            {
                exitCode = fallbackCompiler(args.Where(arg => !arg.StartsWith("/keepalive", StringComparison.Ordinal)).ToArray());
            }

            return exitCode;
        }

        private static int HandleResponse(BuildResponse response)
        {
            if (response.Type == BuildResponse.ResponseType.Completed)
            {
                var completedResponse = (CompletedBuildResponse)response;
                var origEncoding = Console.OutputEncoding;
                try
                {
                    if (completedResponse.Utf8Output && Console.IsOutputRedirected)
                    {
                        Console.OutputEncoding = Encoding.UTF8;
                    }
                    Console.Out.Write(completedResponse.Output);
                    Console.Error.Write(completedResponse.ErrorOutput);
                }
                finally
                {
                    try
                    {
                        Console.OutputEncoding = origEncoding;
                    }
                    catch
                    { // Try to reset the output encoding, ignore if we can't
                    }
                }
                return completedResponse.ReturnCode;
            }
            else
            {
                Console.Error.WriteLine(CommandLineParser.MismatchedVersionErrorText);
                return CommonCompiler.Failed;
            }
        }

        /// <summary>
        /// Returns a Task with a null BuildResponse if no server
        /// response was received.
        /// </summary>
        public static Task<BuildResponse> TryRunServerCompilation(
            RequestLanguage language,
            string workingDir,
            IList<string> arguments,
            CancellationToken cancellationToken,
            string libEnvVariable = null)
        {
            try
            {
                NamedPipeClientStream pipe;

                var expectedServerExePath = Path.Combine(GetExpectedServerExeDir(), s_serverName);
                var mutexName = expectedServerExePath.Replace('\\', '/');
                bool holdsMutex;
                using (var mutex = new Mutex(initiallyOwned: true,
                                             name: mutexName,
                                             createdNew: out holdsMutex))
                {
                    try
                    {

                        if (!holdsMutex)
                        {
                            try
                            {
                                holdsMutex = mutex.WaitOne(TimeOutMsNewProcess,
                                    exitContext: false);
                            }
                            catch (AbandonedMutexException)
                            {
                                holdsMutex = true;
                            }
                        }

                        if (holdsMutex)
                        {
                            var request = BuildRequest.Create(language, workingDir, arguments, libEnvVariable);
                            // Check for already running processes in case someone came in before us
                            pipe = TryExistingProcesses(expectedServerExePath, cancellationToken);
                            if (pipe != null)
                            {
                                return TryCompile(pipe, request, cancellationToken);
                            }
                            else
                            {
                                int processId = TryCreateServerProcess(expectedServerExePath);
                                if (processId != 0 &&
                                    null != (pipe = TryConnectToProcess(processId,
                                                                        TimeOutMsNewProcess,
                                                                        cancellationToken)))
                                {
                                    // Let everyone else access our process
                                    mutex.ReleaseMutex();
                                    holdsMutex = false;

                                    return TryCompile(pipe, request, cancellationToken);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (holdsMutex)
                            mutex.ReleaseMutex();
                    }
                }
            }
            // Swallow all unhandled exceptions from server compilation. If
            // they are show-stoppers then they will crash the in-proc
            // compilation as well
            // TODO: Put in non-fatal Watson code so we still get info
            // when things unexpectedely fail
            catch { }
            return Task.FromResult<BuildResponse>(null);
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

                // Wait for the compilation and a monitor to dectect if the server disconnects
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
        /// Tries to connect to existing servers on the system.
        /// </summary>
        /// <returns>
        /// A <see cref="NamedPipeClientStream"/> on success, null on failure.
        /// </returns>
        private static NamedPipeClientStream TryExistingProcesses(
            string expectedProcessPath,
            CancellationToken cancellationToken)
        {
            Log("Trying existing processes.");
            foreach (int processId in GetAllProcessIds(expectedProcessPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pipeStream = TryConnectToProcess(
                    processId,
                    TimeOutMsExistingProcess,
                    cancellationToken);
                if (pipeStream != null)
                {
                    Log("Found existing process");
                    return pipeStream;
                }
            }

            return null;
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
            var buffer = new byte[0];

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
        /// Get the file path of the executable that started this process.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="flags">Should always be 0: Win32 path format.</param>
        /// <param name="exeNameBuffer">Buffer for the name</param>
        /// <param name="bufferSize">
        /// Size of the buffer coming in, chars written coming out.
        /// </param>
        [DllImport("Kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(
            IntPtr processHandle,
            int flags,
            StringBuilder exeNameBuffer,
            ref int bufferSize);

        private const int MAX_PATH_SIZE = 260;

        /// <summary>
        /// Get all process IDs on the current machine that have executable names matching
        /// the pipe name and started from the <paramref name="expectedPath" />.
        /// </summary>
        private static IEnumerable<int> GetAllProcessIds(string expectedPath)
        {
            // Get all the processes with the right base name.
            var allProcesses = Process.GetProcessesByName(PipeName);
            Log("Found {0} existing processes with matching base name", allProcesses.Length);

            try
            {
                foreach (Process process in allProcesses)
                {
                    int processId = 0;
                    try
                    {
                        var exeNameBuffer = new StringBuilder(MAX_PATH_SIZE);
                        int pathSize = MAX_PATH_SIZE;
                        string fullFileName = null;

                        if (QueryFullProcessImageName(process.Handle,
                                                      0, // Win32 path format
                                                      exeNameBuffer,
                                                      ref pathSize))
                        {
                            fullFileName = exeNameBuffer.ToString();
                            Log("Process file path: {0}", fullFileName);
                        }

                        if (string.Equals(fullFileName,
                                          expectedPath,
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            processId = process.Id;
                        }
                    }
                    // If any exception occurs (e.g., accessing the process handle
                    // fails because the process is exiting), we should simply fail
                    // to connect and let the loop fall through
                    catch
                    { }

                    if (processId != 0)
                        yield return processId;
                }
            }
            finally
            {
                foreach (var p in allProcesses)
                {
                    p.Dispose();
                }
            }
        }

        /// <summary>
        /// Connect to the given process id and return a pipe.
        /// Throws on cancellation.
        /// </summary>
        /// <param name="processId">Process id to try to connect to.</param>
        /// <param name="timeoutMs">Timeout to allow in connecting to process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection to server.</param>
        /// <returns>
        /// An open <see cref="NamedPipeClientStream"/> to the server process or null on failure.
        /// </returns>
        private static NamedPipeClientStream TryConnectToProcess(
            int processId,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            NamedPipeClientStream pipeStream;
            try
            {
                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the pipe name followed by the process id.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                string pipeName = PipeName + processId.ToString();
                Log("Attempt to open named pipe '{0}'", pipeName);

                pipeStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                cancellationToken.ThrowIfCancellationRequested();

                Log("Attempt to connect named pipe '{0}'", pipeName);
                pipeStream.Connect(timeoutMs);
                Log("Named pipe '{0}' connected", pipeName);

                cancellationToken.ThrowIfCancellationRequested();

                // Verify that we own the pipe.
                SecurityIdentifier currentIdentity = WindowsIdentity.GetCurrent().Owner;
                PipeSecurity remoteSecurity = pipeStream.GetAccessControl();
                IdentityReference remoteOwner = remoteSecurity.GetOwner(typeof(SecurityIdentifier));
                if (remoteOwner != currentIdentity)
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
        /// Create a new instance of the server process, returning its process ID.
        /// Returns 0 on failure.
        /// </summary>
        private static int TryCreateServerProcess(string expectedPath)
        {
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

            bool success = CreateProcess(
                expectedPath,
                null,            // command line
                NullPtr,         // process attributes
                NullPtr,         // thread attributes
                false,           // don't inherit handles
                dwCreationFlags,
                NullPtr,         // inherit environment
                Path.GetDirectoryName(expectedPath),    // current directory
                ref startInfo,
                out processInfo);

            if (success)
            {
                Log("Successfully created process with process id {0}", processInfo.dwProcessId);
                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);
                return processInfo.dwProcessId;
            }
            else
            {
                Log("Failed to create process. GetLastError={0}", Marshal.GetLastWin32Error());
                return 0;
            }
        }
    }
}
