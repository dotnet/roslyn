// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Helper type that abstract out JsonRpc communication with extra capability of
    /// using raw stream to move over big chunk of data
    /// </summary>
    internal abstract class JsonRpcEx : IDisposable
    {
        private readonly TraceSource _logger;
        private readonly JsonRpc _rpc;

        private JsonRpcDisconnectedEventArgs _debuggingLastDisconnectReason;
        private string _debuggingLastDisconnectCallstack;

        public JsonRpcEx(Workspace workspace, TraceSource logger, Stream stream, object callbackTarget, bool useThisAsCallback)
        {
            Debug.Assert(workspace != null);
            Debug.Assert(logger != null);
            Debug.Assert(stream != null);

            var target = useThisAsCallback ? this : callbackTarget;

            Workspace = workspace;
            _logger = logger;

            _rpc = stream.CreateStreamJsonRpc(target, logger);
            _rpc.Disconnected += OnDisconnected;
        }

        public Workspace Workspace
        {
            get;
        }

        protected abstract void Dispose(bool disposing);

        protected virtual void Disconnected(JsonRpcDisconnectedEventArgs e)
        {
            // do nothing
        }

        public async Task InvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _rpc.InvokeWithCancellationAsync(targetName, arguments?.AsArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                HandleException(ex, cancellationToken);
            }
        }

        public async Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _rpc.InvokeWithCancellationAsync<T>(targetName, arguments?.AsArray(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                HandleException(ex, cancellationToken);
                return Contract.FailWithReturn<T>("can't reach here");
            }
        }

        public async Task InvokeAsync(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) // no when since Extensions.InvokeAsync already recorded it
            {
                HandleException(ex, cancellationToken);
            }
        }

        public async Task<T> InvokeAsync<T>(
            string targetName, IReadOnlyList<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStreamAsync, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) // no when since Extensions.InvokeAsync already recorded it
            {
                HandleException(ex, cancellationToken);
                return Contract.FailWithReturn<T>("can't reach here");
            }
        }

        private void HandleException(Exception ex, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogError($"exception: {ex.ToString()}");

            // we are getting unexpected exception from service hub. rather than doing hard crash on unexpected exception,
            // we decided to do soft crash where we show info bar to users saying "VS got corrupted and users should save
            // thier works and close VS"
            ThrowSoftCrashException(ex, cancellationToken);
        }

        // these are for debugging purpose. once we find out root cause of the issue
        // we will remove these.
        private static JsonRpcDisconnectedEventArgs s_debuggingLastDisconnectReason;
        private static string s_debuggingLastDisconnectCallstack;

        private bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }

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

        private void ThrowSoftCrashException(Exception ex, CancellationToken token)
        {
            RemoteHostCrashInfoBar.ShowInfoBar(Workspace, ex);

            // log disconnect information before throw
            LogDisconnectInfo(_debuggingLastDisconnectReason, _debuggingLastDisconnectCallstack);

            // throw soft crash exception
            throw new SoftCrashException("remote host call failed", ex, token);
        }

        protected void Disconnect()
        {
            _rpc.Dispose();
        }

        protected void StartListening()
        {
            // due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // _rpc need to be explicitly started
            _rpc.StartListening();
        }

        protected void LogError(string message)
        {
            _logger.TraceEvent(TraceEventType.Error, 1, message);
        }

        protected void LogDisconnectInfo(JsonRpcDisconnectedEventArgs e, string callstack)
        {
            if (e != null)
            {
                LogError($"disconnect exception: {e.Description}, {e.Reason}, {e.LastMessage}, {e.Exception?.ToString()}");
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

            // tell we got disconnected
            Disconnected(e);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
