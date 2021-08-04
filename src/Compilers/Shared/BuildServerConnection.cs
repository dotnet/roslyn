﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        internal const int TimeOutMsExistingProcess = 1000;

        // Spend up to 20s connecting to a new process, to allow time for it to start.
        internal const int TimeOutMsNewProcess = 20000;

        /// <summary>
        /// Determines if the compiler server is supported in this environment.
        /// </summary>
        internal static bool IsCompilerServerSupported => GetPipeName("") is object;

        internal static BuildRequest CreateBuildRequest(
            Guid requestId,
            RequestLanguage language,
            List<string> arguments,
            string workingDirectory,
            string tempDirectory,
            string? keepAlive,
            string? libDirectory)
        {
            Debug.Assert(workingDirectory is object);
            Debug.Assert(tempDirectory is object);

            return BuildRequest.Create(
                language,
                arguments,
                workingDirectory: workingDirectory,
                tempDirectory: tempDirectory,
                compilerHash: BuildProtocolConstants.GetCommitHash() ?? "",
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
#if NET50_OR_GREATER
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
                    tryCreateServerFunc: (pipeName, logger) => TryCreateServer(clientDirectory, pipeName, logger),
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

            // early check for the build hash. If we can't find it something is wrong; no point even trying to go to the server
            if (string.IsNullOrWhiteSpace(BuildProtocolConstants.GetCommitHash()))
            {
                return new IncorrectHashBuildResponse();
            }

            using var pipe = await tryConnectToServer(pipeName, timeoutOverride, logger, tryCreateServerFunc, cancellationToken).ConfigureAwait(false);
            if (pipe is null)
            {
                return new RejectedBuildResponse("Failed to connect to server");
            }
            else
            {
                return await tryRunRequestAsync(pipe, buildRequest, logger, cancellationToken).ConfigureAwait(false);
            }

            // This code uses a Mutex.WaitOne / ReleaseMutex pairing. Both of these calls must occur on the same thread 
            // or an exception will be thrown. This code lives in a separate non-async function to help ensure this 
            // invariant doesn't get invalidated in the future by an `await` being inserted. 
            static Task<NamedPipeClientStream?> tryConnectToServer(
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
            Guid requestId,
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

        internal static (string processFilePath, string commandLineArguments, string toolFilePath) GetServerProcessInfo(string clientDir, string pipeName)
        {
            var serverPathWithoutExtension = Path.Combine(clientDir, "VBCSCompiler");
            var commandLineArgs = $@"""-pipename:{pipeName}""";
            return RuntimeHostInfo.GetProcessInfo(serverPathWithoutExtension, commandLineArgs);
        }

        /// <summary>
        /// This will attempt to start a compiler server process using the executable inside the 
        /// directory <paramref name="clientDirectory"/>. This returns "true" if starting the 
        /// compiler server process was successful, it does not state whether the server successfully
        /// started or not (it could crash on startup).
        /// </summary>
        private static bool TryCreateServer(string clientDirectory, string pipeName, ICompilerServerLogger logger)
        {
            var serverInfo = GetServerProcessInfo(clientDirectory, pipeName);

            if (!File.Exists(serverInfo.toolFilePath))
            {
                return false;
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

                logger.Log("Attempting to create process '{0}'", serverInfo.processFilePath);

                var builder = new StringBuilder($@"""{serverInfo.processFilePath}"" {serverInfo.commandLineArguments}");

                bool success = CreateProcess(
                    lpApplicationName: null,
                    lpCommandLine: builder,
                    lpProcessAttributes: NullPtr,
                    lpThreadAttributes: NullPtr,
                    bInheritHandles: false,
                    dwCreationFlags: dwCreationFlags,
                    lpEnvironment: NullPtr, // Inherit environment
                    lpCurrentDirectory: clientDirectory,
                    lpStartupInfo: ref startInfo,
                    lpProcessInformation: out processInfo);

                if (success)
                {
                    logger.Log("Successfully created process with process id {0}", processInfo.dwProcessId);
                    CloseHandle(processInfo.hProcess);
                    CloseHandle(processInfo.hThread);
                }
                else
                {
                    logger.LogError("Failed to create process. GetLastError={0}", Marshal.GetLastWin32Error());
                }
                return success;
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

                    Process.Start(startInfo);
                    return true;
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
                if (PlatformInformation.IsRunningOnMono)
                {
                    IServerMutex? mutex = null;
                    bool createdNew = false;
                    try
                    {
                        mutex = new ServerFileMutexPair(mutexName, false, out createdNew);
                        return !createdNew;
                    }
                    finally
                    {
                        mutex?.Dispose();
                    }
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
            if (PlatformInformation.IsRunningOnMono)
            {
                return new ServerFileMutexPair(name, initiallyOwned: true, out createdNew);
            }
            else
            {
                return new ServerNamedMutex(name, out createdNew);
            }
        }

        internal static string GetServerMutexName(string pipeName)
        {
            return $"{pipeName}.server";
        }

        internal static string GetClientMutexName(string pipeName)
        {
            return $"{pipeName}.client";
        }

        /// <summary>
        /// Gets the value of the temporary path for the current environment assuming the working directory
        /// is <paramref name="workingDir"/>.  This function must emulate <see cref="Path.GetTempPath"/> as 
        /// closely as possible.
        /// </summary>
        internal static string? GetTempPath(string? workingDir)
        {
            if (PlatformInformation.IsUnix)
            {
                // Unix temp path is fine: it does not use the working directory
                // (it uses ${TMPDIR} if set, otherwise, it returns /tmp)
                return Path.GetTempPath();
            }

            var tmp = Environment.GetEnvironmentVariable("TMP");
            if (Path.IsPathRooted(tmp))
            {
                return tmp;
            }

            var temp = Environment.GetEnvironmentVariable("TEMP");
            if (Path.IsPathRooted(temp))
            {
                return temp;
            }

            if (!string.IsNullOrEmpty(workingDir))
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    return Path.Combine(workingDir, tmp);
                }

                if (!string.IsNullOrEmpty(temp))
                {
                    return Path.Combine(workingDir, temp);
                }
            }

            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (Path.IsPathRooted(userProfile))
            {
                return userProfile;
            }

            return Environment.GetEnvironmentVariable("SYSTEMROOT");
        }
    }

    internal interface IServerMutex : IDisposable
    {
        bool TryLock(int timeoutMs);
        bool IsDisposed { get; }
    }

    /// <summary>
    /// An interprocess mutex abstraction based on OS advisory locking (FileStream.Lock/Unlock).
    /// If multiple processes running as the same user create FileMutex instances with the same name,
    ///  those instances will all point to the same file somewhere in a selected temporary directory.
    /// The TryLock method can be used to attempt to acquire the mutex, with Unlock or Dispose used to release.
    /// Unlike Win32 named mutexes, there is no mechanism for detecting an abandoned mutex. The file
    ///  will simply revert to being unlocked but remain where it is.
    /// </summary>
    internal sealed class FileMutex : IDisposable
    {
        public readonly FileStream Stream;
        public readonly string FilePath;

        public bool IsLocked { get; private set; }

        internal static string GetMutexDirectory()
        {
            var tempPath = BuildServerConnection.GetTempPath(null);
            var result = Path.Combine(tempPath!, ".roslyn");
            Directory.CreateDirectory(result);
            return result;
        }

        public FileMutex(string name)
        {
            FilePath = Path.Combine(GetMutexDirectory(), name);
            Stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public bool TryLock(int timeoutMs)
        {
            if (IsLocked)
                throw new InvalidOperationException("Lock already held");

            var sw = Stopwatch.StartNew();
            do
            {
                try
                {
                    Stream.Lock(0, 0);
                    IsLocked = true;
                    return true;
                }
                catch (IOException)
                {
                    // Lock currently held by someone else.
                    // We want to sleep for a short period of time to ensure that other processes
                    //  have an opportunity to finish their work and relinquish the lock.
                    // Spinning here (via Yield) would work but risks creating a priority
                    //  inversion if the lock is held by a lower-priority process.
                    Thread.Sleep(1);
                }
                catch (Exception)
                {
                    // Something else went wrong.
                    return false;
                }
            } while (sw.ElapsedMilliseconds < timeoutMs);

            return false;
        }

        public void Unlock()
        {
            if (!IsLocked)
                return;
            Stream.Unlock(0, 0);
            IsLocked = false;
        }

        public void Dispose()
        {
            var wasLocked = IsLocked;
            if (wasLocked)
                Unlock();
            Stream.Dispose();
            // We do not delete the lock file here because there is no reliable way to perform a
            //  'delete if no one has the file open' operation atomically on *nix. This is a leak.
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

    /// <summary>
    /// Approximates a named mutex with 'locked', 'unlocked' and 'abandoned' states.
    /// There is no reliable way to detect whether a mutex has been abandoned on some target platforms,
    ///  so we use the AliveMutex to manually track whether the creator of a mutex is still running,
    ///  while the HeldMutex represents the actual lock state of the mutex.
    /// </summary>
    internal sealed class ServerFileMutexPair : IServerMutex
    {
        public readonly FileMutex AliveMutex;
        public readonly FileMutex HeldMutex;

        public bool IsDisposed { get; private set; }

        public ServerFileMutexPair(string mutexName, bool initiallyOwned, out bool createdNew)
        {
            AliveMutex = new FileMutex(mutexName + "-alive");
            HeldMutex = new FileMutex(mutexName + "-held");
            createdNew = AliveMutex.TryLock(0);
            if (initiallyOwned && createdNew)
            {
                if (!TryLock(0))
                    throw new Exception("Failed to lock mutex after creating it");
            }
        }

        public bool TryLock(int timeoutMs)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Mutex");
            return HeldMutex.TryLock(timeoutMs);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            try
            {
                HeldMutex.Unlock();
                AliveMutex.Unlock();
            }
            finally
            {
                AliveMutex.Dispose();
                HeldMutex.Dispose();
            }
        }
    }

}
