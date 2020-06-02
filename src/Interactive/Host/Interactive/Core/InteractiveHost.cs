// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Represents a process that hosts an interactive session.
    /// </summary>
    /// <remarks>
    /// Handles spawning of the host process and communication between the local callers and the remote session.
    /// </remarks>
    internal sealed partial class InteractiveHost
    {
        internal const bool DefaultIs64Bit = true;

        private readonly Type _replServiceProviderType;
        private readonly string _initialWorkingDirectory;

        // adjustable for testing purposes
        private readonly int _millisecondsTimeout;
        private const int MaxAttemptsToCreateProcess = 2;

        private LazyRemoteService? _lazyRemoteService;
        private int _remoteServiceInstanceId;

        private TextWriter _output;
        private TextWriter _errorOutput;
        private readonly object _outputGuard;
        private readonly object _errorOutputGuard;

        /// <remarks>
        /// Test only setting.
        /// True to join output writing threads when the host is being disposed.
        /// We have to join the threads before each test is finished, otherwise xunit won't be able to unload the AppDomain.
        /// WARNING: Joining the threads might deadlock if <see cref="Dispose()"/> is executing on the UI thread, 
        /// since the threads are dispatching to UI thread to write the output to the editor buffer.
        /// </remarks>
        private readonly bool _joinOutputWritingThreadsOnDisposal;

        internal event Action<bool>? ProcessStarting;

        public InteractiveHost(
            Type replServiceProviderType,
            string workingDirectory,
            int millisecondsTimeout = 5000,
            bool joinOutputWritingThreadsOnDisposal = false)
        {
            _millisecondsTimeout = millisecondsTimeout;
            _joinOutputWritingThreadsOnDisposal = joinOutputWritingThreadsOnDisposal;
            _output = TextWriter.Null;
            _errorOutput = TextWriter.Null;
            _replServiceProviderType = replServiceProviderType;
            _initialWorkingDirectory = workingDirectory;
            _outputGuard = new object();
            _errorOutputGuard = new object();
        }

        #region Test hooks

        internal event Action<char[], int>? OutputReceived;
        internal event Action<char[], int>? ErrorOutputReceived;

        internal Process? TryGetProcess()
        {
            var lazyRemoteService = _lazyRemoteService;
            return (lazyRemoteService?.InitializedService != null &&
                    lazyRemoteService.InitializedService.TryGetValue(out var initializedService)) ? initializedService.Service.Process : null;
        }

        internal async Task<RemoteService> TryGetServiceAsync()
        {
            var service = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            return service.Service;
        }
        // Triggered whenever we create a fresh process.
        // The ProcessExited event is not hooked yet.
        internal event Action<Process>? InteractiveHostProcessCreated;

        #endregion

        private static string GenerateUniqueChannelLocalName()
            => typeof(InteractiveHost).FullName + Guid.NewGuid();

        private async Task<RemoteService?> TryStartProcessAsync(string hostPath, CultureInfo culture, CancellationToken cancellationToken)
        {
            Process? newProcess = null;
            int newProcessId = -1;
            try
            {
                int currentProcessId = Process.GetCurrentProcess().Id;

                var remoteServerPort = "InteractiveHostChannel-" + Guid.NewGuid();

                var processInfo = new ProcessStartInfo(hostPath);
                string pipeName = GenerateUniqueChannelLocalName();
                processInfo.Arguments = pipeName + " " + currentProcessId;
                processInfo.WorkingDirectory = _initialWorkingDirectory;
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                processInfo.StandardErrorEncoding = Encoding.UTF8;
                processInfo.StandardOutputEncoding = Encoding.UTF8;

                newProcess = new Process();
                newProcess.StartInfo = processInfo;

                // enables Process.Exited event to be raised:
                newProcess.EnableRaisingEvents = true;

                newProcess.Start();
                InteractiveHostProcessCreated?.Invoke(newProcess);

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    newProcessId = newProcess.Id;
                }
                catch
                {
                    newProcessId = 0;
                }

                var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                JsonRpc jsonRpc; // = JsonRpc.Attach(clientStream);

                try
                {
                    await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                    jsonRpc = JsonRpc.Attach(clientStream);
                    await jsonRpc.InvokeWithCancellationAsync<Task>("InitializeAsync",
                        new object[] { _replServiceProviderType.AssemblyQualifiedName, culture.Name },
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is ObjectDisposedException || (!CheckAlive(newProcess, hostPath)))
                {
                    return null;
                }

                return new RemoteService(this, newProcess, newProcessId, jsonRpc);
            }
            catch (OperationCanceledException)
            {
                if (newProcess != null)
                {
                    RemoteService.InitiateTermination(newProcess, newProcessId);
                }

                return null;
            }
        }

        private bool CheckAlive(Process process, string hostPath)
        {
            bool alive = process.IsAlive();
            if (!alive)
            {
                string errorString = process.StandardError.ReadToEnd();

                WriteOutputInBackground(
                    isError: true,
                    string.Format(InteractiveHostResources.Failed_to_launch_0_process_exit_code_colon_1_with_output_colon, hostPath, process.ExitCode),
                    errorString);
            }

            return alive;
        }

        ~InteractiveHost()
        {
            DisposeRemoteService();
        }

        // Dispose may be called anytime.
        public void Dispose()
        {
            // Run this in background to avoid deadlocking with UIThread operations performing with active outputs.
            _ = Task.Run(() => SetOutputs(TextWriter.Null, TextWriter.Null));

            DisposeRemoteService();
            GC.SuppressFinalize(this);
        }

        private void DisposeRemoteService()
        {
            Interlocked.Exchange(ref _lazyRemoteService, null)?.Dispose();
        }

        public void SetOutputs(TextWriter output, TextWriter errorOutput)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (errorOutput == null)
            {
                throw new ArgumentNullException(nameof(errorOutput));
            }

            lock (_outputGuard)
            {
                _output.Flush();
                _output = output;
            }

            lock (_errorOutputGuard)
            {
                _errorOutput.Flush();
                _errorOutput = errorOutput;
            }
        }

        internal void OnOutputReceived(bool error, char[] buffer, int count)
        {
            (error ? ErrorOutputReceived : OutputReceived)?.Invoke(buffer, count);

            var writer = error ? _errorOutput : _output;
            var guard = error ? _errorOutputGuard : _outputGuard;

            lock (guard)
            {
                writer.Write(buffer, 0, count);
            }
        }

        private void WriteOutputInBackground(bool isError, string firstLine, string? secondLine = null)
        {
            var writer = isError ? _errorOutput : _output;
            var guard = isError ? _errorOutputGuard : _outputGuard;

            // We cannot guarantee that writers can perform writing synchronously 
            // without deadlocks with other operations.
            // This could happen, for example, for writers provided by the Interactive Window,
            // and in the case where the window is being disposed.
            Task.Run(() =>
            {
                lock (guard)
                {
                    writer.WriteLine(firstLine);
                    if (secondLine != null)
                    {
                        writer.WriteLine(secondLine);
                    }
                }
            });
        }

        private LazyRemoteService CreateRemoteService(InteractiveHostOptions options, bool skipInitialization)
        {
            return new LazyRemoteService(this, options, Interlocked.Increment(ref _remoteServiceInstanceId), skipInitialization);
        }

        private Task OnProcessExitedAsync(Process process)
        {
            ReportProcessExited(process);
            return TryGetOrCreateRemoteServiceAsync();
        }

        private void ReportProcessExited(Process process)
        {
            int? exitCode;
            try
            {
                exitCode = process.HasExited ? process.ExitCode : (int?)null;
            }
            catch
            {
                exitCode = null;
            }

            if (exitCode.HasValue)
            {
                WriteOutputInBackground(isError: true, string.Format(InteractiveHostResources.Hosting_process_exited_with_exit_code_0, exitCode.Value));
            }
        }

        private async Task<InitializedRemoteService> TryGetOrCreateRemoteServiceAsync()
        {
            try
            {
                LazyRemoteService? currentRemoteService = _lazyRemoteService;

                for (int attempt = 0; attempt < MaxAttemptsToCreateProcess; attempt++)
                {
                    // Remote service may be disposed anytime.
                    if (currentRemoteService == null)
                    {
                        return default;
                    }

                    var initializedService = await currentRemoteService.InitializedService.GetValueAsync(currentRemoteService.CancellationSource.Token).ConfigureAwait(false);
                    if (initializedService.Service != null && initializedService.Service.Process.IsAlive())
                    {
                        return initializedService;
                    }

                    // Service failed to start or initialize or the process died.
                    var newService = CreateRemoteService(currentRemoteService.Options, skipInitialization: !initializedService.InitializationResult.Success);

                    var previousService = Interlocked.CompareExchange(ref _lazyRemoteService, newService, currentRemoteService);
                    if (previousService == currentRemoteService)
                    {
                        // we replaced the service whose process we know is dead:
                        currentRemoteService.Dispose();
                        currentRemoteService = newService;
                    }
                    else
                    {
                        // the process was reset in between our checks, try to use the new service:
                        newService.Dispose();
                        currentRemoteService = previousService;
                    }
                }

                WriteOutputInBackground(isError: true, InteractiveHostResources.Unable_to_create_hosting_process);
            }
            catch (OperationCanceledException)
            {
                // The user reset the process during initialization. 
                // The reset operation will recreate the process.
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }

            return default;
        }

        private async Task<TResult> Async<TResult>(string targetName, params object?[] arguments)
        {
            var initializedRemoteService = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            return await Async<TResult>(initializedRemoteService.Service, targetName, arguments).ConfigureAwait(false);
        }

        private static async Task<TResult> Async<TResult>(RemoteService remoteService, string targetName, params object?[] arguments)
        {
            try
            {
                return await remoteService.JsonRpc.InvokeAsync<TResult>(targetName, arguments).ConfigureAwait(false);
            }
            catch (Exception e) when (e is ObjectDisposedException || !remoteService.Process.IsAlive())
            {
                return default!;
            }
        }

        #region Operations

        public InteractiveHostOptions? OptionsOpt
            => _lazyRemoteService?.Options;

        /// <summary>
        /// Restarts and reinitializes the host process (or starts a new one if it is not running yet).
        /// </summary>
        /// <param name="options">The options to initialize the new process with.</param>
        public async Task<RemoteExecutionResult> ResetAsync(InteractiveHostOptions options)
        {
            try
            {
                // replace the existing service with a new one:
                var newService = CreateRemoteService(options, skipInitialization: false);

                var oldService = Interlocked.Exchange(ref _lazyRemoteService, newService);
                if (oldService != null)
                {
                    oldService.Dispose();
                }

                var initializedService = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
                if (initializedService.Service == null)
                {
                    return default;
                }

                return initializedService.InitializationResult;
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Asynchronously executes given code in the remote interactive session.
        /// </summary>
        /// <param name="code">The code to execute.</param>
        /// <remarks>
        /// This method is thread safe but operations are sent to the remote process
        /// asynchronously so tasks should be executed serially if order is important.
        /// </remarks>
        public Task<RemoteExecutionResult> ExecuteAsync(string code)
        {
            Contract.ThrowIfNull(code);
            return Async<RemoteExecutionResult>("ExecuteAsync", code);
        }

        /// <summary>
        /// Asynchronously executes given code in the remote interactive session.
        /// </summary>
        /// <param name="path">The file to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <remarks>
        /// This method is thread safe but operations are sent to the remote process
        /// asynchronously so tasks should be executed serially if order is important.
        /// </remarks>
        public Task<RemoteExecutionResult> ExecuteFileAsync(string path)
        {
            Contract.ThrowIfNull(path);
            return Async<RemoteExecutionResult>("ExecuteFileAsync", path);
        }

        /// <summary>
        /// Asynchronously adds a reference to the set of available references for next submission.
        /// </summary>
        /// <param name="reference">The reference to add.</param>
        /// <remarks>
        /// This method is thread safe but operations are sent to the remote process
        /// asynchronously so tasks should be executed serially if order is important.
        /// </remarks>
        public Task<bool> AddReferenceAsync(string reference)
        {
            Contract.ThrowIfNull(reference);
            return Async<bool>("AddReferenceAsync", reference);
        }

        /// <summary>
        /// Sets the current session's search paths and base directory.
        /// </summary>
        public Task<RemoteExecutionResult> SetPathsAsync(string[] referenceSearchPaths, string[] sourceSearchPaths, string baseDirectory)
        {
            Contract.ThrowIfNull(referenceSearchPaths);
            Contract.ThrowIfNull(sourceSearchPaths);
            Contract.ThrowIfNull(baseDirectory);

            return Async<RemoteExecutionResult>("SetPathsAsync", referenceSearchPaths, sourceSearchPaths, baseDirectory);
        }

        #endregion
    }
}
