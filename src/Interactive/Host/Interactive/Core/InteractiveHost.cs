// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Represents a process that hosts an interactive session.
    /// </summary>
    /// <remarks>
    /// Handles spawning of the host process and communication between the local callers and the remote session.
    /// </remarks>
    internal sealed partial class InteractiveHost : IDisposable
    {
        internal const InteractiveHostPlatform DefaultPlatform = InteractiveHostPlatform.Core;

        /// <summary>
        /// Use Unicode encoding for STDOUT and STDERR of the InteractiveHost process.
        /// Ideally, we would use UTF8 but SetConsoleOutputCP Windows API fails with "Invalid Handle" when Console.OutputEncoding is set to UTF8.
        /// (issue tracked by https://github.com/dotnet/roslyn/issues/47571, https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1253106)
        /// Unicode is not ideal since the message printed directly to STDOUT/STDERR from native code that do not encode the output are going to be garbled
        /// (e.g. messages reported by CLR stack overflow and OOM exception handlers: https://github.com/dotnet/runtime/issues/45503).
        /// </summary>
        internal static readonly Encoding OutputEncoding = Encoding.Unicode;

        private static readonly JsonRpcTargetOptions s_jsonRpcTargetOptions = new JsonRpcTargetOptions()
        {
            // Do not allow JSON-RPC to automatically subscribe to events and remote their calls.
            NotifyClientOfEvents = false,

            // Only allow public methods (may be on internal types) to be invoked remotely.
            AllowNonPublicInvocation = false
        };

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

        internal event Action<InteractiveHostPlatformInfo, InteractiveHostOptions, RemoteExecutionResult>? ProcessInitialized;

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
            => _lazyRemoteService?.TryGetInitializedService()?.Service?.Process;

        internal async Task<RemoteService?> TryGetServiceAsync()
            => (await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false)).Service;

        // Triggered whenever we create a fresh process.
        // The ProcessExited event is not hooked yet.
        internal event Action<Process>? InteractiveHostProcessCreated;

        // Triggered whenever InteractiveHost process creation fails.
        internal event Action<Exception?, int?>? InteractiveHostProcessCreationFailed;

        #endregion

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

        private async Task WriteOutputInBackgroundAsync(bool isError, string firstLine, string? secondLine = null)
        {
            var writer = isError ? _errorOutput : _output;
            var guard = isError ? _errorOutputGuard : _outputGuard;

            // We cannot guarantee that writers can perform writing synchronously 
            // without deadlocks with other operations.
            // This could happen, for example, for writers provided by the Interactive Window,
            // and in the case where the window is being disposed.
            await Task.Run(() =>
            {
                lock (guard)
                {
                    writer.WriteLine(firstLine);
                    if (secondLine != null)
                    {
                        writer.WriteLine(secondLine);
                    }
                }
            }).ConfigureAwait(false);
        }

        private LazyRemoteService CreateRemoteService(InteractiveHostOptions options, bool skipInitialization)
        {
            return new LazyRemoteService(this, options, Interlocked.Increment(ref _remoteServiceInstanceId), skipInitialization);
        }

        private async Task OnProcessExitedAsync(Process process)
        {
            await ReportProcessExitedAsync(process).ConfigureAwait(false);
            await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
        }

        private async Task ReportProcessExitedAsync(Process process)
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
                await WriteOutputInBackgroundAsync(isError: true, string.Format(InteractiveHostResources.Hosting_process_exited_with_exit_code_0, exitCode.Value)).ConfigureAwait(false);
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

                    var initializedService = await currentRemoteService.GetInitializedServiceAsync().ConfigureAwait(false);
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

                await WriteOutputInBackgroundAsync(isError: true, InteractiveHostResources.Unable_to_create_hosting_process).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // The user reset the process during initialization. 
                // The reset operation will recreate the process.
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable();
            }

            return default;
        }

        private async Task<RemoteExecutionResult> ExecuteRemoteAsync(string targetName, params object?[] arguments)
            => (await InvokeRemoteAsync<RemoteExecutionResult.Data>(targetName, arguments).ConfigureAwait(false))?.Deserialize() ?? default;

        private async Task<TResult> InvokeRemoteAsync<TResult>(string targetName, params object?[] arguments)
        {
            var initializedRemoteService = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
            if (initializedRemoteService.Service == null)
            {
                return default!;
            }

            return await InvokeRemoteAsync<TResult>(initializedRemoteService.Service, targetName, arguments).ConfigureAwait(false);
        }

        private static async Task<RemoteExecutionResult> ExecuteRemoteAsync(RemoteService remoteService, string targetName, params object?[] arguments)
            => (await InvokeRemoteAsync<RemoteExecutionResult.Data>(remoteService, targetName, arguments).ConfigureAwait(false))?.Deserialize() ?? default;

        private static async Task<TResult> InvokeRemoteAsync<TResult>(RemoteService remoteService, string targetName, params object?[] arguments)
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

        private static JsonRpc CreateRpc(Stream stream, object? incomingCallTarget)
        {
            var jsonFormatter = new JsonMessageFormatter();

            // disable interpreting of strings as DateTime during deserialization:
            jsonFormatter.JsonSerializer.DateParseHandling = DateParseHandling.None;

            var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, jsonFormatter))
            {
                CancelLocallyInvokedMethodsWhenConnectionIsClosed = true,
                ExceptionStrategy = ExceptionProcessing.ISerializable,
            };

            if (incomingCallTarget != null)
            {
                rpc.AddLocalRpcTarget(incomingCallTarget, s_jsonRpcTargetOptions);
            }

            rpc.StartListening();

            return rpc;
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
                oldService?.Dispose();

                var initializedService = await TryGetOrCreateRemoteServiceAsync().ConfigureAwait(false);
                if (initializedService.Service == null)
                {
                    return default;
                }

                return initializedService.InitializationResult;
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable();
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
            return ExecuteRemoteAsync(nameof(Service.ExecuteAsync), code);
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
            return ExecuteRemoteAsync(nameof(Service.ExecuteFileAsync), path);
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
            return InvokeRemoteAsync<bool>(nameof(Service.AddReferenceAsync), reference);
        }

        /// <summary>
        /// Sets the current session's search paths and base directory.
        /// </summary>
        public Task<RemoteExecutionResult> SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string baseDirectory)
        {
            Contract.ThrowIfTrue(referenceSearchPaths.IsDefault);
            Contract.ThrowIfTrue(sourceSearchPaths.IsDefault);
            Contract.ThrowIfNull(baseDirectory);

            return ExecuteRemoteAsync(nameof(Service.SetPathsAsync), referenceSearchPaths, sourceSearchPaths, baseDirectory);
        }

        #endregion
    }
}
