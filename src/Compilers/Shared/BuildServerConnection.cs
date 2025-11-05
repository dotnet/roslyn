// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CommandLine.NativeMethods;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal sealed class BuildServerConnection
    {
        /// <summary>
        /// The time to wait for a named pipe connection to complete to an existing server 
        /// process.
        /// </summary>
        /// <remarks>
        /// The compiler server is designed to be responsive to new connections so in ideal 
        /// circumstances a timeout as short as one second is fine. However, in practice the
        /// server can become temporarily unresponsive if say the machine is under heavy load
        /// or a connection occurs just as a gen2 GC occurs.
        ///
        /// In any of these cases abandoning the connection attempt means falling back to 
        /// starting csc.exe / vbc.exe which will likely make the above problems. That will 
        /// create a new process that adds more load to the system. 
        /// 
        /// As such this timeout should be significantly longer than the average gen2 pause
        /// time for the server. When changing this value consider profiling building 
        /// Roslyn.sln and consulting the GC stats to see what a typical pause time is.
        /// </remarks>
        internal const int TimeOutMsExistingProcess = 5_000;

        /// <summary>
        /// The time to wait for a named pipe connection to complete for a newly started server
        /// </summary>
        internal const int TimeOutMsNewProcess = 20_000;

        /// <summary>
        /// The time to wait after starting the server process to check if it exits immediately
        /// with a FrameworkMissingFailure error code.
        /// </summary>
        internal const int ProcessCheckTimeoutMs = 2_000;

        /// <summary>
        /// Exit code indicating the .NET runtime is missing or incompatible (0x80008096).
        /// See https://github.com/dotnet/runtime/blob/main/docs/design/features/host-error-codes.md
        /// </summary>
        internal const int FrameworkMissingFailure = unchecked((int)0x80008096);

        // To share a mutex between processes the name should have the Global prefix
        private const string GlobalMutexPrefix = "Global\\";

        /// <summary>
        /// Determines if the compiler server is supported in this environment.
        /// </summary>
        internal static bool IsCompilerServerSupported => GetPipeName("") is object;

        /// <summary>
        /// Create a build request for processing on the server. 
        /// </summary>
        internal static BuildRequest CreateBuildRequest(
            string requestId,
            RequestLanguage language,
            List<string> arguments,
            string workingDirectory,
            string? tempDirectory,
            string? keepAlive,
            string? libDirectory,
            string? compilerHash = null)
        {
            Debug.Assert(workingDirectory is object);

            return BuildRequest.Create(
                language,
                arguments,
                workingDirectory: workingDirectory,
                tempDirectory: tempDirectory,
                compilerHash: compilerHash ?? BuildProtocolConstants.GetCommitHash() ?? "",
                requestId: requestId,
                keepAlive: keepAlive,
                libDirectory: libDirectory);
        }

        /// <summary>
        /// Shutting down the server is an inherently racy operation.  The server can be started or stopped by
        /// external parties at any time.
        /// 
        /// This function will return success if at any time in the function the server is determined to no longer
        /// be running.
        /// </summary>
        internal static async Task<bool> RunServerShutdownRequestAsync(
            string pipeName,
            int? timeoutOverride,
            bool waitForProcess,
            ICompilerServerLogger logger,
            CancellationToken cancellationToken)
        {
            if (wasServerRunning(pipeName) == false)
            {
                // The server holds the mutex whenever it is running, if it's not open then the 
                // server simply isn't running.
                return true;
            }

            try
            {
                var request = BuildRequest.CreateShutdown();

                // Don't create the server when sending a shutdown request. That would defeat the 
                // purpose a bit.
                var response = await RunServerBuildRequestAsync(
                    request,
                    pipeName,
                    timeoutOverride,
                    tryCreateServerFunc: (_, _) => false,
                    logger,
                    cancellationToken).ConfigureAwait(false);

                if (response is ShutdownBuildResponse shutdownBuildResponse)
                {
                    if (waitForProcess)
                    {
                        try
                        {
                            var process = Process.GetProcessById(shutdownBuildResponse.ServerProcessId);
#if NET5_0_OR_GREATER
                            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
                            process.WaitForExit();
#endif
                        }
                        catch (Exception)
                        {
                            // There is an inherent race here with the server process.  If it has already shutdown
                            // by the time we try to access it then the operation has succeed.
                        }
                    }

                    return true;
                }

                return wasServerRunning(pipeName) == false;
            }
            catch (Exception)
            {
                // If the server was in the process of shutting down when we connected then it's reasonable
                // for an exception to happen.  If the mutex has shutdown at this point then the server 
                // is shut down.
                return wasServerRunning(pipeName) == false;
            }

            // Was a server running with the specified session key during the execution of this call?
            static bool? wasServerRunning(string pipeName)
            {
                string mutexName = GetServerMutexName(pipeName);
                return WasServerMutexOpen(mutexName);
            }
        }

        internal static Task<BuildResponse> RunServerBuildRequestAsync(
            BuildRequest buildRequest,
            string pipeName,
            string clientDirectory,
            ICompilerServerLogger logger,
            CancellationToken cancellationToken)
                => RunServerBuildRequestAsync(
                    buildRequest,
                    pipeName,
                    timeoutOverride: null,
                    tryCreateServerFunc: (pipeName, logger) => TryCreateServer(clientDirectory, pipeName, logger, out int _),
                    logger,
                    cancellationToken);

        internal static async Task<BuildResponse> RunServerBuildRequestAsync(
            BuildRequest buildRequest,
            string pipeName,
            int? timeoutOverride,
            Func<string, ICompilerServerLogger, bool> tryCreateServerFunc,
            ICompilerServerLogger logger,
            CancellationToken cancellationToken)
        {
            Debug.Assert(pipeName is object);

            using var pipe = await tryConnectToServerAsync(pipeName, timeoutOverride, logger, tryCreateServerFunc, cancellationToken).ConfigureAwait(false);
            if (pipe is null)
            {
                return new CannotConnectResponse();
            }
            else
            {
                return await tryRunRequestAsync(pipe, buildRequest, logger, cancellationToken).ConfigureAwait(false);
            }

            // This code uses a Mutex.WaitOne / ReleaseMutex pairing. Both of these calls must occur on the same thread 
            // or an exception will be thrown. This code lives in a separate non-async function to help ensure this 
            // invariant doesn't get invalidated in the future by an `await` being inserted. 
            static Task<NamedPipeClientStream?> tryConnectToServerAsync(
                string pipeName,
                int? timeoutOverride,
                ICompilerServerLogger logger,
                Func<string, ICompilerServerLogger, bool> tryCreateServerFunc,
                CancellationToken cancellationToken)
            {
                var originalThreadId = Environment.CurrentManagedThreadId;
                var timeoutNewProcess = timeoutOverride ?? TimeOutMsNewProcess;
                var timeoutExistingProcess = timeoutOverride ?? TimeOutMsExistingProcess;
                IServerMutex? clientMutex = null;
                try
                {
                    var holdsMutex = false;
                    try
                    {
                        var clientMutexName = GetClientMutexName(pipeName);
                        clientMutex = OpenOrCreateMutex(clientMutexName, out holdsMutex);
                    }
                    catch
                    {
                        // The Mutex constructor can throw in certain cases. One specific example is docker containers
                        // where the /tmp directory is restricted. In those cases there is no reliable way to execute
                        // the server and we need to fall back to the command line.
                        //
                        // Example: https://github.com/dotnet/roslyn/issues/24124
                        return Task.FromResult<NamedPipeClientStream?>(null);
                    }

                    if (!holdsMutex)
                    {
                        try
                        {
                            holdsMutex = clientMutex.TryLock(timeoutNewProcess);

                            if (!holdsMutex)
                            {
                                return Task.FromResult<NamedPipeClientStream?>(null);
                            }
                        }
                        catch (AbandonedMutexException)
                        {
                            holdsMutex = true;
                        }
                    }

                    // Check for an already running server
                    var serverMutexName = GetServerMutexName(pipeName);
                    bool wasServerRunning = WasServerMutexOpen(serverMutexName);
                    var timeout = wasServerRunning ? timeoutExistingProcess : timeoutNewProcess;

                    if (wasServerRunning || tryCreateServerFunc(pipeName, logger))
                    {
                        return TryConnectToServerAsync(pipeName, timeout, logger, cancellationToken);
                    }
                    else
                    {
                        return Task.FromResult<NamedPipeClientStream?>(null);
                    }
                }
                finally
                {
                    try
                    {
                        clientMutex?.Dispose();
                    }
                    catch (ApplicationException e)
                    {
                        var releaseThreadId = Environment.CurrentManagedThreadId;
                        var message = $"ReleaseMutex failed. WaitOne Id: {originalThreadId} Release Id: {releaseThreadId}";
                        throw new Exception(message, e);
                    }
                }
            }

            // Try and run the given BuildRequest on the server. If the request cannot be run then 
            // an appropriate error response will be returned.
            static async Task<BuildResponse> tryRunRequestAsync(
                NamedPipeClientStream pipeStream,
                BuildRequest request,
                ICompilerServerLogger logger,
                CancellationToken cancellationToken)
            {
                try
                {
                    logger.Log($"Begin writing request for {request.RequestId}");
                    await request.WriteAsync(pipeStream, cancellationToken).ConfigureAwait(false);
                    logger.Log($"End writing request for {request.RequestId}");
                }
                catch (Exception e)
                {
                    logger.LogException(e, $"Error writing build request for {request.RequestId}");
                    return new RejectedBuildResponse($"Error writing build request: {e.Message}");
                }

                // Wait for the compilation and a monitor to detect if the server disconnects
                var serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                logger.Log($"Begin reading response for {request.RequestId}");

                var responseTask = BuildResponse.ReadAsync(pipeStream, serverCts.Token);
                var monitorTask = MonitorDisconnectAsync(pipeStream, request.RequestId, logger, serverCts.Token);
                await Task.WhenAny(responseTask, monitorTask).ConfigureAwait(false);

                logger.Log($"End reading response for {request.RequestId}");

                BuildResponse response;
                if (responseTask.IsCompleted)
                {
                    // await the task to log any exceptions
                    try
                    {
                        response = await responseTask.ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e, $"Reading response for {request.RequestId}");
                        response = new RejectedBuildResponse($"Error reading response: {e.Message}");
                    }
                }
                else
                {
                    logger.LogError($"Client disconnect for {request.RequestId}");
                    response = new RejectedBuildResponse($"Client disconnected");
                }

                // Cancel whatever task is still around
                serverCts.Cancel();
                RoslynDebug.Assert(response != null);
                return response;
            }
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection.
        /// </summary>
        internal static async Task MonitorDisconnectAsync(
            PipeStream pipeStream,
            string requestId,
            ICompilerServerLogger logger,
            CancellationToken cancellationToken = default)
        {
            var buffer = Array.Empty<byte>();

            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                try
                {
                    // Wait a tenth of a second before trying again
                    await Task.Delay(millisecondsDelay: 100, cancellationToken).ConfigureAwait(false);

                    await pipeStream.ReadAsync(buffer, 0, 0, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    // It is okay for this call to fail.  Errors will be reflected in the
                    // IsConnected property which will be read on the next iteration of the
                    logger.LogException(e, $"Error poking pipe {requestId}.");
                }
            }
        }

        /// <summary>
        /// Attempt to connect to the server and return a null <see cref="NamedPipeClientStream"/> if connection 
        /// failed. This method will throw on cancellation.
        /// </summary>
        internal static async Task<NamedPipeClientStream?> TryConnectToServerAsync(
            string pipeName,
            int timeoutMs,
            ICompilerServerLogger logger,
            CancellationToken cancellationToken)
        {
            NamedPipeClientStream? pipeStream = null;
            try
            {
                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the SHA1 of the directory the compiler exes live in as the pipe name.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                logger.Log("Attempt to open named pipe '{0}'", pipeName);

                pipeStream = NamedPipeUtil.CreateClient(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                cancellationToken.ThrowIfCancellationRequested();

                logger.Log("Attempt to connect named pipe '{0}'", pipeName);
                try
                {
                    // NamedPipeClientStream.ConnectAsync on the "full" framework has a bug where it
                    // tries to move potentially expensive work (actually connecting to the pipe) to
                    // a background thread with Task.Factory.StartNew. However, that call will merely
                    // queue the work onto the TaskScheduler associated with the "current" Task which
                    // does not guarantee it will be processed on a background thread and this could
                    // lead to a hang.
                    // To avoid this, we first force ourselves to a background thread using Task.Run.
                    // This ensures that the Task created by ConnectAsync will run on the default
                    // TaskScheduler (i.e., on a threadpool thread) which was the intent all along.
                    await Task.Run(() => pipeStream.ConnectAsync(timeoutMs, cancellationToken), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is IOException || e is TimeoutException)
                {
                    // Note: IOException can also indicate timeout. From docs:
                    // TimeoutException: Could not connect to the server within the
                    //                   specified timeout period.
                    // IOException: The server is connected to another client and the
                    //              time-out period has expired.

                    logger.LogException(e, $"Connecting to server timed out after {timeoutMs} ms");
                    pipeStream.Dispose();
                    return null;
                }
                logger.Log("Named pipe '{0}' connected", pipeName);

                cancellationToken.ThrowIfCancellationRequested();

                // Verify that we own the pipe.
                if (!NamedPipeUtil.CheckPipeConnectionOwnership(pipeStream))
                {
                    pipeStream.Dispose();
                    logger.LogError("Owner of named pipe is incorrect");
                    return null;
                }

                return pipeStream;
            }
            catch (Exception e) when (!(e is TaskCanceledException || e is OperationCanceledException))
            {
                logger.LogException(e, "Exception while connecting to process");
                pipeStream?.Dispose();
                return null;
            }
        }

        internal static (string processFilePath, string commandLineArguments) GetServerProcessInfo(string clientDir, string pipeName)
        {
            var processFilePath = Path.Combine(clientDir, $"VBCSCompiler{PlatformInformation.ExeExtension}");
            var commandLineArgs = $@"""-pipename:{pipeName}""";

            if (!File.Exists(processFilePath))
            {
                // Fallback to not use the apphost if it is not present (can happen in compiler toolset scenarios for example).
                commandLineArgs = RuntimeHostInfo.GetDotNetExecCommandLine(Path.ChangeExtension(processFilePath, ".dll"), commandLineArgs);
                processFilePath = RuntimeHostInfo.GetDotNetPathOrDefault();
            }

            return (processFilePath, commandLineArgs);
        }

        /// <summary>
        /// Creates an environment block for Windows CreateProcess API.
        /// </summary>
        /// <param name="environmentVariables">Dictionary of environment variables to include</param>
        /// <returns>Pointer to environment block that must be freed with <see cref="Marshal.FreeHGlobal"/></returns>
        private static IntPtr CreateEnvironmentBlock(Dictionary<string, string> environmentVariables)
        {
            if (environmentVariables.Count == 0)
            {
                return IntPtr.Zero;
            }

            // Build the environment block as a single string
            // Windows API requires environment variables to be sorted alphabetically by name (case-insensitive, Unicode order)
            var envBlock = new StringBuilder();
            foreach (var kvp in environmentVariables.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                envBlock.Append(kvp.Key);
                envBlock.Append('=');
                envBlock.Append(kvp.Value);
                envBlock.Append('\0');
            }
            // Windows environment block format requires an additional null terminator after the last variable to mark the end of the block
            envBlock.Append('\0');

            // Convert to Unicode and allocate unmanaged memory
            return Marshal.StringToHGlobalUni(envBlock.ToString());
        }

        /// <summary>
        /// Gets the environment variables that should be passed to the server process.
        /// </summary>
        /// <param name="currentEnvironment">Current environment variables to use as a base</param>
        /// <returns>Dictionary of environment variables to set, or null if no custom environment is needed</returns>
        /// <remarks>
        /// This method is <see langword="internal"/> for testing purposes only.
        /// </remarks>
        internal static Dictionary<string, string>? GetServerEnvironmentVariables(System.Collections.IDictionary currentEnvironment)
        {
            if (RuntimeHostInfo.GetToolDotNetRoot() is not { } dotNetRoot)
            {
                return null;
            }

            // Start with current environment
            var environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in currentEnvironment)
            {
                var key = (string)entry.Key;
                var value = (string?)entry.Value;

                // Clear DOTNET_ROOT* variables such as DOTNET_ROOT_X64 by setting them to empty,
                // as we want to set our own DOTNET_ROOT and avoid conflicts
                if (key.StartsWith(RuntimeHostInfo.DotNetRootEnvironmentName, StringComparison.OrdinalIgnoreCase))
                {
                    environmentVariables[key] = string.Empty;
                }
                else
                {
                    environmentVariables[key] = value ?? string.Empty;
                }
            }

            // Set our DOTNET_ROOT
            environmentVariables[RuntimeHostInfo.DotNetRootEnvironmentName] = dotNetRoot;

            return environmentVariables;
        }

        /// <summary>
        /// This will attempt to start a compiler server process using the executable inside the 
        /// directory <paramref name="clientDirectory"/>. This returns "true" if starting the 
        /// compiler server process was successful and the process did not immediately exit with
        /// a framework missing error (0x80008096).
        /// </summary>
        internal static bool TryCreateServer(string clientDirectory, string pipeName, ICompilerServerLogger logger, out int processId)
        {
            processId = 0;
            var serverInfo = GetServerProcessInfo(clientDirectory, pipeName);

            if (!File.Exists(serverInfo.processFilePath))
            {
                return false;
            }

            var environmentVariables = GetServerEnvironmentVariables(Environment.GetEnvironmentVariables());

            if (environmentVariables != null)
            {
                logger.Log("Attempting to create process '{0}' {1} with {2}='{3}'",
                    serverInfo.processFilePath,
                    serverInfo.commandLineArguments,
                    RuntimeHostInfo.DotNetRootEnvironmentName,
                    environmentVariables[RuntimeHostInfo.DotNetRootEnvironmentName]);
            }
            else
            {
                logger.Log("Attempting to create process '{0}' {1}", serverInfo.processFilePath, serverInfo.commandLineArguments);
            }

            if (PlatformInformation.IsWindows)
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

                var builder = new StringBuilder($@"""{serverInfo.processFilePath}"" {serverInfo.commandLineArguments}");

                IntPtr environmentBlockPtr = IntPtr.Zero;
                try
                {
                    if (environmentVariables != null)
                    {
                        environmentBlockPtr = CreateEnvironmentBlock(environmentVariables);
                        // When passing a Unicode environment block, we must set the CREATE_UNICODE_ENVIRONMENT flag
                        dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
                    }

                    bool success = CreateProcess(
                        lpApplicationName: null,
                        lpCommandLine: builder,
                        lpProcessAttributes: NullPtr,
                        lpThreadAttributes: NullPtr,
                        bInheritHandles: false,
                        dwCreationFlags: dwCreationFlags,
                        lpEnvironment: environmentBlockPtr,
                        lpCurrentDirectory: clientDirectory,
                        lpStartupInfo: ref startInfo,
                        lpProcessInformation: out processInfo);

                    if (success)
                    {
                        logger.Log("Successfully created process with process id {0}", processInfo.dwProcessId);
                        processId = processInfo.dwProcessId;

                        // Wait briefly to see if the process exits immediately with FrameworkMissingFailure
                        // This indicates the .NET runtime on PATH doesn't support the compiler
                        var processHandle = processInfo.hProcess;
                        var waitResult = WaitForSingleObject(processHandle, ProcessCheckTimeoutMs);
                        
                        if (waitResult == WAIT_OBJECT_0)
                        {
                            // Process exited quickly, check the exit code
                            if (GetExitCodeProcess(processHandle, out uint exitCode) && exitCode == unchecked((uint)FrameworkMissingFailure))
                            {
                                logger.LogWarning($"VBCS2023: Failed to start compiler server. The .NET runtime on PATH does not support this compiler. Ensure DOTNET_HOST_PATH is set correctly or update the .NET runtime on PATH. See https://aka.ms/dotnet-host-path for more information.");
                                success = false;
                            }
                        }

                        CloseHandle(processInfo.hProcess);
                        CloseHandle(processInfo.hThread);
                    }
                    else
                    {
                        logger.LogError("Failed to create process. GetLastError={0}", Marshal.GetLastWin32Error());
                    }
                    return success;
                }
                finally
                {
                    if (environmentBlockPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(environmentBlockPtr);
                    }
                }
            }
            else
            {
                try
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = serverInfo.processFilePath,
                        Arguments = serverInfo.commandLineArguments,
                        UseShellExecute = false,
                        WorkingDirectory = clientDirectory,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    // Set environment variables directly on ProcessStartInfo
                    if (environmentVariables != null)
                    {
                        foreach (var kvp in environmentVariables)
                        {
                            startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                        }
                    }

                    if (Process.Start(startInfo) is { } process)
                    {
                        processId = process.Id;
                        logger.Log("Successfully created process with process id {0}", processId);

                        // Wait briefly to see if the process exits immediately with FrameworkMissingFailure
                        // This indicates the .NET runtime on PATH doesn't support the compiler
                        if (process.WaitForExit(ProcessCheckTimeoutMs))
                        {
                            // Process exited quickly, check the exit code
                            if (process.ExitCode == FrameworkMissingFailure)
                            {
                                logger.LogWarning($"VBCS2023: Failed to start compiler server. The .NET runtime on PATH does not support this compiler. Ensure DOTNET_HOST_PATH is set correctly or update the .NET runtime on PATH. See https://aka.ms/dotnet-host-path for more information.");
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <returns>
        /// Null if not enough information was found to create a valid pipe name.
        /// </returns>
        internal static string GetPipeName(string clientDirectory)
        {
            // Prefix with username and elevation
            bool isAdmin = false;
            if (PlatformInformation.IsWindows)
            {
                var currentIdentity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentIdentity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            var userName = Environment.UserName;
            return GetPipeName(userName, isAdmin, clientDirectory);
        }

        internal static string GetPipeName(
            string userName,
            bool isAdmin,
            string clientDirectory)
        {
            // Normalize away trailing slashes.  File APIs include / exclude this with no 
            // discernable pattern.  Easiest to normalize it here vs. auditing every caller
            // of this method.
            clientDirectory = clientDirectory.TrimEnd(Path.DirectorySeparatorChar);

            // Similarly, we don't want multiple servers if the provided launch path differs in casing.
            clientDirectory = clientDirectory.ToLowerInvariant();

            var pipeNameInput = $"{userName}.{isAdmin}.{clientDirectory}";
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pipeNameInput));
                return Convert.ToBase64String(bytes)
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }
        }

        internal static bool WasServerMutexOpen(string mutexName)
        {
            try
            {
                if (PlatformInformation.IsUsingMonoRuntime)
                {
                    using var mutex = new ServerFileMutex(mutexName);
                    return !mutex.CouldLock();
                }
                else
                {
                    return ServerNamedMutex.WasOpen(mutexName);
                }
            }
            catch
            {
                // In the case an exception occurred trying to open the Mutex then 
                // the assumption is that it's not open. 
                return false;
            }
        }

        internal static IServerMutex OpenOrCreateMutex(string name, out bool createdNew)
        {
            if (PlatformInformation.IsUsingMonoRuntime)
            {
                var mutex = new ServerFileMutex(name);
                createdNew = mutex.TryLock(0);
                return mutex;
            }
            else
            {
                return new ServerNamedMutex(name, out createdNew);
            }
        }

        internal static string GetServerMutexName(string pipeName)
        {
            return $"{GlobalMutexPrefix}{pipeName}.server";
        }

        internal static string GetClientMutexName(string pipeName)
        {
            return $"{GlobalMutexPrefix}{pipeName}.client";
        }
    }

    internal interface IServerMutex : IDisposable
    {
        bool TryLock(int timeoutMs);
        bool IsDisposed { get; }
    }

    /// <summary>
    /// An interprocess mutex abstraction based on file sharing permission (FileShare.None).
    /// If multiple processes running as the same user create FileMutex instances with the same name,
    ///  those instances will all point to the same file somewhere in a selected temporary directory.
    /// The TryLock method can be used to attempt to acquire the mutex, with Dispose used to release.
    /// The CouldLock method can be used to check whether an attempt to acquire the mutex would have
    ///  succeeded at the current time, without actually acquiring it.
    /// Unlike Win32 named mutexes, there is no mechanism for detecting an abandoned mutex. The file
    ///  will simply revert to being unlocked but remain where it is.
    /// </summary>
    internal sealed class ServerFileMutex : IServerMutex
    {
        public FileStream? Stream;
        public readonly string FilePath;
        public readonly string GuardPath;

        public bool IsDisposed { get; private set; }

        internal static string GetMutexDirectory()
        {
            var tempPath = Path.GetTempPath();
            var result = Path.Combine(tempPath!, ".roslyn");
            Directory.CreateDirectory(result);
            return result;
        }

        public ServerFileMutex(string name)
        {
            var mutexDirectory = GetMutexDirectory();
            FilePath = Path.Combine(mutexDirectory, name);
            GuardPath = Path.Combine(mutexDirectory, ".guard");
        }

        /// <summary>
        /// Acquire the guard by opening the guard file with FileShare.None.  The guard must only ever
        /// be held for very brief amounts of time, so we can simply spin until it is acquired.  The
        /// guard must be released by disposing the FileStream returned from this routine.  Note the
        /// guard file is never deleted; this is a leak, but only of a single file.
        /// </summary>
        internal FileStream LockGuard()
        {
            // We should be able to acquire the guard quickly.  Limit the number of retries anyway
            // by some arbitrary bound to avoid getting hung up in a possibly infinite loop.
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    return new FileStream(GuardPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    // Guard currently held by someone else.
                    // We want to sleep for a short period of time to ensure that other processes
                    //  have an opportunity to finish their work and relinquish the lock.
                    // Spinning here (via Yield) would work but risks creating a priority
                    //  inversion if the lock is held by a lower-priority process.
                    Thread.Sleep(1);
                }
            }
            // Handle unexpected failure to acquire guard as error.
            throw new InvalidOperationException("Unable to acquire guard");
        }

        /// <summary>
        /// Attempt to acquire the lock by opening the lock file with FileShare.None.  Sets "Stream"
        /// and returns true if successful, returns false if the lock is already held by another
        /// thread or process.  Guard must be held when calling this routine.
        /// </summary>
        internal bool TryLockFile()
        {
            Debug.Assert(Stream is null);
            FileStream? stream = null;
            try
            {
                stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                // On some targets, the file locking used to implement FileShare.None may not be
                // atomic with opening/creating the file.   This creates a race window when another
                // thread holds the lock and is just about to unlock: we may be able to open the
                // file here, then the other thread unlocks and deletes the file, and then we
                // acquire the lock on our file handle - but the actual file is already deleted.
                // To close this race, we verify that the file does in fact still exist now that
                // we have successfully acquired the locked FileStream.   (Note that this check is
                // safe because we cannot race with an other attempt to create the file since we
                // hold the guard, and after the FileStream constructor returned we can no race
                // with file deletion because we hold the lock.)
                if (!File.Exists(FilePath))
                {
                    // To simplify the logic, we treat this case as "unable to acquire the lock"
                    // because it we caught another process while it owned the lock and was just
                    // giving it up.  If the caller retries, we'll likely acquire the lock then.
                    stream.Dispose();
                    return false;
                }
            }
            catch (Exception)
            {
                stream?.Dispose();
                return false;
            }
            Stream = stream;
            return true;
        }

        /// <summary>
        /// Release the lock by deleting the lock file and disposing "Stream".
        /// </summary>
        internal void UnlockFile()
        {
            Debug.Assert(Stream is not null);
            try
            {
                // Delete the lock file while the stream is not yet disposed
                // and we therefore still hold the FileShare.None exclusion.
                // There may still be a race with another thread attempting a
                // TryLockFile in parallel, but that is safely handled there.
                File.Delete(FilePath);
            }
            finally
            {
                Stream.Dispose();
                Stream = null;
            }
        }

        public bool TryLock(int timeoutMs)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Mutex");
            if (Stream is not null)
                throw new InvalidOperationException("Lock already held");

            var sw = Stopwatch.StartNew();
            do
            {
                try
                {
                    // Attempt to acquire lock while holding guard.
                    using var guard = LockGuard();
                    if (TryLockFile())
                        return true;
                }
                catch (Exception)
                {
                    return false;
                }

                // See comment in LockGuard.
                Thread.Sleep(1);
            } while (sw.ElapsedMilliseconds < timeoutMs);

            return false;
        }

        public bool CouldLock()
        {
            if (IsDisposed)
                return false;
            if (Stream is not null)
                return false;

            try
            {
                // Attempt to acquire lock while holding guard, and if successful
                // immediately unlock again while still holding guard.  This ensures
                // no other thread will spuriously observe the lock as held due to
                // the lock attempt here.
                using var guard = LockGuard();
                if (TryLockFile())
                {
                    UnlockFile();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            if (Stream is not null)
            {
                try
                {
                    UnlockFile();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    internal sealed class ServerNamedMutex : IServerMutex
    {
        public readonly Mutex Mutex;

        public bool IsDisposed { get; private set; }
        public bool IsLocked { get; private set; }

        public ServerNamedMutex(string mutexName, out bool createdNew)
        {
            Mutex = new Mutex(
                initiallyOwned: true,
                name: mutexName,
                createdNew: out createdNew
            );
            if (createdNew)
                IsLocked = true;
        }

        public static bool WasOpen(string mutexName)
        {
            Mutex? m = null;
            try
            {
                return Mutex.TryOpenExisting(mutexName, out m);
            }
            catch
            {
                // In the case an exception occurred trying to open the Mutex then 
                // the assumption is that it's not open.
                return false;
            }
            finally
            {
                m?.Dispose();
            }
        }

        public bool TryLock(int timeoutMs)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Mutex");
            if (IsLocked)
                throw new InvalidOperationException("Lock already held");
            return IsLocked = Mutex.WaitOne(timeoutMs);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            try
            {
                if (IsLocked)
                    Mutex.ReleaseMutex();
            }
            finally
            {
                Mutex.Dispose();
                IsLocked = false;
            }
        }
    }
}
