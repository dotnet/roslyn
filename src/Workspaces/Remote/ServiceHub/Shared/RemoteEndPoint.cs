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

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal sealed class RemoteEndPoint : IDisposable
    {
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

            _rpc = stream.CreateStreamJsonRpc(incomingCallTarget, logger, jsonConverters);
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
                await Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) // no when since Extensions.InvokeAsync already recorded it
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
                return await Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) // no when since Extensions.InvokeAsync already recorded it
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
        }

        public Task<TResult> InvokeAsync<TResult>(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, TResult> funcWithDirectStream, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_startedListening);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStream, cancellationToken);
            }
            catch (Exception ex) // no when since Extensions.InvokeAsync already recorded it
            {
                throw CreateSoftCrashException(ex, cancellationToken);
            }
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
