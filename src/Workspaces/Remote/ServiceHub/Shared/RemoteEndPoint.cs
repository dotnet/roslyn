// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Roslyn.Utilities;
using StreamJsonRpc;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal sealed class RemoteEndPoint : IDisposable
    {
        private static readonly JsonRpcTargetOptions s_jsonRpcTargetOptions = new JsonRpcTargetOptions()
        {
            // Do not allow JSON-RPC to automatically subscribe to events and remote their calls.
            NotifyClientOfEvents = false,

            // Only allow public methods (may be on internal types) to be invoked remotely.
            AllowNonPublicInvocation = false
        };

        private readonly TraceSource _logger;
        private readonly JsonRpc _rpc;

        private bool _startedListening;
        private JsonRpcDisconnectedEventArgs? _debuggingLastDisconnectReason;
        private string? _debuggingLastDisconnectCallstack;

        public event Action<JsonRpcDisconnectedEventArgs>? Disconnected;
        public event Action<Exception>? UnexpectedExceptionThrown;

        public RemoteEndPoint(Stream stream, TraceSource logger, object? incomingCallTarget, IEnumerable<JsonConverter>? jsonConverters = null)
        {
            RoslynDebug.Assert(stream != null);
            RoslynDebug.Assert(logger != null);

            _logger = logger;

            var jsonFormatter = new JsonMessageFormatter();

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

        public async Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _rpc.InvokeWithCancellationAsync(targetName, arguments?.AsArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public async Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _rpc.InvokeWithCancellationAsync<T>(targetName, arguments?.AsArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public async Task InvokeAsync(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using var stream = new ServerDirectStream();

                Task? task = null;
                try
                {
                    // send request by adding direct stream name to end of arguments
                    task = _rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), cancellationToken);

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    await funcWithDirectStreamAsync(stream, mergedCancellation.Token).ConfigureAwait(false);

                    // wait task to finish
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    // but we need merged one to cancel operation if InvokeAsync has failed. if it failed without
                    // cancellation token is raised, then we do want to have watson report
                    cancellationToken.ThrowIfCancellationRequested();

                    // record reason why task got aborted. use NFW here since we don't want to
                    // crash VS on explicitly killing OOP.
                    (task?.Exception ?? ex).ReportServiceHubNFW("JsonRpc Invoke Failed");

                    throw;
                }
            }
            catch (Exception ex)
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public async Task<T> InvokeAsync<T>(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using var stream = new ServerDirectStream();

                Task? task = null;
                try
                {
                    // send request to asset source
                    task = _rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), cancellationToken);

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    var result = await funcWithDirectStreamAsync(stream, mergedCancellation.Token).ConfigureAwait(false);

                    // wait task to finish
                    await task.ConfigureAwait(false);

                    return result;
                }
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    cancellationToken.ThrowIfCancellationRequested();

                    // record reason why task got aborted. use NFW here since we don't want to
                    // crash VS on explicitly killing OOP.
                    (task?.Exception ?? ex).ReportServiceHubNFW("JsonRpc Invoke Failed");

                    throw;
                }
            }
            catch (Exception ex)
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public async Task<TResult> InvokeAsync<TResult>(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, TResult> funcWithDirectStream, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var mergedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using var stream = new ServerDirectStream();

                Task? task = null;
                try
                {
                    // send request to asset source
                    task = _rpc.InvokeWithCancellationAsync(targetName, arguments.Concat(stream.Name).ToArray(), cancellationToken);

                    // if invoke throws an exception, make sure we raise cancellation.
                    RaiseCancellationIfInvokeFailed(task, mergedCancellation, cancellationToken);

                    // wait for asset source to respond
                    await stream.WaitForDirectConnectionAsync(mergedCancellation.Token).ConfigureAwait(false);

                    // run user task with direct stream
                    var result = funcWithDirectStream(stream, mergedCancellation.Token);

                    // wait task to finish
                    await task.ConfigureAwait(false);

                    return result;
                }
                catch (Exception ex) when (ReportUnlessCanceled(ex, mergedCancellation.Token, cancellationToken))
                {
                    // important to use cancelationToken here rather than mergedCancellationToken.
                    // there is a slight delay when merged cancellation token will be notified once cancellation token
                    // is raised, it can cause one to be in cancelled mode and the other is not. here, one we
                    // actually care is the cancellation token given in, not the merged cancellation token.
                    cancellationToken.ThrowIfCancellationRequested();

                    // record reason why task got aborted. use NFW here since we don't want to
                    // crash VS on explicitly killing OOP.
                    (task?.Exception ?? ex).ReportServiceHubNFW("JsonRpc Invoke Failed");

                    throw;
                }
            }
            catch (Exception ex)
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

#pragma warning disable CA1068 // this method accepts 2 cancellation tokens
        private static bool ReportUnlessCanceled(Exception ex, CancellationToken remoteToken, CancellationToken hostToken)
#pragma warning restore CA1068 // CancellationToken parameters must come last
        {
            // check whether we are in cancellation mode

            // things are either cancelled by us (hostToken) or cancelled by OOP (remoteToken). 
            // "cancelled by us" means operation user invoked is cancelled by another user action such as explicit cancel, or typing.
            // "cancelled by OOP" means operation user invoked is cancelled due to issue on OOP such as user killed OOP process.

            if (hostToken.IsCancellationRequested)
            {
                // we are under our own cancellation, we don't care what the exception is.
                // due to the way we do cancellation (forcefully closing connection in the middle of reading/writing)
                // various exceptions can be thrown. for example, if we close our own named pipe stream in the middle of
                // object reader/writer using it, we could get invalid operation exception or invalid cast exception.
                return true;
            }

            if (remoteToken.IsCancellationRequested)
            {
                // now we allow connection to be closed by users by killing remote host process.
                // in those case, it will be converted to remote token cancellation. we accept that as known
                // exception, and allow us to not crash
                return true;
            }

            // unexpected exception case. crash VS
            return FatalError.Report(ex);
        }

        private static void RaiseCancellationIfInvokeFailed(Task task, CancellationTokenSource mergedCancellation, CancellationToken cancellationToken)
        {
            // if invoke throws an exception, make sure we raise cancellation
            var dummy = task.ContinueWith(p =>
            {
                try
                {
                    // now, we allow user to kill OOP process, when that happen, 
                    // just raise cancellation. 
                    // otherwise, stream.WaitForDirectConnectionAsync can stuck there forever since
                    // cancellation from user won't be raised
                    mergedCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // merged cancellation is already disposed
                }
            }, cancellationToken, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        // these are for debugging purpose. once we find out root cause of the issue
        // we will remove these.
        private static JsonRpcDisconnectedEventArgs? s_debuggingLastDisconnectReason;
        private static string? s_debuggingLastDisconnectCallstack;

        private bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // let cancellation exception thru if they are associated with our cancellation token
                return false;
            }

            _logger.TraceEvent(TraceEventType.Error, 1, ex.ToString());

            s_debuggingLastDisconnectReason = _debuggingLastDisconnectReason;
            s_debuggingLastDisconnectCallstack = _debuggingLastDisconnectCallstack;

            // send NFW to figure out why this is happening
            ex.ReportServiceHubNFW("RemoteHost Failed");

            GC.KeepAlive(_debuggingLastDisconnectReason);
            GC.KeepAlive(_debuggingLastDisconnectCallstack);

            // we return true here to catch all exceptions from servicehub.
            // we record them in NFW and convert that to our soft crash exception.
            return true;
        }

        private SoftCrashException CreateSoftCrashException(Exception ex, CancellationToken cancellationToken)
        {
            // we are getting unexpected exception from service hub. rather than doing hard crash on unexpected exception,
            // we decided to do soft crash where we show info bar to users saying "VS got corrupted and users should save
            // their works and close VS"

            cancellationToken.ThrowIfCancellationRequested();

            LogError($"exception: {ex.ToString()}");

            UnexpectedExceptionThrown?.Invoke(ex);

            // log disconnect information before throw
            LogDisconnectInfo(_debuggingLastDisconnectReason, _debuggingLastDisconnectCallstack);

            // throw soft crash exception
            return new SoftCrashException("remote host call failed", ex, cancellationToken);
        }

        public void LogError(string message)
        {
            _logger.TraceEvent(TraceEventType.Error, 1, message);
        }

        private void LogDisconnectInfo(JsonRpcDisconnectedEventArgs? e, string? callstack)
        {
            if (e != null)
            {
                LogError($@"Stream disconnected unexpectedly: 
{nameof(e.Description)}: {e.Description}
{nameof(e.Reason)}: {e.Reason}
{nameof(e.LastMessage)}: {e.LastMessage}
{nameof(e.Exception)}: {e.Exception?.ToString()}");
            }

            if (callstack != null)
            {
                LogError($"disconnect callstack: {callstack}");
            }
        }

        private void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            _debuggingLastDisconnectReason = e;
            _debuggingLastDisconnectCallstack = new StackTrace().ToString();

            if (e.Reason != DisconnectedReason.LocallyDisposed &&
                e.Reason != DisconnectedReason.RemotePartyTerminated)
            {
                // log when this happens
                LogDisconnectInfo(e, new StackTrace().ToString());
            }

            Disconnected?.Invoke(e);
        }
    }
}
