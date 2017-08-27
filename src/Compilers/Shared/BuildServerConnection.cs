﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CommandLine.CompilerServerLogger;
using static Microsoft.CodeAnalysis.CommandLine.NativeMethods;

namespace Microsoft.CodeAnalysis.CommandLine
{
    internal struct BuildPathsAlt
    {
        /// <summary>
        /// The path which containts the compiler binaries and response files.
        /// </summary>
        internal string ClientDirectory { get; }

        /// <summary>
        /// The path in which the compilation takes place.
        /// </summary>
        internal string WorkingDirectory { get; }

        /// <summary>
        /// The path which contains mscorlib.  This can be null when specified by the user or running in a 
        /// CoreClr environment.
        /// </summary>
        internal string SdkDirectory { get; }

        /// <summary>
        /// The temporary directory a compilation should use instead of <see cref="Path.GetTempPath"/>.  The latter
        /// relies on global state individual compilations should ignore.
        /// </summary>
        internal string TempDirectory { get; }

        internal BuildPathsAlt(string clientDir, string workingDir, string sdkDir, string tempDir)
        {
            ClientDirectory = clientDir;
            WorkingDirectory = workingDir;
            SdkDirectory = sdkDir;
            TempDirectory = tempDir;
        }
    }

    internal sealed class BuildServerConnection
    {
        internal const string ServerName = "VBCSCompiler.exe";

        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        internal const int TimeOutMsExistingProcess = 1000;

        // Spend up to 20s connecting to a new process, to allow time for it to start.
        internal const int TimeOutMsNewProcess = 20000;

        /// <summary>
        /// Determines if the compiler server is supported in this environment.
        /// </summary>
        internal static bool IsCompilerServerSupported => 
            PlatformInformation.IsWindows &&
            GetRuntimeDirectoryOpt() != null &&
            GetPipeNameForPathOpt("") != null;

        internal static string GetRuntimeDirectoryOpt()
        {
            Type runtimeEnvironmentType;
            try
            {
                runtimeEnvironmentType = Type.GetType("System.Runtime.InteropServices.RuntimeEnvironment", throwOnError: false);
            }
            catch
            {
                return null;
            }

            return (string)runtimeEnvironmentType
                ?.GetTypeInfo()
                .GetDeclaredMethod("GetRuntimeDirectory")
                ?.Invoke(obj: null, parameters: null);
        }


        public static Task<BuildResponse> RunServerCompilation(
            RequestLanguage language,
            List<string> arguments,
            BuildPathsAlt buildPaths,
            string keepAlive,
            string libEnvVariable,
            CancellationToken cancellationToken)
        {
            var pipeNameOpt = GetPipeNameForPathOpt(buildPaths.ClientDirectory);

            return RunServerCompilationCore(
                language,
                arguments,
                buildPaths,
                pipeNameOpt,
                keepAlive,
                libEnvVariable,
                timeoutOverride: null,
                tryCreateServerFunc: TryCreateServerCore,
                cancellationToken: cancellationToken);
        }

        internal static Task<BuildResponse> RunServerCompilationCore(
            RequestLanguage language,
            List<string> arguments,
            BuildPathsAlt buildPaths,
            string pipeName,
            string keepAlive,
            string libEnvVariable,
            int? timeoutOverride,
            Func<string, string, bool> tryCreateServerFunc,
            CancellationToken cancellationToken)
        {
            if (pipeName == null)
            {
                return Task.FromResult<BuildResponse>(new RejectedBuildResponse());
            }

            if (buildPaths.TempDirectory == null)
            {
                return Task.FromResult<BuildResponse>(new RejectedBuildResponse());
            }

            var clientDir = buildPaths.ClientDirectory;
            var timeoutNewProcess = timeoutOverride ?? TimeOutMsNewProcess;
            var timeoutExistingProcess = timeoutOverride ?? TimeOutMsExistingProcess;
            var clientMutexName = GetClientMutexName(pipeName);
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
                            holdsMutex = clientMutex.WaitOne(timeoutNewProcess);

                            if (!holdsMutex)
                            {
                                return Task.FromResult<BuildResponse>(new RejectedBuildResponse());
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
                                                          buildPaths.TempDirectory,
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

            return Task.FromResult<BuildResponse>(new RejectedBuildResponse());
        }

        /// <summary>
        /// Try to compile using the server. Returns a null-containing Task if a response
        /// from the server cannot be retrieved.
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
                    return new RejectedBuildResponse();
                }

                // Wait for the compilation and a monitor to detect if the server disconnects
                var serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Log("Begin reading response");

                var responseTask = BuildResponse.ReadAsync(pipeStream, serverCts.Token);
                var monitorTask = CreateMonitorDisconnectTask(pipeStream, "client", serverCts.Token);
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
                        response = new RejectedBuildResponse();
                    }
                }
                else
                {
                    Log("Server disconnect");
                    response = new RejectedBuildResponse();
                }

                // Cancel whatever task is still around
                serverCts.Cancel();
                Debug.Assert(response != null);
                return response;
            }
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection.
        /// </summary>
        internal static async Task CreateMonitorDisconnectTask(
            PipeStream pipeStream,
            string identifier = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var buffer = Array.Empty<byte>();

            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                // Wait a tenth of a second before trying again
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                try
                {
                    Log($"Before poking pipe {identifier}.");
                    await pipeStream.ReadAsync(buffer, 0, 0, cancellationToken).ConfigureAwait(false);
                    Log($"After poking pipe {identifier}.");
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    // It is okay for this call to fail.  Errors will be reflected in the
                    // IsConnected property which will be read on the next iteration of the
                    LogException(e, $"Error poking pipe {identifier}.");
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
                if (!TryConnectToNamedPipeWithSpinWait(pipeStream, timeoutMs, cancellationToken))
                {
                    Log($"Connecting to server timed out after {timeoutMs} ms");
                    return null;
                }
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

        internal static bool TryConnectToNamedPipeWithSpinWait(NamedPipeClientStream pipeStream,
                                                               int timeoutMs,
                                                               CancellationToken cancellationToken)
        {
            Debug.Assert(timeoutMs == Timeout.Infinite || timeoutMs > 0);

            // .NET 4.5 implementation of NamedPipeStream.Connect busy waits the entire time.
            // Work around is to spin wait.
            const int maxWaitIntervalMs = 50;
            int elapsedMs = 0;
            var sw = new SpinWait();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int waitTime;
                if (timeoutMs == Timeout.Infinite)
                {
                    waitTime = maxWaitIntervalMs;
                }
                else
                {
                    waitTime = Math.Min(timeoutMs - elapsedMs, maxWaitIntervalMs);
                    if (waitTime <= 0)
                    {
                        return false;
                    }
                }

                try
                {
                    pipeStream.Connect(waitTime);
                    break;
                }
                catch (Exception e) when (e is IOException || e is TimeoutException)
                {
                    // Ignore timeout

                    // Note: IOException can also indicate timeout. From docs:
                    // TimeoutException: Could not connect to the server within the
                    //                   specified timeout period.
                    // IOException: The server is connected to another client and the
                    //              time-out period has expired.
                }
                unchecked { elapsedMs += waitTime; }
                sw.SpinOnce();
            }
            return true;
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
                var remotePipeSecurity = GetPipeSecurity(pipeStream);
                var remoteOwner = remotePipeSecurity.GetOwner(typeof(SecurityIdentifier));
                return currentOwner.Equals(remoteOwner);
            }
            catch (Exception ex)
            {
                Log("Exception checking pipe connection: {0}", ex.Message);
                return false;
            }
        }

        private static ObjectSecurity GetPipeSecurity(PipeStream pipeStream) =>
            (ObjectSecurity)typeof(PipeStream)
            .GetTypeInfo()
            .GetDeclaredMethod("GetAccessControl")
            ?.Invoke(pipeStream, parameters: null);

        private static string GetUserName() =>
            (string)typeof(Environment)
            .GetTypeInfo()
            .GetDeclaredProperty("UserName")
            ?.GetMethod?.Invoke(null, parameters: null);

        /// <returns>
        /// Null if not enough information was found to create a valid pipe name.
        /// </returns>
        internal static string GetPipeNameForPathOpt(string compilerExeDirectory)
        {
            var basePipeName = GetBasePipeName(compilerExeDirectory);

            // Prefix with username and elevation
            var currentIdentity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(currentIdentity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            var userName = GetUserName();
            if (userName == null)
            {
                return null;
            }

            return $"{userName}.{isAdmin}.{basePipeName}";
        }

        internal static string GetBasePipeName(string compilerExeDirectory)
        {
            // Normalize away trailing slashes.  File APIs include / exclude this with no 
            // discernable pattern.  Easiest to normalize it here vs. auditing every caller
            // of this method.
            compilerExeDirectory = compilerExeDirectory.TrimEnd(Path.DirectorySeparatorChar);

            string basePipeName;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(compilerExeDirectory));
                basePipeName = Convert.ToBase64String(bytes)
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }

            return basePipeName;
        }

        internal static bool WasServerMutexOpen(string mutexName)
        {
            Mutex mutex;
            var open = Mutex.TryOpenExisting(mutexName, out mutex);
            if (open)
            {
                mutex.Dispose();
                return true;
            }

            return false;
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
        public static string GetTempPath(string workingDir)
        {
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
}
