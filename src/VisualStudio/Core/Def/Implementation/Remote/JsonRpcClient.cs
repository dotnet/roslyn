// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
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

        public JsonRpcEx(TraceSource logger, Stream stream, object callbackTarget, bool useThisAsCallback)
        {
            Contract.Requires(logger != null);
            Contract.Requires(stream != null);

            var target = useThisAsCallback ? this : callbackTarget;

            _logger = logger;

            _rpc = new JsonRpc(new JsonRpcMessageHandler(stream, stream), target);
            _rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            _rpc.Disconnected += OnDisconnected;
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

                await _rpc.InvokeWithCancellationAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                // StreamJsonRpc throws RemoteInvocationException if the call is cancelled.
                // Handle this case by throwing a proper cancellation exception instead.
                // See https://github.com/Microsoft/vs-streamjsonrpc/issues/67
                cancellationToken.ThrowIfCancellationRequested();

                LogError($"exception: {ex.ToString()}");

                // this is to make us not crash. we should remove this once we figure out
                // what is causing this
                ThrowOwnCancellationToken();
            }
        }

        public async Task<T> InvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await _rpc.InvokeWithCancellationAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
            {
                // StreamJsonRpc throws RemoteInvocationException if the call is cancelled.
                // Handle this case by throwing a proper cancellation exception instead.
                // See https://github.com/Microsoft/vs-streamjsonrpc/issues/67
                cancellationToken.ThrowIfCancellationRequested();

                LogError($"exception: {ex.ToString()}");

                // this is to make us not crash. we should remove this once we figure out
                // what is causing this
                ThrowOwnCancellationToken();
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
                // StreamJsonRpc throws RemoteInvocationException if the call is cancelled.
                // Handle this case by throwing a proper cancellation exception instead.
                // See https://github.com/Microsoft/vs-streamjsonrpc/issues/67
                cancellationToken.ThrowIfCancellationRequested();

                LogError($"exception: {ex.ToString()}");

                // this is to make us not crash. we should remove this once we figure out
                // what is causing this
                ThrowOwnCancellationToken();
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
                // StreamJsonRpc throws RemoteInvocationException if the call is cancelled.
                // Handle this case by throwing a proper cancellation exception instead.
                // See https://github.com/Microsoft/vs-streamjsonrpc/issues/67
                cancellationToken.ThrowIfCancellationRequested();

                LogError($"exception: {ex.ToString()}");

                // this is to make us not crash. we should remove this once we figure out
                // what is causing this
                ThrowOwnCancellationToken();
                return Contract.FailWithReturn<T>("can't reach here");
            }
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

            // once we fix all pending vso bugs, we should remove these 
            // and return false so that original exception propagate to the top.
            // for now, we NFW and return true so that we can throw special cancellation exception
            // so that VS doesn't crash.
            s_debuggingLastDisconnectReason = _debuggingLastDisconnectReason;
            s_debuggingLastDisconnectCallstack = _debuggingLastDisconnectCallstack;

            // send NFW to figure out why this is happening
            ex.ReportServiceHubNFW("RemoteHost Failed");

            GC.KeepAlive(_debuggingLastDisconnectReason);
            GC.KeepAlive(_debuggingLastDisconnectCallstack);

            return true;
        }

        private static bool s_reported = false;

        /// <summary>
        /// Show info bar and throw its own cancellation exception until 
        /// we figure out this issue.
        /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/453544
        /// 
        /// the issue is basically we are getting unexpected exception from InvokeAsync
        /// and we don't know exactly why that is happening.
        /// </summary>
        private void ThrowOwnCancellationToken()
        {
            if (CodeAnalysis.PrimaryWorkspace.Workspace != null && !s_reported)
            {
                // do not report it multiple times
                s_reported = true;

                // use info bar to show warning to users
                CodeAnalysis.PrimaryWorkspace.Workspace.Services.GetService<IErrorReportingService>()?.ShowGlobalErrorInfo(
                    ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio);
            }

            // log disconnect information before throw
            LogDisconnectInfo(_debuggingLastDisconnectReason, _debuggingLastDisconnectCallstack);

            // throw special cancellation token to indicate this unexpected situation has happened
            // we create new exception since throw sets stacktrace of the exception
            throw new RemoteHostClientExtensions.UnexpectedRemoteHostException();
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
