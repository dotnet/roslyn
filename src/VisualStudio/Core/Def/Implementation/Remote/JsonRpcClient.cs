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
                HandleException(ex, cancellationToken);
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
            // StreamJsonRpc throws RemoteInvocationException if the call is cancelled.
            // Handle this case by throwing a proper cancellation exception instead.
            // See https://github.com/Microsoft/vs-streamjsonrpc/issues/67
            cancellationToken.ThrowIfCancellationRequested();

            LogError($"exception: {ex.ToString()}");

            // we are getting unexpected exception from service hub. rather than doing hard crash on unexpected exception,
            // we decide to do soft crash where we show info bar to users saying VS got corrupted and users should save
            // thier works and close VS.
            //
            // currently, the way we do soft crash is throwing cancellation exception of our own. since after this point,
            // we consider VS is crashed, no caller should attempt to catch this exception and try to recover or care about
            // this exception.
            // in our point of view, VS is crashed. we just let VS process to alive so that users can save thier work.
            //
            // after this point, think it as OOM happened. like certain code path in Roslyn where we let VS to alive after OOM
            // one should consider VS as unusable and crashed. only there so that users can save thier work.
            //
            // once we have proper non fatal watson framework, we should remove this and let generic NFW to take care of this
            // situation. until then, this is a workaround we have.
            ThrowOwnCancellationToken();
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

            // there was several bugs around VsixDiscoveryService, ExtensionManager, StreamJsonRpc and service hub itself
            // that throws unexpected exceptions that we are tracking, once they are fixed, we should remove these 
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
            throw new UnexpectedRemoteHostException();
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

        /// <summary>
        /// this is a workaround to not crash host when remote call is failed for a reason not
        /// related to us. example will be extension manager failure, connection creation failure
        /// and etc. this is a special exception that should be only used in very specific cases.
        /// 
        /// no one except code related to OOP engine should care about this exception. 
        /// if this is fired, then VS is practicially in corrupted/crashed mode. we just didn't
        /// physically crashed VS due to feedbacks that want to give users time to save their works.
        /// when this is fired, VS clearly shows users to save works and restart VS since VS is crashed.
        /// 
        /// so no one should ever, outside of OOP engine, try to catch this exception and try to recover.
        /// 
        /// that facts this inherits cancellation exception is an implementation detail to make VS not physically crash.
        /// it doesn't mean one should try to recover from it or treat it as cancellation exception.
        /// 
        /// we choose cancellation exception since we didn't want this workaround to be too intrusive.
        /// on our code. we already handle cancellation gracefully and recover properly in most of cases.
        /// but that doesn't mean we want to let users to keep use VS. like I stated above, once this is
        /// fired, VS is logically crashed. we just want VS to be stable enough until users save and exist VS.
        /// 
        /// this is a workaround since we would like to go back to normal crash behavior
        /// if enough of the above issues are fixed or we implements official NFW framework in Roslyn
        /// </summary>
        public class UnexpectedRemoteHostException : OperationCanceledException
        {
            public UnexpectedRemoteHostException() :
                base("unexpected remote host exception", CancellationToken.None)
            {
            }
        }
    }
}
