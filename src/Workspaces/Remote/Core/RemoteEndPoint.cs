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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Newtonsoft.Json;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal sealed class RemoteEndPoint : IDisposable
    {
        private const string UnexpectedExceptionLogMessage = "Unexpected exception from JSON-RPC";

        private static readonly JsonRpcTargetOptions s_jsonRpcTargetOptions = new JsonRpcTargetOptions()
        {
            // Do not allow JSON-RPC to automatically subscribe to events and remote their calls.
            NotifyClientOfEvents = false,

            // Only allow public methods (may be on internal types) to be invoked remotely.
            AllowNonPublicInvocation = false
        };

        private static int s_id;

        private readonly int _id;
        private readonly TraceSource _logger;
        private readonly JsonRpc _rpc;

        private bool _startedListening;
        private JsonRpcDisconnectedEventArgs? _disconnectedReason;

        public event Action<JsonRpcDisconnectedEventArgs>? Disconnected;
        public event Action<Exception>? UnexpectedExceptionThrown;

        public RemoteEndPoint(Stream stream, TraceSource logger, object? incomingCallTarget, IEnumerable<JsonConverter>? jsonConverters = null)
        {
            RoslynDebug.Assert(stream != null);
            RoslynDebug.Assert(logger != null);

            _id = Interlocked.Increment(ref s_id);
            _logger = logger;

            var jsonFormatter = new JsonMessageFormatter();

            // disable interpreting of strings as DateTime during deserialization:
            jsonFormatter.JsonSerializer.DateParseHandling = DateParseHandling.None;

            if (jsonConverters != null)
            {
                jsonFormatter.JsonSerializer.Converters.AddRange(jsonConverters);
            }

            jsonFormatter.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            _rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, jsonFormatter))
            {
                CancelLocallyInvokedMethodsWhenConnectionIsClosed = true,
                TraceSource = logger
            };

            if (incomingCallTarget != null)
            {
                _rpc.AddLocalRpcTarget(incomingCallTarget, s_jsonRpcTargetOptions);
            }

            _rpc.Disconnected += OnDisconnected;
        }

        /// <summary>
        /// Must be called before any communication commences.
        /// See https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950.
        /// </summary>
        public void StartListening()
        {
            _rpc.StartListening();
            _startedListening = true;
        }

        public bool IsDisposed
            => _rpc.IsDisposed;

        public void Dispose()
        {
            _rpc.Disconnected -= OnDisconnected;
            _rpc.Dispose();
        }

        public async Task InvokeAsync(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);

            // if this end-point is already disconnected do not log more errors:
            var logError = _disconnectedReason == null;

            try
            {
                await _rpc.InvokeWithCancellationAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!logError || ReportUnlessCanceled(ex, cancellationToken))
            {
                // Remote call may fail with different exception even when our cancellation token is signaled
                // (e.g. on shutdown if the connection is dropped):
                cancellationToken.ThrowIfCancellationRequested();

                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public async Task TryInvokeAsync(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);

            if (_rpc.IsDisposed)
            {
                return;
            }

            try
            {
                await _rpc.InvokeWithCancellationAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        public async Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);

            // if this end-point is already disconnected do not log more errors:
            var logError = _disconnectedReason == null;

            try
            {
                return await _rpc.InvokeWithCancellationAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!logError || ReportUnlessCanceled(ex, cancellationToken))
            {
                // Remote call may fail with different exception even when our cancellation token is signaled
                // (e.g. on shutdown if the connection is dropped):
                cancellationToken.ThrowIfCancellationRequested();

                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        /// <summary>
        /// Invokes a remote method <paramref name="targetName"/> with specified <paramref name="arguments"/> and
        /// establishes a pipe through which the target method may transfer large binary data. The name of the pipe is
        /// passed to the target method as an additional argument following the specified <paramref name="arguments"/>.
        /// The target method is expected to use
        /// <see cref="WriteDataToNamedPipeAsync{TData}(string, TData, Func{Stream, TData, CancellationToken, Task}, CancellationToken)"/>
        /// to write the data to the pipe stream.
        /// </summary>
        public async Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object?> arguments, Func<Stream, CancellationToken, Task<T>> dataReader, CancellationToken cancellationToken)
        {
            const int BufferSize = 12 * 1024;

            Contract.ThrowIfFalse(_startedListening);

            // if this end-point is already disconnected do not log more errors:
            var logError = _disconnectedReason == null;

            using var linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var pipeName = Guid.NewGuid().ToString();

            var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            try
            {
                // Transfer ownership of the pipe to BufferedStream, it will dispose it:
                using var stream = new BufferedStream(pipe, BufferSize);

                // send request to asset source
                var task = _rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(pipeName).ToArray(), cancellationToken);

                // if invoke throws an exception, make sure we raise cancellation.
                RaiseCancellationIfInvokeFailed(task, linkedCancellationSource, cancellationToken);

                // wait for asset source to respond
                await pipe.WaitForConnectionAsync(linkedCancellationSource.Token).ConfigureAwait(false);

                // run user task with direct stream
                var result = await dataReader(stream, linkedCancellationSource.Token).ConfigureAwait(false);

                // wait task to finish
                await task.ConfigureAwait(false);

                return result;
            }
            catch (Exception ex) when (!logError || ReportUnlessCanceled(ex, linkedCancellationSource.Token, cancellationToken))
            {
                // Remote call may fail with different exception even when our cancellation token is signaled
                // (e.g. on shutdown if the connection is dropped).
                // It's important to use cancelationToken here rather than linked token as there is a slight 
                // delay in between linked token being signaled and cancellation token being signaled.
                cancellationToken.ThrowIfCancellationRequested();

                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public static Task WriteDataToNamedPipeAsync<TData>(string pipeName, TData data, Func<ObjectWriter, TData, CancellationToken, Task> dataWriter, CancellationToken cancellationToken)
            => WriteDataToNamedPipeAsync(pipeName, data,
                async (stream, data, cancellationToken) =>
                {
                    using var objectWriter = new ObjectWriter(stream, leaveOpen: true, cancellationToken);
                    await dataWriter(objectWriter, data, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);

        public static async Task WriteDataToNamedPipeAsync<TData>(string pipeName, TData data, Func<Stream, TData, CancellationToken, Task> dataWriter, CancellationToken cancellationToken)
        {
            const int BufferSize = 4 * 1024;

            try
            {
                var pipe = new NamedPipeClientStream(serverName: ".", pipeName, PipeDirection.Out);

                var success = false;
                try
                {
                    await ConnectPipeAsync(pipe, cancellationToken).ConfigureAwait(false);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        pipe.Dispose();
                    }
                }

                // Transfer ownership of the pipe to BufferedStream, it will dispose it:
                using var stream = new BufferedStream(pipe, BufferSize);

                await dataWriter(stream, data, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // The stream has closed before we had chance to check cancellation.
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private static async Task ConnectPipeAsync(NamedPipeClientStream pipe, CancellationToken cancellationToken)
        {
            const int ConnectWithoutTimeout = 1;
            const int MaxRetryAttemptsForFileNotFoundException = 3;
            const int ErrorSemTimeoutHResult = unchecked((int)0x80070079);
            var connectRetryInterval = TimeSpan.FromMilliseconds(20);

            var retryCount = 0;
            while (true)
            {
                try
                {
                    // Try connecting without wait.
                    // Connecting with anything else will consume CPU causing a spin wait.
                    pipe.Connect(ConnectWithoutTimeout);
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // Prefer to throw OperationCanceledException if the caller requested cancellation.
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }
                catch (IOException ex) when (ex.HResult == ErrorSemTimeoutHResult)
                {
                    // Ignore and retry.
                }
                catch (TimeoutException)
                {
                    // Ignore and retry.
                }
                catch (FileNotFoundException) when (retryCount < MaxRetryAttemptsForFileNotFoundException)
                {
                    // Ignore and retry
                    retryCount++;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(connectRetryInterval, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void RaiseCancellationIfInvokeFailed(Task task, CancellationTokenSource linkedCancellationSource, CancellationToken cancellationToken)
        {
            // if invoke throws an exception, make sure we raise cancellation
            _ = task.ContinueWith(p =>
            {
                try
                {
                    // now, we allow user to kill OOP process, when that happen, 
                    // just raise cancellation. 
                    // otherwise, stream.WaitForDirectConnectionAsync can stuck there forever since
                    // cancellation from user won't be raised
                    linkedCancellationSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // merged cancellation is already disposed
                }
            }, cancellationToken, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private static bool ReportUnlessCanceled(Exception ex, CancellationToken linkedCancellationToken, CancellationToken cancellationToken)
        {
            // check whether we are in cancellation mode

            // things are either cancelled by us (cancellationToken) or cancelled by OOP (linkedCancellationToken). 
            // "cancelled by us" means operation user invoked is cancelled by another user action such as explicit cancel, or typing.
            // "cancelled by OOP" means operation user invoked is cancelled due to issue on OOP such as user killed OOP process.

            if (cancellationToken.IsCancellationRequested)
            {
                // we are under our own cancellation, we don't care what the exception is.
                // due to the way we do cancellation (forcefully closing connection in the middle of reading/writing)
                // various exceptions can be thrown. for example, if we close our own named pipe stream in the middle of
                // object reader/writer using it, we could get invalid operation exception or invalid cast exception.
                return true;
            }

            if (linkedCancellationToken.IsCancellationRequested)
            {
                // Connection can be closed when the remote process is killed.
                // That will manifest as remote token cancellation.
                return true;
            }

            ReportNonFatalWatson(ex);
            return true;
        }

        private static bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                ReportNonFatalWatson(ex);
            }

            return true;
        }

        private static void ReportNonFatalWatson(Exception exception)
        {
            FatalError.ReportWithoutCrash(exception);
        }

        private SoftCrashException CreateSoftCrashException(Exception ex, CancellationToken cancellationToken)
        {
            // TODO: revisit https://github.com/dotnet/roslyn/issues/40476
            // We are getting unexpected exception from service hub. Rather than doing hard crash on unexpected exception,
            // we decided to do soft crash where we show info bar to users saying "VS got corrupted and users should save
            // their works and close VS"

            UnexpectedExceptionThrown?.Invoke(ex);

            // throw soft crash exception
            return new SoftCrashException(UnexpectedExceptionLogMessage, ex, cancellationToken);
        }

        private void LogError(string message)
        {
            var currentProcess = Process.GetCurrentProcess();
            _logger.TraceEvent(TraceEventType.Error, _id, $" [{currentProcess.ProcessName}:{currentProcess.Id}] {message}");
        }

        private void LogDisconnectInfo(JsonRpcDisconnectedEventArgs? e)
        {
            if (e != null)
            {
                LogError($@"Stream disconnected unexpectedly:  {e.Reason}, '{e.Description}', Exception: {e.Exception?.Message}");
            }
        }

        /// <summary>
        /// Handle disconnection event, so that we detect disconnection as soon as it happens
        /// without waiting for the next failing remote call. The remote call may not happen 
        /// if there is an issue with the connection. E.g. the client end point might not receive
        /// a callback from server, or the server end point might not receive a call from client.
        /// </summary>
        private void OnDisconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            _disconnectedReason = e;

            // Don't log info in cases that are common - such as if we dispose the connection or the remote host process shuts down.
            if (e.Reason != DisconnectedReason.LocallyDisposed &&
                e.Reason != DisconnectedReason.RemotePartyTerminated)
            {
                LogDisconnectInfo(e);
            }

            Disconnected?.Invoke(e);
        }
    }
}
