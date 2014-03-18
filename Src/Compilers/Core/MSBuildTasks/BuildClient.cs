// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CompilerServer;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Client class that handles communication to the server.
    /// </summary>
    internal class BuildClient
    {
        // Holds the expected name of the server process we connect to and/or start.
        private readonly string serverExecutablePath;

        private const int TimeOutMsExistingProcess = 1000;  // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        private const int TimeOutMsNewProcess = 20000;  // Spend up to 20s connecting to a new process, to allow time for it to start.

        public BuildClient()
        {
            CompilerServerLogger.Initialize("TSK");   // Mark log file entries as from MSBuild Task.
            this.serverExecutablePath = GetExpectedServerExecutablePath();
        }

        /// <summary>
        /// This won't be present in an end-to-end build of VS with Roslyn
        /// but is necessary when we're in a VSIX since all we know is that
        /// the compiler server executable should be in the same location
        /// as the client assembly.
        /// </summary>
        private static string GetExpectedServerExecutablePath()
        {
            string location = typeof(BuildClient).Assembly.Location;
            string directory = Path.GetDirectoryName(location);
            return Path.Combine(directory, BuildProtocolConstants.ServerExeName);
        }

        public async Task<BuildResponse> GetResponseAsync(BuildRequest req, 
                                                     CancellationToken cancellationToken)
        {
            NamedPipeClientStream pipeStream;
            if (TryAutoConnectToServer(cancellationToken, out pipeStream))
            {
                // We have a good connection
                BuildResponse response = await DoCompilationAsync(pipeStream, req, cancellationToken).ConfigureAwait(false);
                if (response != null)
                {
                    return response;
                }
                else
                {
                    CompilerServerLogger.Log("Compilation failed, constructing new compiler server");
                    // The compilation failed. There are a couple possible reasons for this,
                    // including that we are using a 32-bit compiler server and we are out of 
                    // memory. This is the last attempt -- we will create a new server manually
                    // and try to compile. There is no mutex because anyone else using
                    // this server is accidental only.
                    int newProcessId = CreateNewServerProcess();
                    if (newProcessId != 0 &&
                        TryConnectToProcess(newProcessId,
                                            TimeOutMsNewProcess,
                                            cancellationToken,
                                            out pipeStream))
                    {
                        return await DoCompilationAsync(pipeStream, req, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Connect to a running compiler server or automatically start a new one.
        /// Throws on cancellation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for request.</param>
        /// <param name="pipeStream">
        /// A <see cref="NamedPipeClientStream"/> connected to a compiler server process
        /// or null on failure.
        /// </param>
        /// <returns>
        /// </returns>
        private bool TryAutoConnectToServer(CancellationToken cancellationToken,
                                            out NamedPipeClientStream pipeStream)
        {
            pipeStream = null;
            cancellationToken.ThrowIfCancellationRequested();

            CompilerServerLogger.Log("Creating mutex.");
            // We should hold the mutex when starting a new process
            bool haveMutex;
            var singleServerMutex = new Mutex(initiallyOwned: true,
                                              name: serverExecutablePath.Replace('\\', '/'),
                                              createdNew: out haveMutex);

            try
            {
                if (!haveMutex)
                {
                    CompilerServerLogger.Log("Waiting for mutex.");
                    try
                    {
                        haveMutex = singleServerMutex.WaitOne(TimeOutMsNewProcess);
                    }
                    catch (AbandonedMutexException)
                    {
                        // Someone abandoned the mutex, but we still own it
                        // Log and continue
                        CompilerServerLogger.Log("Acquired mutex, but mutex was previously abandoned.");
                        haveMutex = true;
                    }
                }

                if (haveMutex)
                {
                    CompilerServerLogger.Log("Acquired mutex");
                    // First try to connect to an existing process
                    if (TryExistingProcesses(cancellationToken, out pipeStream))
                    {
                        // Release the mutex and get out
                        return true;
                    }

                    CompilerServerLogger.Log("Starting new process");
                    // No luck, start our own process.
                    int newProcessId = CreateNewServerProcess();
                    if (newProcessId != 0 &&
                        TryConnectToProcess(newProcessId,
                                            TimeOutMsNewProcess,
                                            cancellationToken,
                                            out pipeStream))
                    {
                        CompilerServerLogger.Log("Connected to new process");
                        // Release the mutex and get out
                        return true;
                    }
                }
            }
            finally
            {
                if (haveMutex)
                {
                    CompilerServerLogger.Log("Releasing mutex");
                    singleServerMutex.ReleaseMutex();
                }
            }
            return false;
        }

        /// <summary>
        /// Tries to connect to existing servers on the system.
        /// </summary>
        /// <returns>
        /// A <see cref="NamedPipeClientStream"/> on success, null on failure.
        /// </returns>
        private bool TryExistingProcesses(CancellationToken cancellationToken,
                                          out NamedPipeClientStream pipeStream)
        {
            CompilerServerLogger.Log("Trying existing processes.");
            pipeStream = null;
            foreach (int processId in GetAllProcessIds())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (TryConnectToProcess(processId,
                    TimeOutMsExistingProcess,
                    cancellationToken,
                    out pipeStream))
                {
                    CompilerServerLogger.Log("Found existing process");
                    return true;
                }
            }

            return false;
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
        /// "expectedProcessName".
        /// </summary>
        private List<int> GetAllProcessIds()
        {
            List<int> processIds = new List<int>();

            // Get all the processes with the right base name.
            Process[] allProcesses = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(serverExecutablePath));
            CompilerServerLogger.Log("Found {0} existing processes with matching base name", allProcesses.Length);

            foreach (Process process in allProcesses)
            {
                using (process)
                {
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
                            CompilerServerLogger.Log("Process file path: {0}", fullFileName);
                        }

                        if (fullFileName != null &&
                            string.Equals(fullFileName,
                                          serverExecutablePath,
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            processIds.Add(process.Id);
                        }
                    }
                    // If any exception occurs (e.g., accessing the process handle
                    // fails because the process is exiting), we should simply fail
                    // to connect and let the loop fall through
                    catch
                    { }
                }
            }
            return processIds;
        }

        private async Task<BuildResponse> DoCompilationAsync(NamedPipeClientStream pipeStream,
                                                        BuildRequest req,
                                                        CancellationToken cancellationToken)
        {
            using (pipeStream)
            {
                try
                {
                    // Start a monitor that cancels if the pipe closes on us
                    var monitorCancellation = new CancellationTokenSource();
                    Task disconnectMonitor = MonitorPipeForDisconnectionAsync(pipeStream, monitorCancellation.Token);

                    // Write the request.
                    CompilerServerLogger.Log("Writing request");
                    await req.WriteAsync(pipeStream, cancellationToken).ConfigureAwait(false);

                    // Read the response.
                    CompilerServerLogger.Log("Reading response");
                    BuildResponse response = await BuildResponse.ReadAsync(pipeStream, cancellationToken).ConfigureAwait(false);

                    // Stop monitoring pipe
                    monitorCancellation.Cancel(throwOnFirstException: true);
                    await disconnectMonitor.ConfigureAwait(false);

                    Debug.Assert(response != null);
                    CompilerServerLogger.Log("BuildResponse received; exit code={0}", response.ReturnCode);

                    return response;
                }
                catch (PipeBrokenException e)
                {
                    CompilerServerLogger.LogException(e, "Server process died; pipe broken.");
                    return null;
                }
                catch (ObjectDisposedException e)
                {
                    CompilerServerLogger.LogException(e, "Pipe stream unexpectedly disposed");
                    return null;
                }
            }
        }

        /// <summary>
        /// Connect to the given process id and return a pipe.
        /// Throws on cancellation.
        /// </summary>
        /// <param name="processId">Proces id to try to connect to.</param>
        /// <param name="timeoutMs">Timeout to allow in connecting to process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection to server.</param>
        /// <param name="pipeStream">
        /// An open <see cref="NamedPipeClientStream"/> to the server process or null on failure
        /// (including IOException).
        /// </param>
        /// <returns>
        /// </returns>
        private bool TryConnectToProcess(int processId,
       	                                 int timeoutMs,
                                         CancellationToken cancellationToken,
                                         out NamedPipeClientStream pipeStream)
        {
            pipeStream = null;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the pipe name followed by the process id.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                string pipeName = BuildProtocolConstants.PipeName + processId.ToString();
                CompilerServerLogger.Log("Attempt to open named pipe '{0}'", pipeName);

                pipeStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                cancellationToken.ThrowIfCancellationRequested();

                CompilerServerLogger.Log("Attempt to connect named pipe '{0}'", pipeName);
                pipeStream.Connect(timeoutMs);
                CompilerServerLogger.Log("Named pipe '{0}' connected", pipeName);

                cancellationToken.ThrowIfCancellationRequested();

                // Verify that we own the pipe.
                SecurityIdentifier currentIdentity = WindowsIdentity.GetCurrent().Owner;
                PipeSecurity remoteSecurity = pipeStream.GetAccessControl();
                IdentityReference remoteOwner = remoteSecurity.GetOwner(typeof(SecurityIdentifier));
                if (remoteOwner != currentIdentity)
                {
                    CompilerServerLogger.Log("Owner of named pipe is incorrect");
                    return false;
                }

                return true;
            }
            catch (IOException e)
            {
                CompilerServerLogger.LogException(e, "Opening/connecting named pipe");
                return false;
            }
            catch (TimeoutException e)
            {
                CompilerServerLogger.LogException(e, "Timeout while opening/connecting named pipe");
                return false;
            }
        }

        /// <summary>
        /// Create a new instance of the server process, returning its process ID.
        /// Returns 0 on failure.
        /// </summary>
        private int CreateNewServerProcess()
        {
            // As far as I can tell, there isn't a way to use the Process class to 
            // create a process with no stdin/stdout/stderr, so we use P/Invoke.
            // This code was taken from MSBuild task starting code.

            NativeMethods.STARTUPINFO startInfo = new NativeMethods.STARTUPINFO();
            startInfo.cb = Marshal.SizeOf(startInfo);
            startInfo.hStdError = NativeMethods.InvalidHandle;
            startInfo.hStdInput = NativeMethods.InvalidHandle;
            startInfo.hStdOutput = NativeMethods.InvalidHandle;
            startInfo.dwFlags = NativeMethods.STARTF_USESTDHANDLES;
            uint dwCreationFlags = NativeMethods.NORMAL_PRIORITY_CLASS | NativeMethods.CREATE_NO_WINDOW;

            NativeMethods.PROCESS_INFORMATION processInfo = new NativeMethods.PROCESS_INFORMATION();

            CompilerServerLogger.Log("Attempting to create process '{0}'", serverExecutablePath);

            bool success = NativeMethods.CreateProcess(
                serverExecutablePath,
                null,                                        // command line
                NativeMethods.NullPtr,                       // process attributes
                NativeMethods.NullPtr,                       // thread attributes
                false,                                       // don't inherit handles
                dwCreationFlags,
                NativeMethods.NullPtr,                       // inherit environment
                Path.GetDirectoryName(serverExecutablePath), // current directory
                ref startInfo,
                out processInfo);

            if (success)
            {
                CompilerServerLogger.Log("Successfully created process with process id {0}", processInfo.dwProcessId);
                NativeMethods.CloseHandle(processInfo.hProcess);
                NativeMethods.CloseHandle(processInfo.hThread);
                return processInfo.dwProcessId;
            }
            else
            {
                CompilerServerLogger.Log("Failed to create process. GetLastError={0}", Marshal.GetLastWin32Error());
                return 0;
            }
        }

        /// <summary>
        /// Get the process name of the server. It lives in the
        /// same directory as this task itself.
        /// </summary>
        /// <returns></returns>
        private static string GetExpectedProcessName()
        {
            string location = typeof(BuildClient).Assembly.Location;
            string path = Path.GetDirectoryName(location);
            return Path.Combine(path, BuildProtocolConstants.ServerExeName);
        }

        // The IsConnected property on named pipes does not detect when the client has disconnected
        // if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        // which serves to check the pipe for disconnection. 
        private async Task MonitorPipeForDisconnectionAsync(PipeStream pipeStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[0];

            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                CompilerServerLogger.Log("Before poking pipe.");
                try
                {
                    await pipeStream.ReadAsync(buffer, 0, 0).ConfigureAwait(continueOnCapturedContext: false);
                }
                catch (ObjectDisposedException)
                {
                    // Another thread may have closed the stream already.  Not a problem.
                    CompilerServerLogger.Log("Pipe has already been closed.");
                    return;
                }
                CompilerServerLogger.Log("After poking pipe.");
                // Wait a hundredth of a second before trying again
                await Task.Delay(10);
            }
                
            if (!cancellationToken.IsCancellationRequested)
            {
                throw new PipeBrokenException();
            }
        }

        public class PipeBrokenException : IOException { }
    }
}
